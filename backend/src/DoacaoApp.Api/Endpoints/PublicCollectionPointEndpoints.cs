using DoacaoApp.Api.Common;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.CollectionPoints.Queries;
using MediatR;

namespace DoacaoApp.Api.Endpoints;

/// <summary>Parte pública do sistema: lista de pontos de coleta (Minimal API, anônimo).</summary>
public static class PublicCollectionPointEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/collection-points",
                async (ISender sender, CancellationToken ct) =>
                    (await sender.Send(new ListPublicCollectionPointsQuery(), ct))
                    .Match(value => Results.Ok(value)))
            .AllowAnonymous()
            .WithTags("Público")
            .Produces<IReadOnlyList<CollectionPointResponse>>()
            .WithSummary("Lista os pontos de coleta ativos, com endereço e contato.");

        return app;
    }
}