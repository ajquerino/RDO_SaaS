using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using IndustrialOS.Application.Storage;
using Microsoft.Extensions.Configuration;

namespace IndustrialOS.Infrastructure.Storage;

/// <summary>Cloudflare R2 via API S3-compativel (AWS SDK).</summary>
public class R2Storage : IStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public R2Storage(IConfiguration cfg)
    {
        var accountId = cfg["R2:AccountId"];
        var bucket = cfg["R2:Bucket"];
        var key = cfg["R2:AccessKeyId"];
        var secret = cfg["R2:SecretAccessKey"];
        _bucket = bucket ?? throw new InvalidOperationException("R2:Bucket nao configurado.");

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            // R2 nao suporta os checksums automaticos que o SDK v4 envia por padrao.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        _s3 = new AmazonS3Client(new BasicAWSCredentials(key, secret), config);
    }

    public async Task<string> UploadAsync(Stream conteudo, string key, string contentType, CancellationToken ct = default)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = conteudo,
            ContentType = contentType,
            DisablePayloadSigning = true, // exigido pelo R2
        }, ct);
        return key;
    }

    public Task<string> UrlAssinadaAsync(string key, TimeSpan validade) =>
        _s3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(validade),
        });

    public Task DeleteAsync(string key, CancellationToken ct = default) =>
        _s3.DeleteObjectAsync(_bucket, key, ct);

    public async Task<byte[]> DownloadAsync(string key, CancellationToken ct = default)
    {
        using var resp = await _s3.GetObjectAsync(_bucket, key, ct);
        using var ms = new MemoryStream();
        await resp.ResponseStream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
