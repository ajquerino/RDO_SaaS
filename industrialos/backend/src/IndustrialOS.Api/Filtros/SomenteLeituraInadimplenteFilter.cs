using IndustrialOS.Application.Common;
using IndustrialOS.Domain.Services;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Filtros;

/// <summary>Enforcement de inadimplência SOMENTE-LEITURA. Quando a assinatura do tenant está
/// BLOQUEADA, deixa passar as leituras (GET/HEAD/OPTIONS — ver/baixar RDOs/obras/PDFs já salvos)
/// e curto-circuita as escritas com 402 Payment Required. NÃO altera o TenantMiddleware nem o
/// filtro global de tenant; cross-tenant continua exclusivo do PlataformaController.</summary>
public class SomenteLeituraInadimplenteFilter(AppDbContext db, ITenantContext tenant) : IAsyncActionFilter
{
    // Rotas sempre livres: login, console do super-admin e o próprio status da assinatura.
    private static readonly string[] RotasLivres =
        ["/api/v1/auth", "/api/v1/plataforma", "/api/v1/assinatura", "/api/v1/webhooks"];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var metodo = http.Request.Method;

        bool leitura = HttpMethods.IsGet(metodo) || HttpMethods.IsHead(metodo) || HttpMethods.IsOptions(metodo);
        bool superAdmin = http.User.IsInRole("SuperAdmin");
        bool rotaLivre = RotasLivres.Any(r => http.Request.Path.StartsWithSegments(r, StringComparison.OrdinalIgnoreCase));

        // Passa direto: leitura, super-admin, rota livre, ou sem tenant autenticado.
        if (leitura || superAdmin || rotaLivre || tenant.TenantId is not Guid tid)
        {
            await next();
            return;
        }

        // Empresa SUSPENSA pelo super-admin (ou soft-deletada) => somente leitura, independente da assinatura.
        var t = await db.Tenants.IgnoreQueryFilters().Where(x => x.Id == tid)
            .Select(x => new { x.Status, x.DeletadoEm }).FirstOrDefaultAsync();
        bool suspenso = t is not null && (t.Status == "suspenso" || t.DeletadoEm != null);

        // Escrita de um tenant normal: avalia a assinatura (Assinatura não é BaseEntity; IgnoreQueryFilters por segurança).
        var assinatura = await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.TenantId == tid);
        var estado = AssinaturaCalculo.Avaliar(assinatura, DateOnly.FromDateTime(DateTime.UtcNow));

        if (suspenso || estado.Bloqueada)
        {
            var msg = suspenso
                ? "Empresa suspensa — acesso somente leitura."
                : "Assinatura vencida — acesso somente leitura até regularizar.";
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status402PaymentRequired,
                Title = suspenso ? "Empresa suspensa" : "Assinatura vencida",
                Detail = msg,
                Extensions = { ["erro"] = msg, ["diasAtraso"] = estado.DiasAtraso },
            })
            { StatusCode = StatusCodes.Status402PaymentRequired };
            return;
        }

        await next();
    }
}
