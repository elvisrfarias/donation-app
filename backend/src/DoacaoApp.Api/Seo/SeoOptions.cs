namespace DoacaoApp.Api.Seo;

/// <summary>Páginas renderizadas no servidor para buscadores e prévias de link, seção "Seo".</summary>
public sealed class SeoOptions
{
    public const string SectionName = "Seo";

    /// <summary>Endereço público do site, base das URLs canônicas (sem barra no final).</summary>
    public string SiteUrl { get; init; } = "http://localhost:5173";

    /// <summary>De onde a API lê o index.html do build do frontend, que serve de molde para as páginas.</summary>
    public string IndexHtmlUrl { get; init; } = "http://frontend:8080/index.html";

    public string SiteBaseUrl => SiteUrl.TrimEnd('/');
}
