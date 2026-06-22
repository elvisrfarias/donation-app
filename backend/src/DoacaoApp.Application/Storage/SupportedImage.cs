namespace DoacaoApp.Application.Storage;

/// <summary>Restrições de upload de imagens — aplicadas na validação dos comandos.</summary>
public static class SupportedImage
{
    public const int MaxFileSizeMegabytes = 10;
    public const long MaxFileSizeBytes = MaxFileSizeMegabytes * 1024L * 1024L;

    /// <summary>Content-types aceitos (comparação case-insensitive).</summary>
    public static readonly IReadOnlySet<string> ContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "image/avif",
    };

    public static bool IsAllowed(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && ContentTypes.Contains(contentType);
}
