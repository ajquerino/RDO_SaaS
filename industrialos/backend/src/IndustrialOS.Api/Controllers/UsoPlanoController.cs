using IndustrialOS.Api.Common;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialOS.Api.Controllers;

/// <summary>Uso do plano do tenant corrente (obras/usuários vs limites). Só leitura — é aviso, não bloqueio.</summary>
[ApiController]
[Route("api/v1/uso-plano")]
[Authorize]
public class UsoPlanoController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obter()
    {
        var u = await UsoPlanoCalc.CalcularAsync(db);
        return Ok(new
        {
            nObras = u.NObras,
            limiteObras = u.LimiteObras,
            nUsuarios = u.NUsuarios,
            limiteUsuarios = u.LimiteUsuarios,
            plano = u.Plano
        });
    }
}
