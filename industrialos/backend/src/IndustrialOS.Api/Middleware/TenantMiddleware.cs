using IndustrialOS.Application.Common;

namespace IndustrialOS.Api.Middleware;

/// <summary>Resolve o tenant do claim `tenant_id` do JWT e injeta no ITenantContext.</summary>
public class TenantMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext ctx, ITenantContext tenant)
    {
        var claim = ctx.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(claim, out var id)) tenant.Set(id);
        await next(ctx);
    }
}
