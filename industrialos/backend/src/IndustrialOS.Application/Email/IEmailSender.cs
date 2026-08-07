namespace IndustrialOS.Application.Email;

/// <summary>Envio de e-mail transacional (abstração reutilizável). A implementação escolhe o provedor
/// (Resend/SendGrid) por config e, sem chave, faz fallback de log em dev.</summary>
public interface IEmailSender
{
    Task EnviarAsync(string para, string assunto, string html, CancellationToken ct = default);
}
