using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record FuncaoRequest(string Nome, string? Categoria, decimal? CustoHh);

[ApiController]
[Route("api/v1/funcoes")]
[Authorize]
public class FuncoesController(AppDbContext db) : ControllerBase
{
    // Todos os usuários autenticados podem listar (usado no efetivo do RDO).
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? categoria)
    {
        var q = db.Funcoes.Where(f => f.Ativo);
        if (!string.IsNullOrWhiteSpace(categoria)) q = q.Where(f => f.Categoria == categoria);
        return Ok(await q.OrderBy(f => f.Categoria).ThenBy(f => f.Nome)
            .Select(f => new { f.Id, f.Nome, f.Categoria, f.CustoHh }).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "Gestor,Admin")]
    public async Task<IActionResult> Criar([FromBody] FuncaoRequest req)
    {
        var nome = req.Nome.Trim();
        if (string.IsNullOrWhiteSpace(nome)) return BadRequest(new { erro = "Nome vazio." });
        if (await db.Funcoes.AnyAsync(f => f.Nome == nome)) return Conflict(new { erro = "Função já existe." });

        var cat = req.Categoria == "Indireta" ? "Indireta" : "Direta";
        var f = new FuncaoMaoObra { Nome = nome, Categoria = cat, CustoHh = req.CustoHh };
        db.Funcoes.Add(f);
        await db.SaveChangesAsync();
        return Ok(new { f.Id, f.Nome, f.Categoria, f.CustoHh });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Gestor,Admin")]
    public async Task<IActionResult> Apagar(Guid id)
    {
        var f = await db.Funcoes.FindAsync(id);
        if (f is null) return NotFound();
        f.DeletadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
