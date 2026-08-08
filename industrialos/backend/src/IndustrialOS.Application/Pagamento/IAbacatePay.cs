namespace IndustrialOS.Application.Pagamento;

/// <summary>Checkout hospedado gerado no AbacatePay. <see cref="Url"/> é a página segura (PIX/cartão/
/// boleto) para onde a empresa é redirecionada. Valores sempre em CENTAVOS.</summary>
public record CobrancaPix(string Id, string? Url, string Status);

/// <summary>Integração com o AbacatePay (gateway BR: PIX, cartão, boleto). Base https://api.abacatepay.com/v2,
/// auth Bearer. O AMBIENTE é definido pela chave (chave Dev = transações simuladas).
/// Ver docs.abacatepay.com. Sem chave configurada, <see cref="Configurado"/> é false e nada é chamado.</summary>
public interface IAbacatePay
{
    bool Configurado { get; }

    /// <summary>Cria um produto no AbacatePay (necessário p/ o checkout hospedado). Retorna o id (prod_...).</summary>
    Task<string> CriarProdutoAsync(string nome, string? descricao, long precoCentavos, string externalId,
        CancellationToken ct = default);

    /// <summary>Cria um checkout hospedado (PIX/cartão/boleto conforme os métodos habilitados) para o
    /// <paramref name="produtoId"/>. <paramref name="externalId"/> (=TenantId) volta no webhook.
    /// Retorna a URL da página de pagamento.</summary>
    Task<CobrancaPix> CriarCheckoutAsync(string produtoId, string externalId, string retornoUrl,
        CancellationToken ct = default);

    /// <summary>Confere a assinatura HMAC-SHA256 (base64) do corpo CRU do webhook contra o WebhookSecret.
    /// Comparação em tempo constante. Retorna false se não houver secret configurado.</summary>
    bool VerificarAssinaturaWebhook(string corpoCru, string? assinaturaHeader);

    /// <summary>Secret do webhook (também enviado por eles como ?webhookSecret= na URL) para dupla verificação.</summary>
    string? WebhookSecret { get; }
}
