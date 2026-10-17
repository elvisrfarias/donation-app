using System.Collections.Frozen;

namespace DoacaoApp.Application.Storage;

/// <summary>Restrições de upload de imagens — aplicadas na validação dos comandos.</summary>
public static class SupportedImage
{
    public const int MaxFileSizeMegabytes = 10;
    public const long MaxFileSizeBytes = MaxFileSizeMegabytes * 1024L * 1024L;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly FrozenDictionary<string, string> ExtensionsByContentType =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif",
            ["image/avif"] = ".avif",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>Content-types aceitos (comparação case-insensitive).</summary>
    public static IReadOnlyCollection<string> ContentTypes => ExtensionsByContentType.Keys;

    public static bool IsAllowed(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && ExtensionsByContentType.ContainsKey(contentType);

    /// <summary>
    /// Extensão do objeto no storage derivada do content-type já validado — nunca do nome do arquivo
    /// enviado pelo cliente, que poderia gravar algo como <c>.html</c> ou <c>.svg</c> no bucket.
    /// </summary>
    public static string ExtensionFor(string contentType) => ExtensionsByContentType[contentType];

    /// <summary>
    /// Confere os primeiros bytes do arquivo contra a assinatura real do formato — o
    /// <c>Content-Type</c> do multipart é enviado pelo cliente e não prova nada sozinho.
    /// Deixa a posição do stream como estava antes de ler.
    /// </summary>
    public static async Task<bool> MatchesFileSignatureAsync(
        Stream content,
        string? contentType,
        CancellationToken cancellationToken)
    {
        if (!content.CanSeek || string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var originalPosition = content.Position;
        var header = new byte[12];
        int bytesRead;
        try
        {
            content.Position = 0;
            bytesRead = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        }
        finally
        {
            content.Position = originalPosition;
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(PngSignature),
            "image/gif" => bytesRead >= 4 && header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8',
            "image/webp" => bytesRead >= 12
                && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            "image/avif" => bytesRead >= 12 && header.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            _ => false,
        };
    }
}
