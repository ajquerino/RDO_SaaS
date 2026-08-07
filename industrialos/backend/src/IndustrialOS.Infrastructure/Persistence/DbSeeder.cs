using IndustrialOS.Application.Auth;
using IndustrialOS.Application.Common;
using IndustrialOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IndustrialOS.Infrastructure.Persistence;

/// <summary>Seed do banco. Dividido em:
/// - <see cref="SeedSistemaAsync"/>: roda em TODOS os ambientes (tenant de sistema + super-admin).
/// - <see cref="SeedDemoAsync"/>: roda SOMENTE em Development (empresa demo + admin + funções).
/// O catálogo <see cref="FuncoesPadrao"/> é público para ser reusado ao criar empresa nova.</summary>
public static class DbSeeder
{
    // Funções reais (categoria: Direta = executa a obra; Indireta = apoio/gestão).
    // Público: reutilizado ao criar uma empresa nova (PlataformaController.CriarTenant),
    // para que todo tenant novo já nasça com o catálogo de mão de obra.
    public static readonly (string Nome, string Categoria)[] FuncoesPadrao =
    [
        // ---- MÃO DE OBRA DIRETA ----
        ("APRENDIZ", "Direta"), ("AUX.MONTAGEM", "Direta"), ("AUX.MONTAGEM I", "Direta"),
        ("CALDEIREIRO", "Direta"), ("CALDEIREIRO I", "Direta"), ("CALDEIREIRO II", "Direta"),
        ("CALDEIREIRO III", "Direta"), ("CALDEIREIRO IV", "Direta"), ("JATISTA", "Direta"),
        ("MECÂNICO INDUSTRIAL", "Direta"), ("MECÂNICO INDUSTRIAL IV", "Direta"), ("MECÂNICO LIDER", "Direta"),
        ("MONTADOR", "Direta"), ("MONTADOR I", "Direta"), ("MONTADOR II", "Direta"), ("MONTADOR III", "Direta"),
        ("MONTADOR LIDER I", "Direta"), ("MONTADOR V", "Direta"), ("MONTADOR VI", "Direta"),
        ("OPERADOR DE EMPILHADEIRA", "Direta"), ("OPERADOR DE GUINDASTE", "Direta"), ("OPERADOR DE GUINDASTE I", "Direta"),
        ("OPERADOR DE MUNCK", "Direta"), ("OPERADOR DE MUNCK I", "Direta"), ("OPERADOR DE MUNCK II", "Direta"),
        ("OPERADOR DE PLASMA", "Direta"), ("PEDREIRO", "Direta"), ("PEDREIRO I", "Direta"),
        ("PINTOR", "Direta"), ("PINTOR I", "Direta"), ("PINTOR II", "Direta"), ("PINTOR III", "Direta"),
        ("SOLDADOR", "Direta"), ("SOLDADOR I", "Direta"), ("SOLDADOR II", "Direta"),
        ("SOLDADOR III", "Direta"), ("SOLDADOR IV", "Direta"), ("SOLDADOR V", "Direta"),
        // ---- MÃO DE OBRA INDIRETA ----
        ("ALMOXARIFE", "Indireta"), ("ANALISTA DE PLANEJAMENTOS", "Indireta"), ("ASSISTENTE DE PLANEJAMENTO", "Indireta"),
        ("ASSISTENTE DE PROJETO", "Indireta"), ("AUX.ADM", "Indireta"), ("AUX.ADM I", "Indireta"),
        ("AUX.LIMPEZA", "Indireta"), ("AUX.PLANEJAMENTO", "Indireta"), ("COMPRADORA", "Indireta"),
        ("COZINHEIRA", "Indireta"), ("ELETRICISTA", "Indireta"), ("ELETRICISTA I", "Indireta"), ("ELETRICISTA II", "Indireta"),
        ("ENCARREGADO", "Indireta"), ("ENCARREGADO DE MONTAGEM II", "Indireta"), ("ENGENHEIRO DE PRODUÇÃO", "Indireta"),
        ("ENGENHEIRO MECÂNICO", "Indireta"), ("MECÂNICO DE MANUTENÇÃO", "Indireta"), ("ORÇAMENTISTA", "Indireta"),
        ("TECNICO DE MATERIAIS", "Indireta"), ("TST", "Indireta"), ("TST I", "Indireta"), ("TST II", "Indireta")
    ];

    /// <summary>Cria as 61 funções padrão para um tenant (sem salvar — o caller faz o SaveChanges).
    /// Usado no seed demo e ao criar uma empresa nova pelo console do super-admin.</summary>
    public static IEnumerable<FuncaoMaoObra> FuncoesParaTenant(Guid tenantId) =>
        FuncoesPadrao.Select(f => new FuncaoMaoObra { TenantId = tenantId, Nome = f.Nome, Categoria = f.Categoria });

    /// <summary>Roda SEMPRE (todos os ambientes): tenant de SISTEMA "Plataforma" + usuário super-admin.
    /// O super-admin mora no tenant de sistema, então NÃO enxerga dado de nenhuma empresa-cliente.
    /// E-mail/senha vêm da config (Seed:SuperAdminEmail / Seed:SuperAdminSenha); o fallback
    /// super@demo.com / super123 é só conveniência de dev — em produção defina os dois na .env.</summary>
    public static async Task SeedSistemaAsync(AppDbContext db, IPasswordHasher hasher, IConfiguration config)
    {
        var sistema = await db.Tenants.FirstOrDefaultAsync(x => x.EhSistema);
        if (sistema is null)
        {
            sistema = new Tenant { Nome = "Plataforma", EhSistema = true, Plano = "sistema", Status = "sistema" };
            db.Tenants.Add(sistema);
            await db.SaveChangesAsync();
        }

        var email = (config["Seed:SuperAdminEmail"] ?? "super@demo.com").Trim().ToLowerInvariant();
        var senha = config["Seed:SuperAdminSenha"] ?? "super123";

        var super = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Funcao == Funcao.SuperAdmin);
        if (super is null)
        {
            db.Usuarios.Add(new Usuario
            {
                TenantId = sistema.Id,
                Nome = "Super Admin (plataforma)",
                Email = email,
                SenhaHash = hasher.Hash(senha),
                Funcao = Funcao.SuperAdmin
            });
            await db.SaveChangesAsync();
        }
        else if (super.TenantId != sistema.Id)
        {
            // Migra um super-admin que porventura tenha nascido fora do tenant de sistema.
            super.TenantId = sistema.Id;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Roda SOMENTE em Development: cria a "Empresa Demo" + admin@demo.com + as 61 funções
    /// no tenant demo. NUNCA deve rodar em produção (nada de dados fictícios lá).</summary>
    public static async Task SeedDemoAsync(AppDbContext db, ITenantContext tenant, IPasswordHasher hasher)
    {
        var t = await db.Tenants.FirstOrDefaultAsync(x => !x.EhSistema);
        if (t is null)
        {
            t = new Tenant { Nome = "Empresa Demo", Plano = "trial" };
            db.Tenants.Add(t);
            await db.SaveChangesAsync();

            tenant.Set(t.Id);

            var empresa = new Empresa { TenantId = t.Id, RazaoSocial = "Empresa Demo LTDA", NomeFantasia = "Demo" };
            db.Empresas.Add(empresa);
            db.Usuarios.Add(new Usuario
            {
                TenantId = t.Id,
                EmpresaId = empresa.Id,
                Nome = "Administrador",
                Email = "admin@demo.com",
                SenhaHash = hasher.Hash("admin123"),
                Funcao = Funcao.Admin
            });
            await db.SaveChangesAsync();
        }
        else
        {
            tenant.Set(t.Id);
        }

        // Catálogo de funções de mão de obra (idempotente) — no tenant demo.
        if (!await db.Funcoes.AnyAsync())
        {
            db.Funcoes.AddRange(FuncoesParaTenant(t.Id));
            await db.SaveChangesAsync();
        }
    }
}
