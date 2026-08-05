using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record ClienteRequest(string Nome, string? Cnpj, string? Contato, string? Endereco);

[ApiController]
[Route("api/v1/clientes")]
[Authorize(Roles = "Planejador,Gestor,Admin")]
public class ClientesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() =>
        Ok(await db.Clientes.OrderBy(c => c.Nome).ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id) =>
        await db.Clientes.FindAsync(id) is { } c ? Ok(c) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] ClienteRequest req)
    {
        var c = new Cliente { Nome = req.Nome, Cnpj = req.Cnpj, Contato = req.Contato, Endereco = req.Endereco };
        db.Clientes.Add(c);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = c.Id }, c);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] ClienteRequest req)
    {
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return NotFound();
        (c.Nome, c.Cnpj, c.Contato, c.Endereco) = (req.Nome, req.Cnpj, req.Contato, req.Endereco);
        await db.SaveChangesAsync();
        return Ok(c);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Apagar(Guid id)
    {
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return NotFound();
        c.DeletadoEm = DateTime.UtcNow; // soft delete
        await db.SaveChangesAsync();
        return NoContent();
    }
}
