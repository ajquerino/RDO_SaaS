using System.Security.Claims;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record EventoDto(Guid? Id, string Tipo, string Base, decimal? Percentual, decimal? Valor,
    string? Gatilho, DateOnly? DataPrevista, string? Recorrencia, Guid? CondicaoPagamentoId, int Ordem, string? Descricao);
public record FaturamentoRequest(string Nome, decimal ValorContrato, Guid? CondicaoPagamentoId, EventoDto[]? Eventos);

/// <summary>Plano de faturamento por obra (plano + eventos/marcos). So Planejador/Gestor/Admin.</summary>
[ApiController]
[Route("api/v1/obras/{obraId:guid}/faturamento")]
[Authorize(Roles = "Planejador,Gestor,Admin")]
public class FaturamentoController(AppDbContext db) : ControllerBase
{
    private bool VeTodasObras => User.IsInRole("Gestor") || User.IsInRole("Admin") || User.IsInRole("Planejador");
    private Guid UsuarioId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private async Task<bool> PodeVerObra(Guid obraId)
    {
        if (await db.Obras.FindAsync(obraId) is null) return false;
        if (VeTodasObras) return true;
        return await db.UsuarioObras.AnyAsync(x => x.ObraId == obraId && x.UsuarioId == UsuarioId);
    }

    [HttpGet]
    public async Task<IActionResult> Obter(Guid obraId)
    {
        if (!await PodeVerObra(obraId)) return NotFound();
        var plano = await db.FaturamentoPlanos.FirstOrDefaultAsync(p => p.ObraId == obraId);
        if (plano is null) return Ok(new { plano = (object?)null, eventos = Array.Empty<object>() });

        var eventos = await db.FaturamentoEventos.Where(e => e.FaturamentoPlanoId == plano.Id)
            .OrderBy(e => e.Ordem).ToListAsync();
        return Ok(new { plano = new { plano.Id, plano.Nome, plano.ValorContrato, plano.CondicaoPagamentoId }, eventos });
    }

    // Cria ou atualiza o plano da obra (1 plano por obra) + substitui a lista de eventos.
    [HttpPost, HttpPut]
    public async Task<IActionResult> Salvar(Guid obraId, [FromBody] FaturamentoRequest r)
    {
        if (!await PodeVerObra(obraId)) return NotFound();

        var plano = await db.FaturamentoPlanos.FirstOrDefaultAsync(p => p.ObraId == obraId);
        if (plano is null)
        {
            plano = new FaturamentoPlano { ObraId = obraId };
            db.FaturamentoPlanos.Add(plano);
        }
        plano.Nome = string.IsNullOrWhiteSpace(r.Nome) ? "Plano de faturamento" : r.Nome.Trim();
        plano.ValorContrato = r.ValorContrato;
        plano.CondicaoPagamentoId = r.CondicaoPagamentoId;
        await db.SaveChangesAsync(); // garante plano.Id para os eventos

        // substitui a lista de eventos pelo conteudo enviado
        var antigos = await db.FaturamentoEventos.Where(e => e.FaturamentoPlanoId == plano.Id).ToListAsync();
        db.FaturamentoEventos.RemoveRange(antigos);
        int ordem = 0;
        foreach (var e in r.Eventos ?? [])
            db.FaturamentoEventos.Add(new FaturamentoEvento
            {
                FaturamentoPlanoId = plano.Id,
                Tipo = string.IsNullOrWhiteSpace(e.Tipo) ? "outro" : e.Tipo,
                Base = string.IsNullOrWhiteSpace(e.Base) ? "percentual" : e.Base,
                Percentual = e.Percentual, Valor = e.Valor, Gatilho = e.Gatilho,
                DataPrevista = e.DataPrevista, Recorrencia = e.Recorrencia,
                CondicaoPagamentoId = e.CondicaoPagamentoId, Ordem = e.Ordem == 0 ? ++ordem : e.Ordem,
                Descricao = e.Descricao
            });
        await db.SaveChangesAsync();

        return await Obter(obraId);
    }
}
