using DoacaoApp.Api.Common;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.CollectionPoints.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Api.Seo;

/// <summary>
/// Páginas renderizadas no servidor (início, página de cada ponto, sitemap), servidas pelo nginx do frontend em
/// "/", "/pontos/..." e "/sitemap.xml". Ficam fora da documentação da API: são HTML para buscadores e redes sociais.
/// </summary>
public static class SeoEndpoints
{
    private const string HtmlContentType = "text/html; charset=utf-8";
    private static readonly TimeSpan FailedLookupCacheDuration = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddSeoPages(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SeoOptions>(configuration.GetSection(SeoOptions.SectionName));
        services.AddHttpClient<IndexHtmlTemplate>(client => client.Timeout = TimeSpan.FromSeconds(5));
        return services;
    }

    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder app)
    {
        var seo = app.MapGroup("/api/public/seo").AllowAnonymous().ExcludeFromDescription();

        seo.MapGet("/home", RenderHomeAsync);
        seo.MapGet("/pontos/{pointId:guid}/{slug?}", RenderPointAsync);
        seo.MapGet("/pontos/{**unknown}", RenderNotFoundAsync);
        seo.MapGet("/not-found", RenderNotFoundAsync);
        seo.MapGet("/sitemap.xml", RenderSitemapAsync);

        app.MapGet("/api/public/collection-points/{pointId:guid}",
                async (Guid pointId, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new GetPublicCollectionPointQuery(pointId), ct))
                    .Match(point => Results.Ok(point)))
            .AllowAnonymous()
            .WithTags("Público")
            .Produces<CollectionPointResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Um ponto de coleta ativo, com os mesmos dados públicos do mapa (base da página do ponto).");

        app.MapGet("/api/public/collection-points/{pointId:guid}/image", RedirectToPointImageAsync)
            .AllowAnonymous()
            .WithTags("Público")
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Endereço fixo da foto do ponto (usado nas prévias de link); redireciona para uma URL temporária do storage.");

        return app;
    }

    private static async Task<IResult> RenderHomeAsync(
        ISender sender,
        IndexHtmlTemplate template,
        IMemoryCache cache,
        IOptions<SeoOptions> options,
        CancellationToken cancellationToken)
    {
        var points = await ListPublicPointsAsync(sender, cache, options.Value, cancellationToken);
        var html = SeoHtmlRenderer.Render(
            await template.GetAsync(cancellationToken), SeoPages.Home(points, options.Value.SiteBaseUrl));

        return Html(html);
    }

    private static async Task<IResult> RenderPointAsync(
        Guid pointId,
        string? slug,
        ISender sender,
        IndexHtmlTemplate template,
        IOptions<SeoOptions> options,
        CancellationToken cancellationToken)
    {
        var point = await sender.Send(new GetPublicCollectionPointQuery(pointId), cancellationToken);
        if (point.IsError)
        {
            return await NotFoundPageAsync(template, options, cancellationToken);
        }

        if (slug != point.Value.Slug)
        {
            return Results.Redirect(SeoPages.PointPath(point.Value), permanent: true);
        }

        var html = SeoHtmlRenderer.Render(
            await template.GetAsync(cancellationToken), SeoPages.Point(point.Value, options.Value.SiteBaseUrl));
        return Html(html);
    }

    private static Task<IResult> RenderNotFoundAsync(
        IndexHtmlTemplate template,
        IOptions<SeoOptions> options,
        CancellationToken cancellationToken) =>
        NotFoundPageAsync(template, options, cancellationToken);

    private static async Task<IResult> RenderSitemapAsync(
        ISender sender,
        IMemoryCache cache,
        IOptions<SeoOptions> options,
        CancellationToken cancellationToken)
    {
        var points = await ListPublicPointsAsync(sender, cache, options.Value, cancellationToken);
        return Results.Text(SeoPages.Sitemap(points, options.Value.SiteBaseUrl), "application/xml; charset=utf-8");
    }

    private static async Task<IResult> RedirectToPointImageAsync(Guid pointId, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var point = await sender.Send(new GetPublicCollectionPointQuery(pointId), cancellationToken);
        if (point.IsError || point.Value.ImageUrl is not { } imageUrl)
        {
            return Results.NotFound();
        }

        http.Response.Headers.CacheControl = "public, max-age=600";
        return Results.Redirect(imageUrl);
    }

    private static async Task<IResult> NotFoundPageAsync(
        IndexHtmlTemplate template,
        IOptions<SeoOptions> options,
        CancellationToken cancellationToken)
    {
        var html = SeoHtmlRenderer.Render(
            await template.GetAsync(cancellationToken), SeoPages.NotFound(options.Value.SiteBaseUrl));
        return Html(html, StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Só os dados ficam em cache (o HTML é montado a cada acesso com o molde atual), protegendo o banco de
    /// robôs sem arriscar servir uma página que aponte para assets de um build antigo.
    /// </summary>
    private static async Task<IReadOnlyList<CollectionPointResponse>> ListPublicPointsAsync(
        ISender sender,
        IMemoryCache cache,
        SeoOptions options,
        CancellationToken cancellationToken)
    {
        if (options.PublicDataCacheSeconds <= 0)
        {
            var uncached = await sender.Send(new ListPublicCollectionPointsQuery(), cancellationToken);
            return uncached.IsError ? [] : uncached.Value;
        }

        var points = await cache.GetOrCreateAsync("seo:public-points", async entry =>
        {
            var result = await sender.Send(new ListPublicCollectionPointsQuery(), cancellationToken);
            entry.AbsoluteExpirationRelativeToNow = result.IsError
                ? FailedLookupCacheDuration
                : TimeSpan.FromSeconds(options.PublicDataCacheSeconds);
            return result.IsError ? [] : result.Value;
        });

        return points ?? [];
    }

    private static IResult Html(string html, int statusCode = StatusCodes.Status200OK) =>
        new NoCacheHtmlResult(html, statusCode);

    /// <summary>HTML sempre revalidado (como o index.html), para um deploy ou uma edição do ponto aparecer logo.</summary>
    private sealed class NoCacheHtmlResult(string html, int statusCode) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers.CacheControl = "no-cache";
            return Results.Content(html, HtmlContentType, statusCode: statusCode).ExecuteAsync(httpContext);
        }
    }
}
