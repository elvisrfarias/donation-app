using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Infrastructure.Storage;

/// <summary>Garante que o bucket configurado exista no startup (o MinIO sobe vazio).</summary>
public sealed class StorageBucketInitializer(
    IAmazonS3 client,
    IOptions<StorageOptions> options,
    ILogger<StorageBucketInitializer> logger)
{
    public async Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
        var bucket = options.Value.Bucket;

        try
        {
            if (await AmazonS3Util.DoesS3BucketExistV2Async(client, bucket))
            {
                logger.LogInformation("Bucket de storage '{Bucket}' já existe.", bucket);
                return;
            }

            await client.PutBucketAsync(
                new PutBucketRequest { BucketName = bucket, UseClientRegion = true },
                cancellationToken);

            logger.LogInformation("Bucket de storage '{Bucket}' criado.", bucket);
        }
        catch (Exception ex)
        {
            // Não derruba a aplicação: uploads falharão e serão logados até o storage normalizar.
            logger.LogError(ex, "Não foi possível garantir o bucket de storage '{Bucket}'.", bucket);
        }
    }
}
