using IndustrialOS.Application.Auth;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")] // 10 req/min por IP em login/refresh
public class AuthController(AppDbContext db, IPasswordHasher hasher, IJwtService jwt) : ControllerBase
{
    /// <summary>Login por e-mail OU nome + senha. Pre-tenant: ignora o filtro de tenant.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var chave = req.EmailOuNome.Trim();
        var user = await db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Ativo && u.DeletadoEm == null &&
                (u.Email == chave || u.Nome == chave));

        if (user is null || !hasher.Verify(req.Senha, user.SenhaHash))
            return Unauthorized(new { erro = "Credenciais invalidas." });

        // Empresa-cliente suspensa: bloqueia o login dos seus usuários (não afeta o super-admin).
        if (user.Funcao != Funcao.SuperAdmin &&
            await db.Tenants.AnyAsync(t => t.Id == user.TenantId && t.Status == "suspenso"))
            return StatusCode(403, new { erro = "Empresa suspensa. Contate o suporte." });

        var tokens = jwt.Gerar(user);
        user.UltimoLogin = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new LoginResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiraEm,
            new UsuarioDto(user.Id, user.Nome, user.Email, user.Funcao.ToString())));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var id = jwt.ValidarRefresh(req.RefreshToken);
        if (id is null) return Unauthorized(new { erro = "Refresh token invalido." });

        var user = await db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.Ativo && u.DeletadoEm == null);
        if (user is null) return Unauthorized();

        var tokens = jwt.Gerar(user);
        return Ok(new { tokens.AccessToken, tokens.RefreshToken, tokens.ExpiraEm });
    }
}
