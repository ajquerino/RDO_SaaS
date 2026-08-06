using System.Security.Claims;
using System.Text.Json;
using IndustrialOS.Application.Pdf;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Domain.Services;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record MedicaoRequest(DateOnly De, DateOnly Ate, Guid? CondicaoPagamentoId, Guid? FaturamentoEventoId);

/// <summary>Boletins de medicao (BM). Emitir/ver = Planejador/Gestor/Admin; apagar = so Admin.</summary>
[ApiController]
[Route("api/v1")]
[Authorize(Roles = "Planejador,Gestor,Admin")]
public class MedicoesController(AppDbContext db, IMedicaoPdf pdf) : ControllerBase
{
    private bool EhAdmin => User.IsInRole("Admin");
    private bool VeTodasObras => User.IsInRole("Gestor") || User.IsInRole("Admin") || User.IsInRole("Planejador");
    private Guid UsuarioId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private async Task<bool> PodeVerObra(Guid obraId)
    {
        if (await db.Obras.FindAsync(obraId) is null) return false;
        if (VeTodasObras) return true;
        return await db.UsuarioObras.AnyAsync(x => x.ObraId == obraId && x.UsuarioId == UsuarioId);
    }

    // ---- calcular (sem salvar) ----
    [HttpPost("obras/{obraId:guid}/medicoes/calcular")]
    public async Task<IActionResult> Calcular(Guid obraId, [FromBody] MedicaoRequest r)
    {
        if (!await PodeVerObra(obraId)) return NotFound();
        var calc = await Computar(obraId, r.De, r.Ate, r.CondicaoPagamentoId);
        return Ok(Projetar(calc, r.De, r.Ate));
    }

    // ---- salvar (numera + gera token) ----
    [HttpPost("obras/{obraId:guid}/medicoes")]
    public async Task<IActionResult> Criar(Guid obraId, [FromBody] MedicaoRequest r)
    {
        if (!await PodeVerObra(obraId)) return NotFound();
        if (r.Ate < r.De) return BadRequest(new { erro = "A data final deve ser maior ou igual a inicial." });

        var calc = await Computar(obraId, r.De, r.Ate, r.CondicaoPagamentoId);
        var numero = (await db.Medicoes.Where(m => m.ObraId == obraId).MaxAsync(m => (int?)m.Numero) ?? 0) + 1;

        var medicao = new Medicao
        {
            ObraId = obraId, Numero = numero, De = r.De, Ate = r.Ate,
            FaturamentoEventoId = r.FaturamentoEventoId,
            ValorContrato = calc.ValorContrato, MedidoAcumulado = calc.MedidoAcumulado,
            ValorPeriodo = calc.ValorPeriodo, PctFisico = calc.PctFisico, PctFinanceiro = calc.PctFinanceiro,
            Status = "emitido", TokenAprovacao = Guid.NewGuid().ToString("N"), CriadoPor = UsuarioId,
        };
        foreach (var i in calc.Itens)
            medicao.Itens.Add(new MedicaoItem
            {
                ObraItemId = i.ObraItemId, PctIni = i.PctIni, PctFim = i.PctFim,
                Valor = i.Valor, MedidoPeriodo = i.MedidoPeriodo, MedidoAcum = i.MedidoAcum
            });
        foreach (var p in calc.Parcelas)
            medicao.Parcelas.Add(new MedicaoParcela { Dias = p.Dias, Vencimento = p.Vencimento, Valor = p.Valor, Pct = p.Pct });

        db.Medicoes.Add(medicao);
        db.EventosDominio.Add(new EventoDominio
        {
            Tipo = "medicao_emitida", AgregadoTipo = "Medicao", AgregadoId = medicao.Id,
            Payload = JsonSerializer.Serialize(new { medicao.Numero, medicao.ObraId, medicao.ValorPeriodo })
        });
        await db.SaveChangesAsync();
        return Ok(new { medicao.Id, medicao.Numero, medicao.Status, medicao.TokenAprovacao });
    }

    // ---- listar ----
    [HttpGet("obras/{obraId:guid}/medicoes")]
    public async Task<IActionResult> Listar(Guid obraId)
    {
        if (!await PodeVerObra(obraId)) return NotFound();
        var medicoes = await db.Medicoes.Where(m => m.ObraId == obraId)
            .OrderByDescending(m => m.Numero)
            .Select(m => new { m.Id, m.Numero, m.De, m.Ate, m.ValorPeriodo, m.MedidoAcumulado, m.PctFisico, m.PctFinanceiro, m.Status })
            .ToListAsync();
        return Ok(medicoes);
    }

    // ---- obter (com itens + parcelas) ----
    [HttpGet("medicoes/{id:guid}")]
    public async Task<IActionResult> Obter(Guid id)
    {
        var m = await db.Medicoes.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null || !await PodeVerObra(m.ObraId)) return NotFound();

        var descr = await db.ObraItens.Where(i => i.ObraId == m.ObraId).ToDictionaryAsync(i => i.Id, i => i.Descricao);
        return Ok(new
        {
            m.Id, m.ObraId, m.Numero, m.De, m.Ate, m.ValorContrato, m.MedidoAcumulado, m.ValorPeriodo,
            m.PctFisico, m.PctFinanceiro, m.Status, m.CriadoPor, m.AprovadoPor, m.AprovadoEm,
            Itens = m.Itens.Select(i => new
            {
                i.ObraItemId, Descricao = descr.TryGetValue(i.ObraItemId, out var d) ? d : "(item)",
                i.PctIni, i.PctFim, i.Valor, i.MedidoPeriodo, i.MedidoAcum
            }),
            Parcelas = m.Parcelas.OrderBy(p => p.Dias).Select(p => new { p.Dias, p.Vencimento, p.Valor, p.Pct })
        });
    }

    // ---- apagar (so Admin) ----
    [HttpDelete("medicoes/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Apagar(Guid id)
    {
        var m = await db.Medicoes.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null || !await PodeVerObra(m.ObraId)) return NotFound();
        m.DeletadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- PDF do boletim ----
    [HttpGet("medicoes/{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        var m = await db.Medicoes.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null || !await PodeVerObra(m.ObraId)) return NotFound();

        var obra = await db.Obras.FindAsync(m.ObraId);
        var cliente = obra?.ClienteId is { } cid ? await db.Clientes.FindAsync(cid) : null;
        var criador = m.CriadoPor is { } uid ? (await db.Usuarios.FindAsync(uid))?.Nome : null;
        var descr = await db.ObraItens.Where(i => i.ObraId == m.ObraId).ToDictionaryAsync(i => i.Id, i => i.Descricao);

        var model = new MedicaoPdfModel(
            obra?.Nome ?? "Obra", obra?.Contrato, cliente?.Nome, obra?.Local,
            m.Numero, m.De.ToString("dd/MM/yyyy"), m.Ate.ToString("dd/MM/yyyy"), m.Status,
            m.ValorContrato, m.MedidoAcumulado, m.ValorPeriodo, m.PctFisico, m.PctFinanceiro,
            m.Itens.Select(i => new MedicaoItemRow(
                descr.TryGetValue(i.ObraItemId, out var d) ? d : "(item)",
                i.PctIni, i.PctFim, i.Valor, i.MedidoPeriodo, i.MedidoAcum)).ToList(),
            m.Parcelas.OrderBy(p => p.Dias).Select(p => new MedicaoParcelaRow(p.Dias, p.Vencimento.ToString("dd/MM/yyyy"), p.Valor, p.Pct)).ToList(),
            criador);

        return File(pdf.Gerar(model), "application/pdf", $"BM-{m.Numero}.pdf");
    }

    // ================= calculo (fonte unica) =================
    private record ItemCalc(Guid ObraItemId, string Descricao, decimal PctIni, decimal PctFim, decimal Valor, decimal MedidoPeriodo, decimal MedidoAcum);
    private record Calc(decimal ValorContrato, List<ItemCalc> Itens, decimal MedidoAcumulado, decimal ValorPeriodo,
        decimal PctFisico, decimal PctFinanceiro, decimal AvancoHh, List<MedicaoCalculo.ParcelaCalc> Parcelas);

    private async Task<Calc> Computar(Guid obraId, DateOnly de, DateOnly ate, Guid? condicaoOverride)
    {
        var itens = await db.ObraItens.Where(i => i.ObraId == obraId).OrderBy(i => i.Ordem).ToListAsync();
        // avanco atual: RDOs ate o fim do periodo (mesma regra do dashboard, max por item)
        var rdos = await db.Rdos.Where(r => r.ObraId == obraId && r.Data <= ate).ToListAsync();
        // %ini = maior %fim ja medido para o item (= ultima medicao)
        var medicoesAnteriores = await db.Medicoes.Where(x => x.ObraId == obraId).ToListAsync();

        decimal PctFim(ObraItem item)
        {
            decimal pct = 0m;
            foreach (var r in rdos)
                foreach (var s in r.Servicos.Where(s => s.ObraItemId == item.Id))
                    pct = Math.Max(pct, AvancoCalculo.PctItem(s, item));
            return pct;
        }
        decimal PctIni(Guid itemId)
        {
            decimal pct = 0m;
            foreach (var med in medicoesAnteriores)
                foreach (var mi in med.Itens.Where(mi => mi.ObraItemId == itemId))
                    pct = Math.Max(pct, mi.PctFim);
            return pct;
        }

        var calcItens = new List<ItemCalc>();
        foreach (var item in itens.Where(i => i.Valor is not null))
        {
            var valor = item.Valor ?? 0m;
            var pf = PctFim(item);
            var pi = Math.Min(PctIni(item.Id), pf); // %ini nunca ultrapassa %fim (evita periodo negativo)
            calcItens.Add(new ItemCalc(item.Id, item.Descricao, pi, pf, valor,
                MedicaoCalculo.MedidoPeriodo(pi, pf, valor), MedicaoCalculo.MedidoAcum(pf, valor)));
        }

        decimal somaValorItens = calcItens.Sum(i => i.Valor);
        decimal medidoAcum = calcItens.Sum(i => i.MedidoAcum);
        decimal valorPeriodo = calcItens.Sum(i => i.MedidoPeriodo);

        var plano = await db.FaturamentoPlanos.FirstOrDefaultAsync(p => p.ObraId == obraId);
        decimal valorContrato = plano is { ValorContrato: > 0 } ? plano.ValorContrato : somaValorItens;

        decimal pctFinanceiro = MedicaoCalculo.PctFinanceiro(medidoAcum, valorContrato);
        // avanco fisico ponderado por valor; fallback HH; fallback media simples
        decimal avancoHh = AvancoHh(itens, PctFim);
        decimal pctFisico = somaValorItens > 0 ? medidoAcum / somaValorItens
            : (itens.Any(i => i.HhPrevisto is > 0) ? avancoHh
            : (calcItens.Count > 0 ? calcItens.Average(i => i.PctFim) : 0m));

        // parcelas: condicao do request > condicao default do plano
        var condId = condicaoOverride ?? plano?.CondicaoPagamentoId;
        var cond = condId is { } cid ? await db.CondicoesPagamento.FirstOrDefaultAsync(c => c.Id == cid) : null;
        var defs = MedicaoCalculo.LerCondicao(cond?.Parcelas);
        var parcelas = MedicaoCalculo.Parcelas(defs, valorPeriodo, ate);

        return new Calc(valorContrato, calcItens, medidoAcum, valorPeriodo, pctFisico, pctFinanceiro, avancoHh, parcelas);
    }

    // avanco HH-ponderado (mesma logica do DashboardController) usando o %fim atual
    private static decimal AvancoHh(List<ObraItem> itens, Func<ObraItem, decimal> pctFim)
    {
        decimal somaHh = itens.Sum(i => i.HhPrevisto ?? 0);
        if (somaHh > 0) return itens.Sum(i => (i.HhPrevisto ?? 0) * pctFim(i)) / somaHh;
        decimal somaQtd = itens.Sum(i => i.QtdPrevista ?? 0);
        if (somaQtd > 0) return itens.Sum(i => (i.QtdPrevista ?? 0) * pctFim(i)) / somaQtd;
        return itens.Count > 0 ? itens.Average(pctFim) : 0m;
    }

    private static object Projetar(Calc c, DateOnly de, DateOnly ate) => new
    {
        de, ate,
        valorContrato = c.ValorContrato,
        medidoAcumulado = c.MedidoAcumulado,
        valorPeriodo = c.ValorPeriodo,
        pctFisico = c.PctFisico,
        pctFinanceiro = c.PctFinanceiro,
        avancoHh = c.AvancoHh,
        itens = c.Itens.Select(i => new { i.ObraItemId, i.Descricao, i.PctIni, i.PctFim, i.Valor, i.MedidoPeriodo, i.MedidoAcum }),
        parcelas = c.Parcelas.Select(p => new { p.Dias, p.Vencimento, p.Valor, p.Pct })
    };
}
