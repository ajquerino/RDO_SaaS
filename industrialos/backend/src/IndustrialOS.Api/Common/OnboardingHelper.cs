using System.Security.Cryptography;
using System.Text;
using IndustrialOS.Application.Auth;
using IndustrialOS.Application.Email;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace IndustrialOS.Api.Common;

/// <summary>Criação de empresa (tenant) compartilhada pelo cadastro assistido (SuperAdmin) e pelo
/// autocadastro público (/signup): tenant + empresa + admin (Funcao.Admin) + Assinatura trial 14d +
/// 61 funções de M.O. — tudo num SaveChanges. Não valida e-mail duplicado (o caller decide).</summary>
public static class OnboardingHelper
{
    public static async Task<(Tenant Tenant, Usuario Admin)> CriarEmpresaAsync(
        AppDbContext db, IPasswordHasher hasher,
        string nomeEmpresa, string? cnpj, string adminNome, string adminEmail, string adminSenha)
    {
        var tenant = new Tenant
        {
            Nome = nomeEmpresa.Trim(),
            Cnpj = string.IsNullOrWhiteSpace(cnpj) ? null : cnpj.Trim(),
            Plano = "trial",
            Status = "ativo",
        };
        db.Tenants.Add(tenant);

        // TenantId setado explicitamente (o Stamp só preenche quando vazio, e no /signup não há tenant atual).
        var empresa = new Empresa { TenantId = tenant.Id, RazaoSocial = nomeEmpresa.Trim() };
        db.Empresas.Add(empresa);

        var admin = new Usuario
        {
            TenantId = tenant.Id,
            EmpresaId = empresa.Id,
            Nome = adminNome.Trim(),
            Email = adminEmail.Trim().ToLowerInvariant(),
            SenhaHash = hasher.Hash(adminSenha),
            Funcao = Funcao.Admin,
        };
        db.Usuarios.Add(admin);

        db.Assinaturas.Add(new Assinatura
        {
            TenantId = tenant.Id,
            PlanoId = null,
            TrialAte = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14),
        });
        db.Funcoes.AddRange(DbSeeder.FuncoesParaTenant(tenant.Id));

        await db.SaveChangesAsync();
        return (tenant, admin);
    }

    /// <summary>Gera um token de definição de senha (RedefinicaoSenha, 24h) e envia o e-mail de
    /// boas-vindas com o link. Usado no cadastro assistido para o admin definir a própria senha.</summary>
    public static async Task EnviarConviteAsync(AppDbContext db, IEmailSender email, IConfiguration cfg, Usuario admin)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        db.RedefinicoesSenha.Add(new RedefinicaoSenha
        {
            UsuarioId = admin.Id,
            TokenHash = hash,
            ExpiraEm = DateTime.UtcNow.AddHours(24),
        });
        await db.SaveChangesAsync();

        var baseUrl = (cfg["App:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var link = $"{baseUrl}/redefinir-senha/{token}";
        var html = $"""
            <p>Bem-vindo ao IndustrialOS! Uma conta de administrador foi criada para você.</p>
            <p><a href="{link}">Clique aqui para definir sua senha</a> — o link expira em 24 horas.</p>
            <p>Depois, entre com o seu e-mail e a senha que você escolher.</p>
            """;
        await email.EnviarAsync(admin.Email!, "Bem-vindo ao IndustrialOS — defina sua senha", html);
    }
}
