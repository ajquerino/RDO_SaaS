using IndustrialOS.Api.Common;
using IndustrialOS.Application.Auth;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IndustrialOS.Api.Controllers;

public record SignupRequest(string NomeEmpresa, string? Cnpj, string AdminNome, string AdminEmail, string AdminSenha);

/// <summary>Autocadastro público (sem login): cria a empresa + admin e já devolve os tokens de login.
/// Rota livre do filtro de inadimplência (sem tenant autenticado) e protegida por rate limit "auth".</summary>
[ApiController]
[Route("api/v1/signup")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public class SignupController(AppDbContext db, IPasswordHasher hasher, IJwtService jwt, SessaoService sessoes, IConfiguration cfg) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Signup([FromBody] SignupRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.NomeEmpresa) || string.IsNullOrWhiteSpace(r.AdminNome) ||
            string.IsNullOrWhiteSpace(r.AdminEmail) || string.IsNullOrWhiteSpace(r.AdminSenha))
            return BadRequest(new { erro = "Informe empresa, nome, e-mail e senha." });
        if (r.AdminSenha.Length < 6)
            return BadRequest(new { erro = "A senha precisa ter ao menos 6 caracteres." });

        var email = r.AdminEmail.Trim().ToLowerInvariant();
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
            return Conflict(new { erro = "Já existe uma conta com este e-mail." });

        var (_, admin) = await OnboardingHelper.CriarEmpresaAsync(
            db, hasher, r.NomeEmpresa, r.Cnpj, r.AdminNome, r.AdminEmail, r.AdminSenha);

        // Obra de exemplo (EAP + 6 RDOs) p/ a conta grátis já nascer preenchida. Best-effort: NUNCA
        // pode impedir o cadastro nem o auto-login (o SeedAsync já é idempotente e não lança).
        if (ContaExemploSeeder.Habilitado(cfg))
        {
            try { await ContaExemploSeeder.SeedAsync(db, admin.TenantId, admin.EmpresaId); }
            catch { /* ignora: seeding do exemplo não bloqueia o signup */ }
        }

        // Auto-login (1 sessão por usuário): define a sessão e devolve os tokens.
        admin.SessaoAtual = Guid.NewGuid();
        await db.SaveChangesAsync();
        sessoes.Atualizar(admin.Id, admin.SessaoAtual);

        var tokens = jwt.Gerar(admin);
        return Ok(new LoginResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiraEm,
            new UsuarioDto(admin.Id, admin.Nome, admin.Email, admin.Funcao.ToString())));
    }
}
