using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record EquipamentoRequest(string Nome, string? Tipo, string? ProprioLocado,
    decimal? Horimetro, decimal? CustoHora, string? StatusManutencao);

/// <summary>Catalogo de equipamentos por tenant. Todos leem (usado no RDO); so PLAN/GES/ADM editam.</summary>
[ApiController]
[Route("api/v1/equipamentos")]
[Authorize]
public class EquipamentosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() =>
        Ok(await db.Equipamentos.Where(e => e.Ativo).OrderBy(e => e.Nome)
            .Select(e => new { e.Id, e.Nome, e.Tipo, e.ProprioLocado, e.Horimetro, e.CustoHora, e.StatusManutencao }).ToListAsync());

    [HttpPost]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Criar([FromBody] EquipamentoRequest req)
    {
        var nome = req.Nome?.Trim();
        if (string.IsNullOrWhiteSpace(nome)) return BadRequest(new { erro = "Nome vazio." });
        if (await db.Equipamentos.AnyAsync(e => e.Nome == nome)) return Conflict(new { erro = "Equipamento já existe." });

        var e = new Equipamento
        {
            Nome = nome,
            Tipo = req.Tipo,
            ProprioLocado = req.ProprioLocado == "locado" ? "locado" : "proprio",
            Horimetro = req.Horimetro, CustoHora = req.CustoHora, StatusManutencao = req.StatusManutencao,
        };
        db.Equipamentos.Add(e);
        await db.SaveChangesAsync();
        return Ok(new { e.Id, e.Nome, e.Tipo, e.ProprioLocado, e.Horimetro, e.CustoHora, e.StatusManutencao });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EquipamentoRequest req)
    {
        var e = await db.Equipamentos.FindAsync(id);
        if (e is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Nome)) e.Nome = req.Nome.Trim();
        e.Tipo = req.Tipo;
        e.ProprioLocado = req.ProprioLocado == "locado" ? "locado" : "proprio";
        e.Horimetro = req.Horimetro; e.CustoHora = req.CustoHora; e.StatusManutencao = req.StatusManutencao;
        await db.SaveChangesAsync();
        return Ok(new { e.Id, e.Nome, e.Tipo, e.ProprioLocado, e.Horimetro, e.CustoHora, e.StatusManutencao });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Planejador,Gestor,Admin")]
    public async Task<IActionResult> Apagar(Guid id)
    {
        var e = await db.Equipamentos.FindAsync(id);
        if (e is null) return NotFound();
        e.DeletadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
