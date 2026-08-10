using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IndustrialOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IndustrialOS.Infrastructure.Persistence;

/// <summary>Cria uma OBRA DE EXEMPLO completa e realista em um tenant, para quem testa a conta grátis já
/// ver o produto preenchido (EAP + 6 RDOs com avanço, HH, hora-extra, paralisação, segurança). Tudo com o
/// prefixo <see cref="Marcador"/> para ser óbvio e fácil de excluir depois.
///
/// SEGURANÇA: NÃO cria entidade/schema novo (usa só as entidades existentes). É IDEMPOTENTE (marcador no
/// Cliente) e NUNCA pode quebrar o cadastro/boot — todo o SeedAsync é envolto em try/catch.
///
/// PEGADINHAS respeitadas (ver CLAUDE.md):
/// - Sem contexto de tenant no signup/boot → TenantId é setado EXPLICITAMENTE em toda BaseEntity
///   (Cliente/Obra/ObraItem/Rdo). O Stamp do SaveChanges só preenche quando vazio.
/// - Filhos de RDO (Efetivo/Paralisacao/Recurso/Servico/Retrabalho) NÃO são BaseEntity → adicionados por
///   navegação e deixa o EF cascatear. NENHUM .Id é atribuído manualmente (EF gera).</summary>
public static class ContaExemploSeeder
{
    public const string Marcador = "[EXEMPLO]";

    /// <summary>Gate por config (default TRUE): permite desligar via Seed:ContaExemplo=false sem mudar código.</summary>
    public static bool Habilitado(IConfiguration cfg) => cfg.GetValue("Seed:ContaExemplo", true);

    /// <summary>Cria a obra de exemplo NESSE tenant. Idempotente: se já existir Cliente "[EXEMPLO]" nesse
    /// tenant, retorna false sem fazer nada. Não seeda tenant de SISTEMA. Nunca lança (try/catch interno).</summary>
    public static async Task<bool> SeedAsync(AppDbContext db, Guid tenantId, Guid? empresaId, ILogger? log = null)
    {
        try
        {
            var t = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == tenantId);
            if (t is null || t.EhSistema || t.DeletadoEm != null) return false;

            var jaTem = await db.Clientes.IgnoreQueryFilters()
                .AnyAsync(c => c.TenantId == tenantId && c.Nome.StartsWith(Marcador));
            if (jaTem) return false;

            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            // ---- Cliente + Obra (BaseEntity → TenantId explícito; Id da BaseEntity já vem gerado) ----
            var cliente = new Cliente
            {
                TenantId = tenantId,
                Nome = $"{Marcador} Refinaria Litoral S.A.",
                Cnpj = "12.345.678/0001-90",
                Contato = "Eng. Marina Prado",
            };
            db.Clientes.Add(cliente);

            var obra = new Obra
            {
                TenantId = tenantId,
                ClienteId = cliente.Id,
                EmpresaId = empresaId,
                Nome = $"{Marcador} Montagem de Tubulação e Estruturas — Unidade de Utilidades",
                Contrato = "CT-2026-014",
                Local = "Cubatão/SP",
                FrenteServico = "UTE",
                PrazoPagamento = "30/60",
                Status = ObraStatus.Andamento,
                DataInicio = hoje.AddDays(-60),
            };
            db.Obras.Add(obra);

            // ---- EAP (ObraItem: BaseEntity → TenantId explícito) ----
            ObraItem Item(int ordem, string desc, string un, decimal qtd, decimal hh, decimal valor, string disc)
            {
                var i = new ObraItem
                {
                    TenantId = tenantId, ObraId = obra.Id, Ordem = ordem, Descricao = desc,
                    Unidade = un, QtdPrevista = qtd, HhPrevisto = hh, Valor = valor, Disciplina = disc,
                };
                db.ObraItens.Add(i);
                return i;
            }

            var itMob = Item(1, "Mobilização e canteiro", "vb", 1, 800, 120000, "Mobilização");
            var itFund = Item(2, "Fundações e bases de equipamentos", "m³", 350, 4200, 480000, "Civil");
            var itEstr = Item(3, "Montagem de estruturas metálicas", "t", 220, 9800, 1650000, "Estrutura");
            var itTub = Item(4, "Montagem de tubulação - spools", "jt", 5400, 16200, 2900000, "Tubulação");
            var itCald = Item(5, "Caldeiraria - tanques e vasos", "t", 90, 5400, 890000, "Caldeiraria");
            var itEle = Item(6, "Elétrica e instrumentação", "vb", 1, 6100, 760000, "Elétrica/Instrumentação");
            Item(7, "Pintura industrial", "m²", 12000, 3600, 420000, "Pintura");
            Item(8, "Comissionamento", "vb", 1, 2800, 380000, "Comissionamento");
            Item(9, "Desmobilização", "vb", 1, 600, 90000, "Desmobilização");

            // ---- 6 RDOs em dias úteis recentes ----
            var dias = UltimosDiasUteis(hoje.AddDays(-1), 6); // ascendente: dias[0]=+antigo … dias[5]=+recente

            // RDO 1..4 Aprovado · 5 Enviado · 6 Rascunho
            var r1 = NovoRdo(tenantId, obra.Id, 1, dias[0], RdoStatus.Aprovado, 26, "Início da mobilização do canteiro.");
            AddEfetivo(r1, false);
            AddRecursos(r1);
            AddServ(r1, itMob.Id, "Mobilização e organização do canteiro", "vb", qtdExec: 0.5m, status: "Em andamento");
            AddServ(r1, itFund.Id, "Concretagem de bases de equipamentos", "m³", qtdExec: 90m, status: "Em andamento");

            var r2 = NovoRdo(tenantId, obra.Id, 2, dias[1], RdoStatus.Aprovado, 28, "Canteiro concluído; início da montagem de estruturas.");
            AddEfetivo(r2, false);
            AddRecursos(r2);
            AddServ(r2, itMob.Id, "Conclusão da mobilização", "vb", qtdExec: 1m, status: "Concluido");
            AddServ(r2, itFund.Id, "Concretagem de bases de equipamentos", "m³", qtdExec: 160m, status: "Em andamento");
            AddServ(r2, itEstr.Id, "Montagem de estruturas metálicas", "t", qtdExec: 30m, status: "Em andamento");

            var r3 = NovoRdo(tenantId, obra.Id, 3, dias[2], RdoStatus.Aprovado, 30, "Início da montagem de spools de tubulação.");
            AddEfetivo(r3, true); // soldadores com hora-extra
            AddRecursos(r3);
            r3.Retrabalho.Add(new RdoRetrabalho
            {
                Atividade = "Recorte e reajuste de spool", Local = "UTE", Pessoas = 2, Horas = 3m,
                Causa = "Divergência de medida", Origem = "Fabricação", AcaoCorretiva = "Refeito conforme isométrico.",
            });
            AddServ(r3, itFund.Id, "Concretagem de bases de equipamentos", "m³", qtdExec: 230m, status: "Em andamento");
            AddServ(r3, itEstr.Id, "Montagem de estruturas metálicas", "t", qtdExec: 60m, status: "Em andamento");
            AddServ(r3, itTub.Id, "Montagem de spools de tubulação", "jt", qtdExec: 400m, status: "Em andamento");

            var r4 = NovoRdo(tenantId, obra.Id, 4, dias[3], RdoStatus.Aprovado, 24,
                "Chuva no período da manhã impactou a montagem em altura.",
                dificuldades: "Chuva forte pela manhã atrasou a frente de estruturas.");
            AddEfetivo(r4, false);
            AddRecursos(r4);
            r4.Paralisacoes.Add(new RdoParalisacao
            {
                Inicio = "10:00", Fim = "12:00", Motivo = "Chuva",
                Descricao = "Chuva forte — parada da frente de montagem em altura.",
            });
            AddServ(r4, itFund.Id, "Fundações e bases (conclusão da etapa)", "m³", qtdExec: 280m, status: "Em andamento");
            AddServ(r4, itEstr.Id, "Montagem de estruturas metálicas", "t", qtdExec: 90m, status: "Em andamento");
            AddServ(r4, itTub.Id, "Montagem de spools de tubulação", "jt", qtdExec: 800m, status: "Em andamento");
            AddServ(r4, itCald.Id, "Fabricação de vasos e tanques", "t", qtdExec: 5m, status: "Em andamento");

            var r5 = NovoRdo(tenantId, obra.Id, 5, dias[4], RdoStatus.Enviado, 29, "Avanço firme em tubulação; início de elétrica.");
            AddEfetivo(r5, true); // soldadores com hora-extra
            AddRecursos(r5);
            AddServ(r5, itEstr.Id, "Montagem de estruturas metálicas", "t", qtdExec: 110m, status: "Em andamento");
            AddServ(r5, itTub.Id, "Montagem de spools de tubulação", "jt", qtdExec: 1100m, status: "Em andamento");
            AddServ(r5, itCald.Id, "Fabricação de vasos e tanques", "t", qtdExec: 10m, status: "Em andamento");
            AddServ(r5, itEle.Id, "Eletrocalhas e instrumentação", "vb", pctInformado: 3m, status: "Em andamento");

            var r6 = NovoRdo(tenantId, obra.Id, 6, dias[5], RdoStatus.Rascunho, 27, "Continuação das frentes de tubulação e caldeiraria.");
            AddEfetivo(r6, false);
            AddRecursos(r6);
            AddServ(r6, itEstr.Id, "Montagem de estruturas metálicas", "t", qtdExec: 121m, status: "Em andamento");
            AddServ(r6, itTub.Id, "Montagem de spools de tubulação", "jt", qtdExec: 1350m, status: "Em andamento");
            AddServ(r6, itCald.Id, "Fabricação de vasos e tanques", "t", qtdExec: 13.5m, status: "Em andamento");
            AddServ(r6, itEle.Id, "Eletrocalhas e instrumentação", "vb", pctInformado: 5m, status: "Em andamento");

            db.Rdos.AddRange(r1, r2, r3, r4, r5, r6);

            await db.SaveChangesAsync();
            log?.LogInformation("ContaExemploSeeder: obra de exemplo criada para o tenant {Tenant}.", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            // NUNCA pode quebrar o cadastro nem o boot: loga e segue.
            log?.LogWarning(ex, "ContaExemploSeeder: falha ao semear tenant {Tenant} (ignorado).", tenantId);
            return false;
        }
    }

    /// <summary>Percorre todos os tenants não-sistema e semeia o exemplo em quem ainda não tem. Cada tenant
    /// em try/catch próprio: um erro num tenant não aborta os demais. Idempotente pelo marcador.</summary>
    public static async Task BackfillAsync(AppDbContext db, ILogger? log = null)
    {
        var tenantIds = await db.Tenants
            .Where(t => !t.EhSistema && t.DeletadoEm == null)
            .Select(t => t.Id)
            .ToListAsync();

        foreach (var tid in tenantIds)
        {
            try
            {
                var empresaId = await db.Empresas.IgnoreQueryFilters()
                    .Where(e => e.TenantId == tid)
                    .Select(e => (Guid?)e.Id)
                    .FirstOrDefaultAsync();
                await SeedAsync(db, tid, empresaId, log);
            }
            catch (Exception ex)
            {
                log?.LogWarning(ex, "ContaExemploSeeder.Backfill: tenant {Tenant} falhou (ignorado).", tid);
            }
        }
    }

    // ---- helpers ----
    private static string J(object o) => JsonSerializer.Serialize(o); // preserva os nomes camelCase das chaves

    private static Rdo NovoRdo(Guid tenantId, Guid obraId, int numero, DateOnly data, RdoStatus status,
        int tempC, string ocorrencias, string? dificuldades = null)
    {
        var ptNum = $"2026-014-{numero:D2}";
        var rdo = new Rdo
        {
            TenantId = tenantId,
            ObraId = obraId,
            Numero = numero,
            Revisao = 0,
            Data = data,
            DiaSemana = DiaSemanaPt(data),
            Turno = "Diurno",
            Status = status,
            Ocorrencias = ocorrencias,
            // Chaves EXATAS lidas pelo front (Rdo.tsx) e pelo PDF:
            Clima = J(new { condicoes = new[] { "Parcialmente Nublado" }, temperatura = tempC }),
            Jornada = J(new { inicio = "07:00", almoco = "12:00", retorno = "13:00", termino = "16:48", feriado = false }),
            Dificuldades = J(new { descricao = dificuldades ?? "" }),
            ProximoDia = J(new
            {
                maoObra = "Manter efetivo de montagem e soldagem.",
                equipamentos = "Guindaste 25t e máquinas de solda.",
                materiais = "Spools e consumíveis de solda.",
                ferramentas = "Ferramental de montagem e torqueadeira.",
                pendencias = Array.Empty<object>(),
            }),
            Planejamento = J(new
            {
                servicos = "Montagem de estruturas e tubulação; fabricação de caldeiraria.",
                prioridades = "Liberar frentes de tubulação para teste.",
                areas = "UTE — pipe rack e área de tanques.",
                observacoes = "",
            }),
            Seguranca = J(new
            {
                dds = true, apr = true, pt = true, areaIsolada = true, epis = true, ferramentas = true,
                observacoes = $"DDS: Trabalho em altura. APR emitida. PT nº {ptNum}.",
            }),
            Assinaturas = J(new { encarregado = new { nome = "Carlos Nunes" }, fiscal = new { } }),
        };

        if (status == RdoStatus.Aprovado)
        {
            rdo.AprovadoPor = "Eng. Marina Prado";
            rdo.AprovadoEm = DateTime.UtcNow;
        }
        else if (status == RdoStatus.Enviado)
        {
            rdo.EnviadoEm = DateTime.UtcNow;
            rdo.TokenAprovacao = Guid.NewGuid().ToString("N");
        }
        return rdo;
    }

    // Efetivo do dia (nomes EXATOS do catálogo FuncoesPadrao). Filhos SEM Id/TenantId — via navegação.
    private static void AddEfetivo(Rdo rdo, bool comHoraExtra)
    {
        void E(string funcao, int qtd, string? horaExtra = null) => rdo.Efetivo.Add(new RdoEfetivo
        {
            Funcao = funcao, Quantidade = qtd, Entrada = "07:00", Saida = "16:48", HoraExtra = horaExtra,
        });
        E("ENCARREGADO", 1);
        E("SOLDADOR", 6, comHoraExtra ? "16:48-18:48" : null);
        E("CALDEIREIRO", 4);
        E("MONTADOR", 5);
        E("AUX.MONTAGEM", 4);
    }

    private static void AddRecursos(Rdo rdo)
    {
        rdo.Recursos.Add(new RdoRecurso { Equipamento = "Máquina de solda", Quantidade = 6, Horas = "8" });
        rdo.Recursos.Add(new RdoRecurso { Equipamento = "Guindaste 25t", Quantidade = 1, Horas = "6" });
    }

    // Serviço vinculado a item da EAP. Avanço = max(pctInformado/100, qtdExec/qtdPrev, Concluido?1) — ver AvancoCalculo.
    private static void AddServ(Rdo rdo, Guid obraItemId, string atividade, string unidade,
        decimal? qtdExec = null, decimal? pctInformado = null, string status = "Em andamento")
        => rdo.Servicos.Add(new RdoServico
        {
            ObraItemId = obraItemId, Atividade = atividade, Local = "UTE",
            QtdExec = qtdExec, Unidade = unidade, PctInformado = pctInformado, Status = status,
        });

    private static List<DateOnly> UltimosDiasUteis(DateOnly fim, int n)
    {
        var dias = new List<DateOnly>();
        var d = fim;
        while (dias.Count < n)
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) dias.Add(d);
            d = d.AddDays(-1);
        }
        dias.Reverse(); // ordem cronológica ascendente
        return dias;
    }

    private static string DiaSemanaPt(DateOnly d) => d.DayOfWeek switch
    {
        DayOfWeek.Monday => "Segunda-feira",
        DayOfWeek.Tuesday => "Terça-feira",
        DayOfWeek.Wednesday => "Quarta-feira",
        DayOfWeek.Thursday => "Quinta-feira",
        DayOfWeek.Friday => "Sexta-feira",
        DayOfWeek.Saturday => "Sábado",
        _ => "Domingo",
    };
}
