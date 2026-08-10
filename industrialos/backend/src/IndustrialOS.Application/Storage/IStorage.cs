namespace IndustrialOS.Application.Storage;

/// <summary>Armazenamento de objetos (Cloudflare R2 / S3-compat) para midia e PDFs.</summary>
public interface IStorage
{
    Task<string> UploadAsync(Stream conteudo, string key, string contentType, CancellationToken ct = default);
    Task<string> UrlAssinadaAsync(string key, TimeSpan validade);
    Task DeleteAsync(string key, CancellationToken ct = default);
    /// <summary>Baixa o objeto inteiro em memória (ex.: embutir foto no PDF).</summary>
    Task<byte[]> DownloadAsync(string key, CancellationToken ct = default);
}
