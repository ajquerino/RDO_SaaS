using IndustrialOS.Application.Auth;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record NovoTenantRequest(string NomeEmpresa, string AdminNome, string AdminEmail, string AdminSenha);

/// <summary>Onboarding de tenant: cria tenant + empresa + primeiro usuário admin.
/// Protegido (Roles=Admin) — NÃO é signup público (isso seria sensível; pedir ao usuário antes).</summary>
[ApiController]
[Route("api/v1/tenants")]
[Authorize(Roles = "SuperAdmin")] // criar empresas/tenants: só o dono da plataforma
public class TenantsController(AppDbContext db, IPasswordHasher hasher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] NovoTenantRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.NomeEmpresa) || string.IsNullOrWhiteSpace(r.AdminNome) ||
            string.IsNullOrWhiteSpace(r.AdminEmail) || string.IsNullOrWhiteSpace(r.AdminSenha))
            return BadRequest(new { erro = "Informe empresa, nome, e-mail e senha do admin." });

        var email = r.AdminEmail.Trim().ToLowerInvariant();

        var tenant = new Tenant { Nome = r.NomeEmpresa.Trim(), Plano = "trial", Status = "ativo" };
        db.Tenants.Add(tenant);

        // TenantId setado explicitamente com o novo tenant (o Stamp só preenche quando vazio).
        var empresa = new Empresa { TenantId = tenant.Id, RazaoSocial = r.NomeEmpresa.Trim() };
        db.Empresas.Add(empresa);

        var admin = new Usuario
        {
            TenantId = tenant.Id,
            EmpresaId = empresa.Id,
            Nome = r.AdminNome.Trim(),
            Email = email,
            SenhaHash = hasher.Hash(r.AdminSenha),
            Funcao = Funcao.Admin,
        };
        db.Usuarios.Add(admin);

        await db.SaveChangesAsync();
        return Ok(new { tenantId = tenant.Id, tenant.Nome, adminId = admin.Id, admin.Email });
    }
}
