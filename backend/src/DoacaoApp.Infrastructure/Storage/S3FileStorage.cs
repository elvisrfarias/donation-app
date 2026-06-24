using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Domain.Storage;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Infrastructure.Storage;

/// <summary>
/// Implementação de <see cref="IFileStorage"/> sobre a API do S3 (AWS SDK). Funciona
/// igual contra o MinIO (dev) e o Amazon S3 (produção) — só muda a configuração.
///
/// Usa dois clientes: o injetado (<c>serverClient</c>), com o endpoint interno usado para
/// upload/delete; e um cliente de pré-assinatura, configurado com o endpoint público. Como
/// gerar URL pré-assinada é um cálculo local (não há chamada de rede), a assinatura SigV4 fica
/// válida para o host que o navegador realmente acessa.
/// </summary>
public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private readonly IAmazonS3 _serverClient;
    private readonly IAmazonS3 _presignClient;
    private readonly bool _ownsPresignClient;
    private readonly StorageOptions _options;
    private readonly StorageProvider _provider;

    public S3FileStorage(IAmazonS3 serverClient, IOptions<StorageOptions> options)
    {
        _serverClient = serverClient;
        _options = options.Value;
        _provider = _options.ResolveProvider();

        if (!string.IsNullOrWhiteSpace(_options.PublicUrl)
            && !string.Equals(_options.PublicUrl, _options.ServiceUrl, StringComparison.OrdinalIgnoreCase))
        {
            var config = new AmazonS3Config
            {
                ForcePathStyle = _options.ForcePathStyle,
                ServiceURL = _options.PublicUrl,
                AuthenticationRegion = _options.Region,
            };
            _presignClient = new AmazonS3Client(
                new BasicAWSCredentials(_options.AccessKey, _options.SecretKey),
                config);
            _ownsPresignClient = true;
        }
        else
        {
            _presignClient = serverClient;
            _ownsPresignClient = false;
        }
    }

    public async Task<StoredObject> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        var put = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = request.ObjectKey,
            InputStream = request.Content,
            ContentType = request.ContentType,
            AutoCloseStream = false,
        };

        var response = await _serverClient.PutObjectAsync(put, cancellationToken);

        return new StoredObject(
            _provider,
            _options.Bucket,
            request.ObjectKey,
            response.ETag?.Trim('"') ?? string.Empty,
            request.Length);
    }

    public Task<Uri> GetPresignedDownloadUrlAsync(
        string objectKey,
        string? downloadFileName = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_options.PresignExpiryMinutes),
        };

        if (!string.IsNullOrWhiteSpace(downloadFileName))
        {
            request.ResponseHeaderOverrides.ContentDisposition =
                $"inline; filename=\"{SanitizeFileName(downloadFileName)}\"";
        }

        var url = _presignClient.GetPreSignedURL(request);
        return Task.FromResult(new Uri(url));
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        await _serverClient.DeleteObjectAsync(_options.Bucket, objectKey, cancellationToken);
    }

    private static string SanitizeFileName(string fileName) =>
        fileName.Replace("\"", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);

    public void Dispose()
    {
        if (_ownsPresignClient)
        {
            _presignClient.Dispose();
        }
    }
}
