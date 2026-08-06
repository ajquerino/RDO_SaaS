using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record PlanoRequest(string Nome, int? LimiteObras, int? LimiteUsuarios, decimal? PrecoMensal);

/// <summary>Catálogo de planos do SaaS — APENAS o modelo. Nenhuma cobrança implementada.</summary>
// TODO(billing): checkout, gateway de pagamento (Stripe/PagSeguro/Mercado Pago?), webhook e trial
// NÃO estão implementados — dependem de decisão do usuário (provedor? planos/preços? regra de trial?).
[ApiController]
[Route("api/v1/planos")]
[Authorize(Roles = "Admin")]
public class PlanosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar() =>
        Ok(await db.Planos.OrderBy(p => p.PrecoMensal ?? 0).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] PlanoRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest(new { erro = "Informe o nome do plano." });
        var p = new Plano
        {
            Nome = r.Nome.Trim(),
            LimiteObras = r.LimiteObras,
            LimiteUsuarios = r.LimiteUsuarios,
            PrecoMensal = r.PrecoMensal,
        };
        db.Planos.Add(p);
        await db.SaveChangesAsync();
        return Ok(p);
    }
}
