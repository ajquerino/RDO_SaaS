using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Middleware;

/// <summary>Tratador global de exceções: devolve ProblemDetails (RFC 7807) com status adequado,
/// em vez de stack trace. O detalhe completo só aparece em Development.</summary>
public class ExceptionMiddleware(RequestDelegate next, IHostEnvironment env, ILogger<ExceptionMiddleware> log)
{
    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            var (status, title) = Mapear(ex);
            log.LogError(ex, "Erro não tratado ({Status}): {Title}", status, title);

            if (ctx.Response.HasStarted) throw; // resposta já começou: não dá para reescrever

            ctx.Response.Clear();
            ctx.Response.StatusCode = status;
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = $"https://httpstatuses.io/{status}",
                Detail = env.IsDevelopment() ? ex.ToString() : null,
            };
            problem.Extensions["correlationId"] = ctx.Response.Headers[CorrelationIdMiddleware.Header].ToString();
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(problem);
        }
    }

    private static (int status, string title) Mapear(Exception ex) => ex switch
    {
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
        ArgumentException => (StatusCodes.Status400BadRequest, "Requisição inválida."),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflito de concorrência."),
        _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor."),
    };
}
