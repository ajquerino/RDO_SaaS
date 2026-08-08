namespace IndustrialOS.Application.Pagamento;

/// <summary>Cobrança PIX gerada no AbacatePay (v2 /transparents/create). Valores em CENTAVOS.
/// <see cref="BrCode"/> é o copia-e-cola PIX; <see cref="BrCodeBase64"/> é o QR (PNG base64).</summary>
public record CobrancaPix(string Id, string? BrCode, string? BrCodeBase64, string Status);

/// <summary>Integração com o AbacatePay (gateway BR, foco em PIX). Base https://api.abacatepay.com/v2,
/// auth Bearer. O AMBIENTE é definido pela chave (chave Dev = transações simuladas).
/// Ver docs.abacatepay.com. Sem chave configurada, <see cref="Configurado"/> é false e nada é chamado.</summary>
public interface IAbacatePay
{
    bool Configurado { get; }

    /// <summary>Gera uma cobrança PIX (valor em centavos). <paramref name="externalId"/> volta no webhook
    /// para mapear a empresa (usamos o TenantId). Retorna o copia-e-cola (brCode) e o QR em base64.</summary>
    Task<CobrancaPix> CriarCobrancaPixAsync(long valorCentavos, string externalId, string descricao,
        string? nomePagador, string? emailPagador, string? docPagador, CancellationToken ct = default);

    /// <summary>Confere a assinatura HMAC-SHA256 (base64) do corpo CRU do webhook contra o WebhookSecret.
    /// Comparação em tempo constante. Retorna false se não houver secret configurado.</summary>
    bool VerificarAssinaturaWebhook(string corpoCru, string? assinaturaHeader);

    /// <summary>Secret do webhook (também enviado por eles como ?webhookSecret= na URL) para dupla verificação.</summary>
    string? WebhookSecret { get; }
}
