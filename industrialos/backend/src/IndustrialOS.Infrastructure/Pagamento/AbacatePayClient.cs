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
    private readonly string[] _metodos;

    public AbacatePayClient(HttpClient http, IConfiguration cfg, ILogger<AbacatePayClient> log)
    {
        _http = http;
        _log = log;
        _baseUrl = (cfg["AbacatePay:BaseUrl"] ?? "https://api.abacatepay.com/v2").TrimEnd('/');
        _apiKey = cfg["AbacatePay:ApiKey"];
        WebhookSecret = cfg["AbacatePay:WebhookSecret"];
        // Métodos habilitados na conta AbacatePay. CARD exige a conta verificada (KYC) — por isso o
        // default é PIX,BOLETO; adicione CARD em AbacatePay:Methods quando o cartão estiver liberado.
        _metodos = (cfg["AbacatePay:Methods"] ?? "PIX,BOLETO")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(m => m.ToUpperInvariant()).ToArray();
    }

    public bool Configurado => !string.IsNullOrWhiteSpace(_apiKey);
    public string? WebhookSecret { get; }

    private async Task<JsonElement> PostAsync(string path, object corpo, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{path}");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(req, ct);
        var texto = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogError("AbacatePay {Path} falhou ({Status}): {Body}", path, (int)resp.StatusCode, texto);
            throw new InvalidOperationException($"AbacatePay retornou {(int)resp.StatusCode}.");
        }
        var doc = JsonDocument.Parse(texto);
        return doc.RootElement.TryGetProperty("data", out var d) ? d.Clone() : doc.RootElement.Clone();
    }

    public async Task<string> CriarProdutoAsync(string nome, string? descricao, long precoCentavos,
        string externalId, CancellationToken ct = default)
    {
        if (!Configurado) throw new InvalidOperationException("AbacatePay não configurado.");
        var data = await PostAsync("/products/create", new Dictionary<string, object?>
        {
            ["name"] = nome,
            ["description"] = descricao ?? nome,
            ["price"] = precoCentavos,
            ["currency"] = "BRL",
            ["externalId"] = externalId,
        }, ct);
        return data.GetProperty("id").GetString()!;
    }

    public async Task<CobrancaPix> CriarCheckoutAsync(string produtoId, string externalId, string retornoUrl,
        CancellationToken ct = default)
    {
        if (!Configurado) throw new InvalidOperationException("AbacatePay não configurado.");
        var data = await PostAsync("/checkouts/create", new Dictionary<string, object?>
        {
            ["items"] = new[] { new Dictionary<string, object?> { ["id"] = produtoId, ["quantity"] = 1 } },
            ["methods"] = _metodos,
            ["externalId"] = externalId,
            ["frequency"] = "ONE_TIME",
            ["returnUrl"] = retornoUrl,
            ["completionUrl"] = retornoUrl,
        }, ct);
        string S(string k) => data.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
        return new CobrancaPix(Id: S("id"), Url: S("url"), Status: S("status"));
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
