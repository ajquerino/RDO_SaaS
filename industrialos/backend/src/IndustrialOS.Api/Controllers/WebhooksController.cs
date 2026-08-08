using System.Text.Json;
using IndustrialOS.Application.Pagamento;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IndustrialOS.Api.Controllers;

/// <summary>Recebe webhooks de gateways de pagamento. PÚBLICO (o provedor chama sem JWT), mas
/// autenticado por webhookSecret (query) + assinatura HMAC (header). NÃO tem tenant no contexto —
/// a empresa é identificada pelo externalId (= TenantId) que enviamos ao criar a cobrança.</summary>
[ApiController]
[Route("api/v1/webhooks")]
[AllowAnonymous]
public class WebhooksController(AppDbContext db, IAbacatePay abacate, IMemoryCache cache,
    ILogger<WebhooksController> log) : ControllerBase
{
    // Eventos que confirmam pagamento (avançam o vencimento) e o de cancelamento.
    private static readonly string[] Pagos =
        ["checkout.completed", "transparent.completed", "subscription.renewed", "subscription.completed"];

    [HttpPost("abacatepay")]
    public async Task<IActionResult> AbacatePay()
    {
        // 1) corpo CRU (necessário p/ conferir o HMAC).
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var corpo = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        // 2) dupla verificação: ?webhookSecret= e header X-Webhook-Signature (HMAC do corpo).
        var secretQuery = Request.Query["webhookSecret"].ToString();
        var assinatura = Request.Headers["X-Webhook-Signature"].ToString();
        var secretOk = !string.IsNullOrEmpty(abacate.WebhookSecret) && secretQuery == abacate.WebhookSecret;
        var hmacOk = abacate.VerificarAssinaturaWebhook(corpo, assinatura);
        if (!secretOk && !hmacOk)
        {
            log.LogWarning("Webhook AbacatePay rejeitado (secret/assinatura inválidos).");
            return Unauthorized();
        }

        // 3) parse + idempotência (mesmo evento reenviado não avança duas vezes).
        JsonElement raiz;
        try { raiz = JsonDocument.Parse(corpo).RootElement; }
        catch { return BadRequest(); }

        var evento = Str(raiz, "event");
        var eventoId = Str(raiz, "id");
        if (!string.IsNullOrEmpty(eventoId))
        {
            if (cache.TryGetValue($"wh:{eventoId}", out _)) return Ok(new { ignorado = "duplicado" });
            cache.Set($"wh:{eventoId}", true, TimeSpan.FromHours(6));
        }

        var data = raiz.TryGetProperty("data", out var d) ? d : default;
        var externalId = data.ValueKind == JsonValueKind.Object ? Str(data, "externalId") : null;
        if (!Guid.TryParse(externalId, out var tenantId))
        {
            log.LogInformation("Webhook AbacatePay '{Evento}' sem externalId de tenant — ignorado.", evento);
            return Ok(new { ignorado = "sem tenant" });
        }

        var a = await db.Assinaturas.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.TenantId == tenantId);
        if (a is null) { a = new Assinatura { TenantId = tenantId }; db.Assinaturas.Add(a); }
        a.ProvedorNome = "abacatepay";

        if (Pagos.Contains(evento))
        {
            // Regulariza: próximo vencimento sempre no futuro (mesma regra do "marcar pago").
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var baseData = (a.VencimentoEm is { } v && v > hoje) ? v : hoje;
            a.VencimentoEm = baseData.AddMonths(1);
            a.TrialAte = null;
            a.Cancelada = false;
            log.LogInformation("Webhook '{Evento}': tenant {Tenant} regularizado até {Venc}.", evento, tenantId, a.VencimentoEm);
        }
        else if (evento == "subscription.cancelled")
        {
            a.Cancelada = true;
            log.LogInformation("Webhook '{Evento}': assinatura do tenant {Tenant} cancelada.", evento, tenantId);
        }

        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    private static string? Str(JsonElement e, string prop) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;
}
