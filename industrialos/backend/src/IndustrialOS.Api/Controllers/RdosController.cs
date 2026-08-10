using System.Security.Claims;
using System.Text.Json;
using IndustrialOS.Application.Ia;
using IndustrialOS.Application.Pdf;
using IndustrialOS.Application.Storage;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Domain.Services;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IndustrialOS.Api.Controllers;

// ---- DTOs de entrada ----
public record EfetivoDto(string? Funcao, int Quantidade, string? Entrada, string? Saida, string? HoraExtra, string? Obs);
public record ParalisacaoDto(string? Inicio, string? Fim, string? Motivo, string? Descricao);
public record RecursoDto(string? Equipamento, int Quantidade, string? Horas, string? Obs);
public record ServicoDto(Guid? ObraItemId, string? Atividade, string? Local, decimal? QtdExec, string? Unidade,
    string? Status, decimal? PctInformado, JsonElement? EtapasFeitas, string? MotivoHold, string? Obs);
public record RetrabalhoDto(string? Atividade, string? Local, decimal? Quantidade, string? Unidade, int Pessoas,
    decimal? Horas, string? Causa, string? Origem, string? Descricao, string? AcaoCorretiva);
public record RdoUpsert(DateOnly Data, string? DiaSemana, string? Turno, string? Ocorrencias,
    JsonElement? Clima, JsonElement? Jornada, JsonElement? Dificuldades, JsonElement? ProximoDia,
    JsonElement? Planejamento, JsonElement? Seguranca, JsonElement? Assinaturas,
    EfetivoDto[]? Efetivo, ParalisacaoDto[]? Paralisacoes, RecursoDto[]? Recursos, ServicoDto[]? Servicos,
    RetrabalhoDto[]? Retrabalho);

[ApiController]
[Route("api/v1")]
[Authorize]
public class RdosController(AppDbContext db, IRdoPdf pdf, IStorage storage, IConfiguration cfg) : ControllerBase
{
    // Planejador/Gestor/Admin acompanham RDOs de todas as obras; demais só das vinculadas.
    private bool VeTodasObras => User.IsInRole("Gestor") || User.IsInRole("Admin") || User.IsInRole("Planejador");
    private Guid UsuarioId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private async Task<bool> PodeVerObra(Guid obraId)
    {
        if (await db.Obras.FindAsync(obraId) is null) return false;
        if (VeTodasObras) return true;
        return await db.UsuarioObras.AnyAsync(x => x.ObraId == obraId && x.UsuarioId == UsuarioId);
    }

    [HttpGet("obras/{obraId:guid}/rdos")]
    public async Task<IActionResult> Listar(Guid obraId)
    {
        if (!await PodeVerObra(obraId)) return Forbid();
        var rdos = await db.Rdos.Where(r => r.ObraId == obraId)
            .OrderByDescending(r => r.Numero)
            .Select(r => new { r.Id, r.Numero, r.Revisao, r.Data, r.Turno, Status = r.Status.ToString() })
            .ToListAsync();
        return Ok(rdos);
    }

    [HttpPost("obras/{obraId:guid}/rdos")]
    public async Task<IActionResult> Criar(Guid obraId, [FromBody] RdoUpsert dto)
    {
        if (!await PodeVerObra(obraId)) return Forbid();
        // Número sequencial: MAX inclui RDOs EXCLUÍDOS (soft delete) — o índice único IX_rdos_ObraId_Numero
        // conta eles, então usar o filtro global aqui causaria colisão (23505) após excluir um RDO.
        var numero = (await db.Rdos.IgnoreQueryFilters().Where(r => r.ObraId == obraId).MaxAsync(r => (int?)r.Numero) ?? 0) + 1;

        var rdo = new Rdo { ObraId = obraId, Numero = numero, ResponsavelUsuarioId = UsuarioId };
        Aplicar(rdo, dto);
        db.Rdos.Add(rdo);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = rdo.Id }, new { rdo.Id, rdo.Numero });
    }

    // Duplica um RDO num novo Rascunho pré-preenchido: copia efetivo, equipamentos e serviços (com o
    // QtdExec ACUMULADO, base do incremento do dia), jornada/clima/segurança. Zera o que é do dia
    // (paralisações, retrabalho, fotos, assinaturas, ocorrências, aprovação/envio). Número novo seguro.
    [HttpPost("rdos/{id:guid}/duplicar")]
    public async Task<IActionResult> Duplicar(Guid id)
    {
        var origem = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (origem is null || !await PodeVerObra(origem.ObraId)) return NotFound();

        // Mesma correção do Criar: MAX inclui RDOs excluídos (índice único conta soft-deletados).
        var numero = (await db.Rdos.IgnoreQueryFilters().Where(r => r.ObraId == origem.ObraId).MaxAsync(r => (int?)r.Numero) ?? 0) + 1;
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var novo = new Rdo
        {
            ObraId = origem.ObraId,
            Numero = numero,
            Revisao = 0,
            Data = hoje,
            DiaSemana = DiaSemanaPt(hoje),
            Turno = origem.Turno,
            ResponsavelUsuarioId = UsuarioId,
            Status = RdoStatus.Rascunho,
            // copiados
            Jornada = origem.Jornada,
            Clima = origem.Clima,
            Seguranca = origem.Seguranca,
            // dia-específicos limpos
            Ocorrencias = null,
            Assinaturas = "{}",
            Dificuldades = "[]",
            ProximoDia = "{}",
            Planejamento = "{}",
        };

        foreach (var e in origem.Efetivo)
            novo.Efetivo.Add(new RdoEfetivo { Funcao = e.Funcao, Quantidade = e.Quantidade, Entrada = e.Entrada, Saida = e.Saida, HoraExtra = e.HoraExtra, Obs = e.Obs });
        foreach (var rc in origem.Recursos)
            novo.Recursos.Add(new RdoRecurso { Equipamento = rc.Equipamento, Quantidade = rc.Quantidade, Horas = rc.Horas, Obs = rc.Obs });
        foreach (var s in origem.Servicos)
            novo.Servicos.Add(new RdoServico
            {
                ObraItemId = s.ObraItemId, Atividade = s.Atividade, Local = s.Local, Unidade = s.Unidade,
                Status = s.Status, PctInformado = null, QtdExec = null, // avanço é por dia: começa zerado; o "anterior" aparece fixo (calculado) na tela
                EtapasFeitas = s.EtapasFeitas, MotivoHold = s.MotivoHold, Obs = s.Obs
            });
        // NÃO copia: Paralisacoes, Retrabalho, RdoMidia (fotos/vídeos). Filhos por navegação, sem setar .Id.

        db.Rdos.Add(novo);
        await db.SaveChangesAsync();
        return Ok(new { id = novo.Id });
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

    [HttpGet("rdos/{id:guid}")]
    public async Task<IActionResult> Obter(Guid id)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();

        // itens da EAP para calcular o avanco por qtd
        var itens = await db.ObraItens.Where(i => i.ObraId == rdo.ObraId).ToDictionaryAsync(i => i.Id);

        // Avanço anterior (fixo) por item da EAP: MAIOR QtdExec do mesmo ObraItemId entre os RDOs da obra
        // com Numero < o deste RDO. Carrega os RDOs anteriores e agrega EM MEMÓRIA (owned collections).
        var rdosAnteriores = await db.Rdos.Where(r => r.ObraId == rdo.ObraId && r.Numero < rdo.Numero).ToListAsync();
        var avancoAnterior = rdosAnteriores
            .SelectMany(r => r.Servicos)
            .Where(s => s.ObraItemId != null && s.QtdExec != null)
            .GroupBy(s => s.ObraItemId!.Value)
            .ToDictionary(g => g.Key, g => g.Max(s => s.QtdExec) ?? 0m);

        return Ok(new
        {
            rdo.Id, rdo.ObraId, rdo.Numero, rdo.Revisao, rdo.Data, rdo.DiaSemana, rdo.Turno,
            Status = rdo.Status.ToString(), rdo.Ocorrencias,
            rdo.TokenAprovacao, rdo.MotivoRevisao, rdo.RevisadoPor, rdo.RevisadoEm, rdo.AprovadoPor, rdo.AprovadoEm,
            Clima = Json(rdo.Clima), Jornada = Json(rdo.Jornada), Dificuldades = Json(rdo.Dificuldades),
            ProximoDia = Json(rdo.ProximoDia), Planejamento = Json(rdo.Planejamento), Seguranca = Json(rdo.Seguranca),
            Assinaturas = Json(rdo.Assinaturas),
            rdo.Efetivo, rdo.Paralisacoes, rdo.Recursos, rdo.Retrabalho,
            Servicos = rdo.Servicos.Select(s => new
            {
                s.Id, s.ObraItemId, s.Atividade, s.Local, s.QtdExec, s.Unidade, s.Status, s.PctInformado,
                s.MotivoHold, s.Obs,
                PctItem = AvancoCalculo.PctItem(s, s.ObraItemId is { } oid && itens.TryGetValue(oid, out var it) ? it : null),
                AvancoAnterior = s.ObraItemId is { } oid2 && avancoAnterior.TryGetValue(oid2, out var av) ? av : 0m
            })
        });
    }

    [HttpPut("rdos/{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] RdoUpsert dto)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();
        if (rdo.Status == RdoStatus.Aprovado)
            return Conflict(new { erro = "RDO aprovado nao pode ser editado." });

        rdo.Efetivo.Clear(); rdo.Paralisacoes.Clear(); rdo.Recursos.Clear(); rdo.Servicos.Clear(); rdo.Retrabalho.Clear();
        Aplicar(rdo, dto);
        await db.SaveChangesAsync();
        return Ok(new { rdo.Id, Status = rdo.Status.ToString() });
    }

    [HttpDelete("rdos/{id:guid}")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();
        // RDO aprovado é registro fechado: não se exclui (o caminho é "solicitar revisão").
        if (rdo.Status == RdoStatus.Aprovado)
            return Conflict(new { erro = "RDO aprovado nao pode ser excluido." });

        // Soft delete (reversível): some das listas, mas fica no banco. Auditado automaticamente.
        rdo.DeletadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("rdos/{id:guid}/finalizar")]
    public async Task<IActionResult> Finalizar(Guid id)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();
        if (rdo.Status == RdoStatus.Aprovado) return Conflict(new { erro = "Ja aprovado." });

        // Reenvio apos revisao solicitada pelo fiscal: incrementa a revisao e limpa o motivo.
        if (rdo.Status == RdoStatus.RevisaoSolicitada || rdo.Status == RdoStatus.EmRevisao)
        {
            rdo.Revisao += 1;
            rdo.MotivoRevisao = null;
            rdo.RevisadoPor = null;
            rdo.RevisadoEm = null;
        }

        rdo.Status = RdoStatus.Enviado;
        rdo.EnviadoPor = UsuarioId;
        rdo.EnviadoEm = DateTime.UtcNow;
        rdo.TokenAprovacao = Guid.NewGuid().ToString("N");

        // Evento de domínio (outbox) — mesma transação. Base p/ IA/integrações futuras.
        db.EventosDominio.Add(new EventoDominio
        {
            Tipo = "rdo_finalizado", AgregadoTipo = "Rdo", AgregadoId = rdo.Id,
            Payload = JsonSerializer.Serialize(new { rdo.Numero, rdo.Revisao, rdo.ObraId })
        });

        await db.SaveChangesAsync();
        return Ok(new { rdo.Id, Status = rdo.Status.ToString(), rdo.TokenAprovacao });
    }

    /// <summary>Resumo automático do dia. Hoje por REGRAS (origem="regras"); a interface IResumoIa
    /// já está pronta para trocar por uma implementação LLM sem mudar este endpoint.</summary>
    [HttpGet("rdos/{id:guid}/resumo")]
    public async Task<IActionResult> Resumo(Guid id, [FromServices] IResumoIa resumoIa)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();

        var obra = await db.Obras.FindAsync(rdo.ObraId);
        if (obra is null) return NotFound();
        var itens = await db.ObraItens.Where(i => i.ObraId == rdo.ObraId).ToListAsync();
        var categorias = (await db.Funcoes.Select(f => new { f.Nome, f.Categoria }).ToListAsync())
            .GroupBy(x => x.Nome.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First().Categoria);

        var ctx = RdoContextoBuilder.Build(rdo, obra, itens, categorias);
        var res = await resumoIa.ResumirRdoAsync(ctx);
        return Ok(new { texto = res.Texto, origem = res.Origem });
    }

    [HttpGet("rdos/{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();

        var model = await RdoPdfFactory.BuildAsync(db, rdo, storage, cfg["App:BaseUrl"] ?? "http://localhost:5173");
        return File(pdf.Gerar(model), "application/pdf", $"RDO-{rdo.Numero}.pdf");
    }

    // ---- midia (fotos/videos) — separada do autosave, guardada no R2 ----
    [HttpPost("rdos/{id:guid}/midia")]
    [RequestSizeLimit(50_000_000)] // 50 MB
    public async Task<IActionResult> SubirMidia(Guid id, IFormFile file, [FromForm] string? categoria, [FromForm] string? descricao)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();
        if (file is null || file.Length == 0) return BadRequest(new { erro = "Arquivo vazio." });

        var tipo = file.ContentType.StartsWith("video") ? "video" : "foto";
        var ext = Path.GetExtension(file.FileName);
        var key = $"rdos/{id}/{Guid.NewGuid():N}{ext}";

        await using (var s = file.OpenReadStream())
            await storage.UploadAsync(s, key, string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);

        var midia = new RdoMidia { RdoId = id, Tipo = tipo, R2Key = key, Categoria = categoria, Descricao = descricao, TamanhoBytes = file.Length };
        db.RdoMidias.Add(midia);
        await db.SaveChangesAsync();
        return Ok(new { midia.Id, midia.Tipo });
    }

    [HttpGet("rdos/{id:guid}/midia")]
    public async Task<IActionResult> ListarMidia(Guid id)
    {
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == id);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();

        var itens = await db.RdoMidias.Where(m => m.RdoId == id).OrderBy(m => m.CriadoEm).ToListAsync();
        var res = new List<object>();
        foreach (var m in itens)
            res.Add(new { m.Id, m.Tipo, m.Categoria, m.Descricao, m.TamanhoBytes, Url = await storage.UrlAssinadaAsync(m.R2Key, TimeSpan.FromHours(1)) });
        return Ok(res);
    }

    [HttpDelete("midia/{midiaId:guid}")]
    public async Task<IActionResult> ApagarMidia(Guid midiaId)
    {
        var m = await db.RdoMidias.FindAsync(midiaId);
        if (m is null) return NotFound();
        var rdo = await db.Rdos.FirstOrDefaultAsync(r => r.Id == m.RdoId);
        if (rdo is null || !await PodeVerObra(rdo.ObraId)) return NotFound();

        await storage.DeleteAsync(m.R2Key);
        db.RdoMidias.Remove(m);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- helpers ----
    private static JsonElement Json(string raw) => JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
    private static string Raw(JsonElement? e, string fallback) => e is { } el ? el.GetRawText() : fallback;

    private static void Aplicar(Rdo rdo, RdoUpsert d)
    {
        rdo.Data = d.Data; rdo.DiaSemana = d.DiaSemana; rdo.Turno = d.Turno; rdo.Ocorrencias = d.Ocorrencias;
        rdo.Clima = Raw(d.Clima, "{}"); rdo.Jornada = Raw(d.Jornada, "{}");
        rdo.Dificuldades = Raw(d.Dificuldades, "[]"); rdo.ProximoDia = Raw(d.ProximoDia, "{}");
        rdo.Planejamento = Raw(d.Planejamento, "{}"); rdo.Seguranca = Raw(d.Seguranca, "{}");
        rdo.Assinaturas = Raw(d.Assinaturas, "{}");

        foreach (var rt in d.Retrabalho ?? [])
            rdo.Retrabalho.Add(new RdoRetrabalho
            {
                Atividade = rt.Atividade, Local = rt.Local, Quantidade = rt.Quantidade, Unidade = rt.Unidade,
                Pessoas = rt.Pessoas, Horas = rt.Horas, Causa = rt.Causa, Origem = rt.Origem,
                Descricao = rt.Descricao, AcaoCorretiva = rt.AcaoCorretiva
            });

        foreach (var e in d.Efetivo ?? [])
            rdo.Efetivo.Add(new RdoEfetivo { Funcao = e.Funcao, Quantidade = e.Quantidade, Entrada = e.Entrada, Saida = e.Saida, HoraExtra = e.HoraExtra, Obs = e.Obs });
        foreach (var p in d.Paralisacoes ?? [])
            rdo.Paralisacoes.Add(new RdoParalisacao { Inicio = p.Inicio, Fim = p.Fim, Motivo = p.Motivo, Descricao = p.Descricao });
        foreach (var r in d.Recursos ?? [])
            rdo.Recursos.Add(new RdoRecurso { Equipamento = r.Equipamento, Quantidade = r.Quantidade, Horas = r.Horas, Obs = r.Obs });
        foreach (var s in d.Servicos ?? [])
            rdo.Servicos.Add(new RdoServico
            {
                ObraItemId = s.ObraItemId, Atividade = s.Atividade, Local = s.Local, QtdExec = s.QtdExec,
                Unidade = s.Unidade, Status = s.Status, PctInformado = s.PctInformado,
                EtapasFeitas = Raw(s.EtapasFeitas, "[]"), MotivoHold = s.MotivoHold, Obs = s.Obs
            });
    }
}
