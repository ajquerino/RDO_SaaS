using System.Text.Json;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record ParcelaDefDto(int Dias, decimal? Pct);
public record CondicaoPagamentoRequest(string Nome, ParcelaDefDto[]? Parcelas);

/// <summary>Condicoes de pagamento reutilizaveis (ex.: "21/42"). So Planejador/Gestor/Admin.</summary>
[ApiController]
[Route("api/v1/condicoes-pagamento")]
[Authorize(Roles = "Planejador,Gestor,Admin")]
public class CondicoesPagamentoController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() =>
        Ok(await db.CondicoesPagamento.OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.Parcelas }).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CondicaoPagamentoRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest(new { erro = "Informe o nome." });
        var c = new CondicaoPagamento { Nome = r.Nome.Trim(), Parcelas = Serializar(r.Parcelas) };
        db.CondicoesPagamento.Add(c);
        await db.SaveChangesAsync();
        return Ok(new { c.Id, c.Nome, c.Parcelas });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] CondicaoPagamentoRequest r)
    {
        var c = await db.CondicoesPagamento.FindAsync(id);
        if (c is null) return NotFound();
        c.Nome = r.Nome.Trim();
        c.Parcelas = Serializar(r.Parcelas);
        await db.SaveChangesAsync();
        return Ok(new { c.Id, c.Nome, c.Parcelas });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Apagar(Guid id)
    {
        var c = await db.CondicoesPagamento.FindAsync(id);
        if (c is null) return NotFound();
        c.DeletadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // Grava a lista [{dias[, pct]}] em jsonb — pct e opcional (se omitido, divide igualmente no calculo).
    private static string Serializar(ParcelaDefDto[]? parcelas)
    {
        var limpa = (parcelas ?? []).Select(p =>
        {
            var d = new Dictionary<string, object> { ["dias"] = p.Dias };
            if (p.Pct is { } pct) d["pct"] = pct;
            return d;
        });
        return JsonSerializer.Serialize(limpa);
    }
}
