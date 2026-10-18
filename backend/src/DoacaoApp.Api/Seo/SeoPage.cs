namespace DoacaoApp.Api.Seo;

/// <summary>Metadados e conteúdo sem JavaScript de uma página renderizada no servidor.</summary>
public sealed record SeoPage(
    string Title,
    string Description,
    string CanonicalUrl,
    string? ImageUrl,
    string ImageAlt,
    string? JsonLd,
    string NoscriptHtml,
    bool Indexable = true);
