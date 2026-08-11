using System.Net.Http.Headers;
using System.Net.Http.Json;
using IndustrialOS.Application.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IndustrialOS.Infrastructure.Email;

/// <summary>Envio de e-mail via HTTP (sem SDK pesado). Provedor por config Email:Provider
/// ("resend" | "sendgrid"). FALLBACK DEV: se Email:ApiKey estiver vazio, NÃO chama a rede —
/// loga o e-mail (assunto + corpo) e retorna sucesso, deixando o fluxo testável sem chave.</summary>
public class EmailSender(IHttpClientFactory http, IConfiguration cfg, ILogger<EmailSender> log) : IEmailSender
{
    public async Task EnviarAsync(string para, string assunto, string html, CancellationToken ct = default)
    {
        var apiKey = cfg["Email:ApiKey"];
        var from = cfg["Email:From"] ?? "no-reply@localhost";
        var fromName = cfg["Email:FromName"] ?? "Montaris";
        var provider = (cfg["Email:Provider"] ?? "resend").Trim().ToLowerInvariant();

        // Sem chave => modo dev: loga e retorna (fluxo de recuperação testável sem provedor real).
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            log.LogWarning("[E-MAIL DEV — não enviado, Email:ApiKey vazio] Para={Para} | Assunto={Assunto}\n{Html}",
                para, assunto, html);
            return;
        }

        var client = http.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        HttpResponseMessage resp;
        if (provider == "sendgrid")
        {
            var body = new
            {
                personalizations = new[] { new { to = new[] { new { email = para } } } },
                from = new { email = from, name = fromName },
                subject = assunto,
                content = new[] { new { type = "text/html", value = html } },
            };
            resp = await client.PostAsJsonAsync("https://api.sendgrid.com/v3/mail/send", body, ct);
        }
        else // resend (padrão)
        {
            var body = new
            {
                from = $"{fromName} <{from}>",
                to = new[] { para },
                subject = assunto,
                html,
            };
            resp = await client.PostAsJsonAsync("https://api.resend.com/emails", body, ct);
        }

        if (!resp.IsSuccessStatusCode)
        {
            var txt = await resp.Content.ReadAsStringAsync(ct);
            log.LogError("Falha ao enviar e-mail via {Provider}: {Status} {Body}", provider, (int)resp.StatusCode, txt);
            throw new InvalidOperationException("Falha ao enviar e-mail.");
        }
    }
}
