using IndustrialOS.Application.Auth;
using IndustrialOS.Application.Common;
using IndustrialOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Infrastructure.Persistence;

/// <summary>Seed de desenvolvimento: tenant demo + admin, e catálogo de funções de M.O.</summary>
public static class DbSeeder
{
    // Funções reais (categoria: Direta = executa a obra; Indireta = apoio/gestão).
    private static readonly (string Nome, string Categoria)[] FuncoesPadrao =
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

    public static async Task SeedAsync(AppDbContext db, ITenantContext tenant, IPasswordHasher hasher)
    {
        // Tenant de SISTEMA (Plataforma) — dono do SaaS. O super-admin mora aqui,
        // por isso NÃO enxerga dados de nenhuma empresa-cliente pelo filtro global.
        var sistema = await db.Tenants.FirstOrDefaultAsync(x => x.EhSistema);
        if (sistema is null)
        {
            sistema = new Tenant { Nome = "Plataforma", EhSistema = true, Plano = "sistema", Status = "sistema" };
            db.Tenants.Add(sistema);
            await db.SaveChangesAsync();
        }

        // Tenant DEMO (empresa-cliente) — como antes.
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
            foreach (var (nome, categoria) in FuncoesPadrao)
                db.Funcoes.Add(new FuncaoMaoObra { TenantId = t.Id, Nome = nome, Categoria = categoria });
            await db.SaveChangesAsync();
        }

        // Super-admin da plataforma (dono do SaaS) — mora no tenant de SISTEMA.
        var super = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Funcao == Funcao.SuperAdmin);
        if (super is null)
        {
            db.Usuarios.Add(new Usuario
            {
                TenantId = sistema.Id,
                Nome = "Super Admin (plataforma)",
                Email = "super@demo.com",
                SenhaHash = hasher.Hash("super123"),
                Funcao = Funcao.SuperAdmin
            });
            await db.SaveChangesAsync();
        }
        else if (super.TenantId != sistema.Id)
        {
            // Migra o super-admin que estava no tenant demo para o de sistema.
            super.TenantId = sistema.Id;
            await db.SaveChangesAsync();
        }
    }
}
