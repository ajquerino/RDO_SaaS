using IndustrialOS.Application.Auth;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

public record NovaEmpresaRequest(string NomeEmpresa, string? Cnpj, string AdminNome, string AdminEmail, string AdminSenha);
public record EditarEmpresaRequest(string Nome, string? Cnpj);
public record StatusTenantRequest(string Status);
public record PlanoPlataformaRequest(string Nome, int? LimiteObras, int? LimiteUsuarios, decimal? PrecoMensal);
public record PlanoTenantRequest(Guid? PlanoId);

/// <summary>Console de PLATAFORMA (dono do SaaS). ÚNICO ponto de acesso cross-tenant, e SOMENTE aqui:
/// todo acesso a dados de empresas usa <c>.IgnoreQueryFilters()</c> EXPLÍCITO + [Authorize(SuperAdmin)].
/// O filtro global e o TenantMiddleware seguem intactos para o fluxo normal dos tenants.</summary>
[ApiController]
[Route("api/v1/plataforma")]
[Authorize(Roles = "SuperAdmin")]
public class PlataformaController(AppDbContext db, IPasswordHasher hasher) : ControllerBase
{
    // ---- Empresas (tenants) ----
    [HttpGet("tenants")]
    public async Task<IActionResult> Tenants()
    {
        // Exclui o tenant de sistema (a própria plataforma) e empresas excluídas (soft delete).
        var tenants = await db.Tenants
            .Where(t => !t.EhSistema && t.DeletadoEm == null)
            .OrderBy(t => t.Nome).ToListAsync();

        var obras = await db.Obras.IgnoreQueryFilters().Where(o => o.DeletadoEm == null)
            .GroupBy(o => o.TenantId).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        var usuarios = await db.Usuarios.IgnoreQueryFilters().Where(u => u.DeletadoEm == null)
            .GroupBy(u => u.TenantId).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        var obrasMap = obras.ToDictionary(x => x.Key, x => x.N);
        var usuariosMap = usuarios.ToDictionary(x => x.Key, x => x.N);

        // Plano atual de cada empresa (fonte de verdade = PlanoId, join com o catálogo de Planos).
        var planos = await db.Planos.IgnoreQueryFilters().ToDictionaryAsync(p => p.Id);

        return Ok(tenants.Select(t =>
        {
            var plano = t.PlanoId is { } pid && planos.TryGetValue(pid, out var p) ? p : null;
            return new
            {
                t.Id, t.Nome, t.Cnpj, t.Plano, t.Status, t.CriadoEm,
                t.PlanoId,
                planoNome = plano?.Nome,
                nObras = obrasMap.GetValueOrDefault(t.Id),
                nUsuarios = usuariosMap.GetValueOrDefault(t.Id),
                limiteObras = plano?.LimiteObras,
                limiteUsuarios = plano?.LimiteUsuarios
            };
        }));
    }

    [HttpGet("tenants/{id:guid}")]
    public async Task<IActionResult> Tenant(Guid id)
    {
        var t = await db.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null || t.EhSistema) return NotFound();

        var nObras = await db.Obras.IgnoreQueryFilters().CountAsync(o => o.TenantId == id && o.DeletadoEm == null);
        var nRdos = await db.Rdos.IgnoreQueryFilters().CountAsync(r => r.TenantId == id && r.DeletadoEm == null);
        var nUsuarios = await db.Usuarios.IgnoreQueryFilters().CountAsync(u => u.TenantId == id && u.DeletadoEm == null);

        return Ok(new { t.Id, t.Nome, t.Cnpj, t.Plano, t.Status, t.CriadoEm, nObras, nRdos, nUsuarios });
    }

    [HttpPut("tenants/{id:guid}/status")]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] StatusTenantRequest req)
    {
        var status = req.Status?.Trim().ToLowerInvariant();
        if (status != "ativo" && status != "suspenso")
            return BadRequest(new { erro = "Status deve ser 'ativo' ou 'suspenso'." });

        var t = await db.Tenants.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null || t.EhSistema) return NotFound();

        t.Status = status;
        await db.SaveChangesAsync();
        return Ok(new { t.Id, t.Status });
    }

    [HttpPut("tenants/{id:guid}")]
    public async Task<IActionResult> EditarEmpresa(Guid id, [FromBody] EditarEmpresaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Informe o nome da empresa." });

        var t = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.DeletadoEm == null);
        if (t is null || t.EhSistema) return NotFound();

        t.Nome = req.Nome.Trim();
        t.Cnpj = string.IsNullOrWhiteSpace(req.Cnpj) ? null : req.Cnpj.Trim();

        // Mantém a razão social da empresa principal sincronizada com o nome do tenant.
        var empresa = await db.Empresas.IgnoreQueryFilters()
            .Where(e => e.TenantId == id && e.DeletadoEm == null)
            .OrderBy(e => e.CriadoEm).FirstOrDefaultAsync();
        if (empresa is not null) empresa.RazaoSocial = t.Nome;

        await db.SaveChangesAsync();
        return Ok(new { t.Id, t.Nome, t.Cnpj });
    }

    [HttpDelete("tenants/{id:guid}")]
    public async Task<IActionResult> ExcluirEmpresa(Guid id)
    {
        var t = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.DeletadoEm == null);
        if (t is null || t.EhSistema) return NotFound();

        // Soft delete: a empresa some do console e o login é bloqueado, mas os dados
        // (obras, RDOs, usuários) são preservados — exclusão reversível pelo banco.
        t.DeletadoEm = DateTime.UtcNow;
        t.Status = "suspenso";
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("tenants/{id:guid}/plano")]
    public async Task<IActionResult> AtribuirPlano(Guid id, [FromBody] PlanoTenantRequest req)
    {
        // Super-admin está fora do tenant: IgnoreQueryFilters explícito para achar empresa e plano.
        var t = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null || t.EhSistema) return NotFound();

        string? planoNome = null;
        if (req.PlanoId is { } pid)
        {
            var plano = await db.Planos.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == pid);
            if (plano is null) return BadRequest(new { erro = "Plano não encontrado." });
            t.PlanoId = pid;
            t.Plano = plano.Nome;   // mantém o campo string legado sincronizado (fonte de verdade = PlanoId)
            planoNome = plano.Nome;
        }
        else
        {
            t.PlanoId = null;       // "sem plano"
            t.Plano = "trial";
        }

        await db.SaveChangesAsync();
        return Ok(new { t.Id, t.PlanoId, planoNome });
    }

    [HttpPost("tenants")]
    public async Task<IActionResult> CriarTenant([FromBody] NovaEmpresaRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.NomeEmpresa) || string.IsNullOrWhiteSpace(r.AdminNome) ||
            string.IsNullOrWhiteSpace(r.AdminEmail) || string.IsNullOrWhiteSpace(r.AdminSenha))
            return BadRequest(new { erro = "Informe empresa, nome, e-mail e senha do admin." });

        var email = r.AdminEmail.Trim().ToLowerInvariant();
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
            return Conflict(new { erro = "Já existe um usuário com este e-mail." });

        var tenant = new Tenant
        {
            Nome = r.NomeEmpresa.Trim(),
            Cnpj = string.IsNullOrWhiteSpace(r.Cnpj) ? null : r.Cnpj.Trim(),
            Plano = "trial",
            Status = "ativo",
        };
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

    // ---- Planos (globais / do SaaS) ----
    [HttpGet("planos")]
    public async Task<IActionResult> Planos() =>
        Ok(await db.Planos.OrderBy(p => p.PrecoMensal ?? 0).ToListAsync());

    [HttpPost("planos")]
    public async Task<IActionResult> CriarPlano([FromBody] PlanoPlataformaRequest r)
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

    [HttpPut("planos/{id:guid}")]
    public async Task<IActionResult> EditarPlano(Guid id, [FromBody] PlanoPlataformaRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest(new { erro = "Informe o nome do plano." });
        var p = await db.Planos.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        p.Nome = r.Nome.Trim();
        p.LimiteObras = r.LimiteObras;
        p.LimiteUsuarios = r.LimiteUsuarios;
        p.PrecoMensal = r.PrecoMensal;

        // Mantém o nome do plano (campo string legado) sincronizado nas empresas que o usam.
        await db.Tenants.IgnoreQueryFilters().Where(t => t.PlanoId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Plano, p.Nome));

        await db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpDelete("planos/{id:guid}")]
    public async Task<IActionResult> ExcluirPlano(Guid id)
    {
        var p = await db.Planos.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        // Bloqueia se alguma empresa (não excluída) ainda usa o plano — evita plano órfão.
        var emUso = await db.Tenants.IgnoreQueryFilters()
            .CountAsync(t => t.PlanoId == id && t.DeletadoEm == null);
        if (emUso > 0)
            return Conflict(new { erro = $"{emUso} empresa(s) usam este plano. Troque o plano delas antes de excluir." });

        db.Planos.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }
    // TODO(billing): cobrança/checkout/gateway/trial NÃO implementados — dependem de decisão do usuário.

    // ---- Métricas globais ----
    [HttpGet("metricas")]
    public async Task<IActionResult> Metricas()
    {
        var totalTenants = await db.Tenants.CountAsync(t => !t.EhSistema && t.DeletadoEm == null);
        var tenantsAtivos = await db.Tenants.CountAsync(t => !t.EhSistema && t.DeletadoEm == null && t.Status == "ativo");
        var totalObras = await db.Obras.IgnoreQueryFilters().CountAsync(o => o.DeletadoEm == null);
        var totalRdos = await db.Rdos.IgnoreQueryFilters().CountAsync(r => r.DeletadoEm == null);
        var totalUsuarios = await db.Usuarios.IgnoreQueryFilters()
            .CountAsync(u => u.DeletadoEm == null && u.Funcao != Funcao.SuperAdmin);

        return Ok(new { totalTenants, tenantsAtivos, totalObras, totalRdos, totalUsuarios });
    }
}
