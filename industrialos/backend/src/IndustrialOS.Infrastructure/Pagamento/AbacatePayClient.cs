using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IndustrialOS.Application.Pagamento;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IndustrialOS.Infrastructure.Pagamento;

/// <summary>Cliente do AbacatePay via HttpClient. Endpoints e formatos conforme docs.abacatepay.com
/// (base /v2, Bearer). ATENÇÃO: os formatos exatos de request/response (transparents/create) precisam
/// ser confirmados AO VIVO com uma chave Dev — este cliente é defensivo na leitura da resposta.</summary>
public class AbacatePayClient : IAbacatePay
{
    private readonly HttpClient _http;
    private readonly ILogger<AbacatePayClient> _log;
    private readonly string _baseUrl;
    private readonly string? _apiKey;

    public AbacatePayClient(HttpClient http, IConfiguration cfg, ILogger<AbacatePayClient> log)
    {
        _http = http;
        _log = log;
        _baseUrl = (cfg["AbacatePay:BaseUrl"] ?? "https://api.abacatepay.com/v2").TrimEnd('/');
        _apiKey = cfg["AbacatePay:ApiKey"];
        WebhookSecret = cfg["AbacatePay:WebhookSecret"];
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(_apiKey);
    public string? WebhookSecret { get; }

    public async Task<CobrancaPix> CriarCobrancaPixAsync(long valorCentavos, string externalId, string descricao,
        string? nomePagador, string? emailPagador, CancellationToken ct = default)
    {
        if (!Configurado) throw new InvalidOperationException("AbacatePay não configurado (AbacatePay:ApiKey vazio).");

        // /transparents/create (PIX): corpo FLAT. Campos conforme docs.abacatepay.com (llms-full).
        var corpo = new Dictionary<string, object?>
        {
            ["method"] = "PIX",
            ["amount"] = valorCentavos,
            ["externalId"] = externalId,
            ["description"] = descricao,
            ["expiresIn"] = 86400,   // QR válido por 24h
        };
        _ = (nomePagador, emailPagador); // customer não vai no /transparents PIX (quebra a validação)

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/transparents/create");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var texto = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogError("AbacatePay cobrança falhou ({Status}): {Body}", (int)resp.StatusCode, texto);
            throw new InvalidOperationException($"AbacatePay retornou {(int)resp.StatusCode}.");
        }

        // Resposta padrão { data, error, success }. Leitura defensiva.
        using var doc = JsonDocument.Parse(texto);
        var data = doc.RootElement.TryGetProperty("data", out var d) ? d : doc.RootElement;
        string S(string k) => data.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
        return new CobrancaPix(
            Id: S("id"),
            BrCode: data.TryGetProperty("brCode", out var bc) ? bc.GetString() : null,
            BrCodeBase64: data.TryGetProperty("brCodeBase64", out var bq) ? bq.GetString() : null,
            Status: S("status"));
    }

    public bool VerificarAssinaturaWebhook(string corpoCru, string? assinaturaHeader)
    {
        if (string.IsNullOrEmpty(WebhookSecret) || string.IsNullOrEmpty(assinaturaHeader)) return false;
        var esperado = Convert.ToBase64String(
            new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret)).ComputeHash(Encoding.UTF8.GetBytes(corpoCru)));
        var a = Encoding.UTF8.GetBytes(esperado);
        var b = Encoding.UTF8.GetBytes(assinaturaHeader);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
