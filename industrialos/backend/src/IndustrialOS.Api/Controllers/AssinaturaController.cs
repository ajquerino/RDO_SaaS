using IndustrialOS.Application.Common;
using IndustrialOS.Domain.Services;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

/// <summary>Status da assinatura do tenant atual — alimenta o banner e o modo somente-leitura do front.</summary>
[ApiController]
[Route("api/v1/assinatura")]
[Authorize]
public class AssinaturaController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet("minha")]
    public async Task<IActionResult> Minha()
    {
        if (tenant.TenantId is not Guid tid)
            return Ok(new { estado = nameof(EstadoAssinatura.SemAssinatura), bloqueada = false });

        var a = await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tid);
        var e = AssinaturaCalculo.Avaliar(a, DateOnly.FromDateTime(DateTime.UtcNow));

        string? planoNome = null;
        if (a?.PlanoId is Guid pid)
            planoNome = await db.Planos.IgnoreQueryFilters().Where(p => p.Id == pid).Select(p => p.Nome).FirstOrDefaultAsync();

        return Ok(new
        {
            estado = e.Estado.ToString(),
            bloqueada = e.Bloqueada,
            diasParaVencer = e.DiasParaVencer,
            diasAtraso = e.DiasAtraso,
            avisoNivel = e.AvisoNivel,
            vencimentoEm = e.VencimentoEm,
            planoNome,
        });
    }
}
