using Serilog.Context;

namespace IndustrialOS.Api.Middleware;

/// <summary>Garante um X-Correlation-Id por request (aceita o do cliente ou gera um) e o injeta
/// no LogContext do Serilog + no header de resposta, para rastrear a requisição ponta a ponta.</summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string Header = "X-Correlation-Id";

    public async Task Invoke(HttpContext ctx)
    {
        var id = ctx.Request.Headers.TryGetValue(Header, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : Guid.NewGuid().ToString("N");

        ctx.Response.Headers[Header] = id;
        using (LogContext.PushProperty("CorrelationId", id))
            await next(ctx);
    }
}
