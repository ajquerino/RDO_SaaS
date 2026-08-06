using System.Security.Claims;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

/// <summary>LGPD: o próprio usuário exporta seus dados pessoais e registra consentimento.</summary>
[ApiController]
[Route("api/v1/lgpd")]
[Authorize]
public class LgpdController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    [HttpGet("meus-dados")]
    public async Task<IActionResult> MeusDados()
    {
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == UsuarioId);
        if (u is null) return NotFound();

        var obras = await db.UsuarioObras.Where(x => x.UsuarioId == u.Id).Select(x => x.ObraId).ToListAsync();
        return Ok(new
        {
            u.Id, u.Nome, u.Email, Funcao = u.Funcao.ToString(), u.Ativo, u.UltimoLogin,
            u.ConsentimentoLgpd, u.ConsentimentoEm, u.CriadoEm,
            obrasVinculadas = obras
        });
    }

    [HttpPost("consentimento")]
    public async Task<IActionResult> Consentir()
    {
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == UsuarioId);
        if (u is null) return NotFound();
        u.ConsentimentoLgpd = true;
        u.ConsentimentoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { u.ConsentimentoLgpd, u.ConsentimentoEm });
    }
}
