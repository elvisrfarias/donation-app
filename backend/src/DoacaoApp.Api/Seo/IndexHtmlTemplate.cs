using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Api.Seo;

/// <summary>
/// O index.html publicado pelo frontend (com os nomes dos assets do build atual). É lido pela rede interna e
/// guardado só por alguns segundos: depois de um deploy do frontend, os assets antigos deixam de existir, e um
/// molde velho geraria páginas apontando para arquivos que não existem mais.
/// </summary>
public sealed class IndexHtmlTemplate(HttpClient httpClient, IMemoryCache cache, IOptions<SeoOptions> options)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);
    private const string CacheKey = "seo:index-html";

    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        var html = await httpClient.GetStringAsync(options.Value.IndexHtmlUrl, cancellationToken);
        cache.Set(CacheKey, html, CacheDuration);
        return html;
    }
}
