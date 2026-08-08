using IndustrialOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IndustrialOS.Api.Common;

/// <summary>Resolve/mantém a sessão ativa vigente de cada usuário (1 sessão por usuário).
/// Cacheia por userId (~30s) para o enforcement em todo request não bater no banco toda hora;
/// no login, atualiza o cache para o "kick" do dispositivo anterior ser imediato.</summary>
public class SessaoService(AppDbContext db, IMemoryCache cache)
{
    private static string Chave(Guid userId) => $"sessao:{userId}";

    public async Task<Guid?> SessaoAtualAsync(Guid userId) =>
        await cache.GetOrCreateAsync(Chave(userId), async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            // TenantMiddleware ainda não rodou no OnTokenValidated => IgnoreQueryFilters.
            return await db.Usuarios.IgnoreQueryFilters()
                .Where(u => u.Id == userId)
                .Select(u => u.SessaoAtual)
                .FirstOrDefaultAsync();
        });

    /// <summary>Grava a sessão vigente no cache (chamado no login para invalidar a anterior na hora).</summary>
    public void Atualizar(Guid userId, Guid? sessao) =>
        cache.Set(Chave(userId), sessao, TimeSpan.FromSeconds(30));

    /// <summary>Lê a sessão DIRETO do banco (autoritativo) e reidrata o cache. Usado quando o cache
    /// diverge do token, para não derrubar por cache velho — ex.: logo após o login, se uma leitura
    /// concorrente do <see cref="SessaoAtualAsync"/> tiver repovoado o cache com a sessão antiga.</summary>
    public async Task<Guid?> RevalidarNoBancoAsync(Guid userId)
    {
        var sessao = await db.Usuarios.IgnoreQueryFilters()
            .Where(u => u.Id == userId)
            .Select(u => u.SessaoAtual)
            .FirstOrDefaultAsync();
        cache.Set(Chave(userId), sessao, TimeSpan.FromSeconds(30)); // corrige o cache velho
        return sessao;
    }
}
