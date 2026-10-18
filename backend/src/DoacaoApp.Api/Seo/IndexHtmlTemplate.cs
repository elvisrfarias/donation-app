using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Api.Seo;

/// <summary>
/// O index.html publicado pelo frontend (com os nomes dos assets do build atual). É lido pela rede interna e
/// guardado por poucos minutos, então um deploy do frontend chega às páginas renderizadas logo em seguida.
/// </summary>
public sealed class IndexHtmlTemplate(HttpClient httpClient, IMemoryCache cache, IOptions<SeoOptions> options)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);
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
