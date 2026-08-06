using IndustrialOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Common;

/// <summary>Uso do tenant corrente vs limites do plano (Tenant.PlanoId → Plano).
/// Limites null = ilimitado (sem plano ou plano sem limite). É só AVISO — nunca bloqueia.</summary>
public record UsoPlanoResultado(int NObras, int? LimiteObras, int NUsuarios, int? LimiteUsuarios, string? Plano);

public static class UsoPlanoCalc
{
    /// <summary>Conta obras (não deletadas) e usuários (ativos, não deletados) do tenant corrente
    /// — o filtro global já restringe ao tenant — e resolve os limites do plano vinculado.</summary>
    public static async Task<UsoPlanoResultado> CalcularAsync(AppDbContext db)
    {
        int nObras = await db.Obras.CountAsync();
        int nUsuarios = await db.Usuarios.CountAsync(u => u.Ativo);

        int? limObras = null, limUsuarios = null;
        string? plano = null;
        if (db.CurrentTenant is Guid tid)
        {
            var t = await db.Tenants.FirstOrDefaultAsync(x => x.Id == tid);
            if (t?.PlanoId is Guid pid && await db.Planos.FirstOrDefaultAsync(x => x.Id == pid) is { } p)
            {
                limObras = p.LimiteObras;
                limUsuarios = p.LimiteUsuarios;
                plano = p.Nome;
            }
        }
        return new UsoPlanoResultado(nObras, limObras, nUsuarios, limUsuarios, plano);
    }

    /// <summary>Mensagem de aviso de obras (ou null se sem limite / dentro do limite). Nunca bloqueia.</summary>
    public static string? AvisoObras(int nObras, int? limite, string? plano) =>
        limite is int l && nObras >= l
            ? $"Sua empresa atingiu o limite de {l} obras do plano {plano ?? "atual"}. Fale com o administrador para fazer upgrade."
            : null;

    /// <summary>Mensagem de aviso de usuários (ou null se sem limite / dentro do limite). Nunca bloqueia.</summary>
    public static string? AvisoUsuarios(int nUsuarios, int? limite, string? plano) =>
        limite is int l && nUsuarios >= l
            ? $"Sua empresa atingiu o limite de {l} usuários do plano {plano ?? "atual"}. Fale com o administrador para fazer upgrade."
            : null;
}
