using IndustrialOS.Application.Common;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

/// <summary>Trilha de auditoria (só Admin). Escopo ao tenant corrente (Auditoria não tem filtro global).</summary>
[ApiController]
[Route("api/v1/auditoria")]
[Authorize(Roles = "Admin")]
public class AuditoriaController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int page = 1, [FromQuery] int size = 50,
        [FromQuery] string? entidade = null, [FromQuery] DateTime? de = null, [FromQuery] DateTime? ate = null)
    {
        page = Math.Max(1, page);
        size = Math.Clamp(size, 1, 200);

        var q = db.Auditorias.Where(a => a.TenantId == tenant.TenantId);
        if (!string.IsNullOrWhiteSpace(entidade)) q = q.Where(a => a.Entidade == entidade);
        if (de is { } d) q = q.Where(a => a.CriadoEm >= d);
        if (ate is { } dataAte) q = q.Where(a => a.CriadoEm <= dataAte);

        var total = await q.CountAsync();
        var itens = await q.OrderByDescending(a => a.CriadoEm)
            .Skip((page - 1) * size).Take(size)
            .Select(a => new { a.Id, a.Acao, a.Entidade, a.EntidadeId, a.UsuarioId, a.Detalhe, a.CriadoEm })
            .ToListAsync();

        return Ok(new { total, page, size, itens });
    }
}
