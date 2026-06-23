using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Domain.Storage;

namespace DoacaoApp.IntegrationTests;

/// <summary>
/// Implementação em memória de <see cref="IFileStorage"/> para os testes de integração —
/// evita a dependência de um MinIO/S3 real. O "upload" só drena o stream e devolve metadados;
/// a URL de download é uma URL fixa, suficiente para os asserts.
/// </summary>
public sealed class FakeFileStorage : IFileStorage
{
    public async Task<StoredObject> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        // Drena o conteúdo para simular o consumo do stream (o handler conta com isso).
        using var sink = new MemoryStream();
        await request.Content.CopyToAsync(sink, cancellationToken);

        return new StoredObject(
            StorageProvider.MinIO,
            "doacao-test",
            request.ObjectKey,
            ETag: "\"fake-etag\"",
            SizeBytes: sink.Length);
    }

    public Task<Uri> GetPresignedDownloadUrlAsync(
        string objectKey,
        string? downloadFileName = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new Uri($"https://fake-storage.test/{objectKey}"));

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
