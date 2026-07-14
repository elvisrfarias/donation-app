using DoacaoApp.Api.Common;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.CollectionPoints.Queries;
using DoacaoApp.Application.Reviews.Commands;
using DoacaoApp.Application.Reviews.Contracts;
using DoacaoApp.Application.Reviews.Queries;
using MediatR;

namespace DoacaoApp.Api.Endpoints;

/// <summary>
/// Parte pública do sistema: pontos de coleta e as avaliações da comunidade
/// (Minimal API, anônimo).
/// </summary>
public static class PublicCollectionPointEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/collection-points",
                async (ISender sender, string? sort, CancellationToken ct) =>
                    (await sender.Send(
                        new ListPublicCollectionPointsQuery(TopRatedFirst: sort == "top-rated"), ct))
                    .Match(value => Results.Ok(value)))
            .AllowAnonymous()
            .WithTags("Público")
            .Produces<IReadOnlyList<CollectionPointResponse>>()
            .WithSummary("Lista os pontos de coleta ativos, com endereço, contato e resumo das avaliações.")
            .WithDescription("Use sort=top-rated para ordenar do mais bem avaliado ao menos avaliado (ranking).");

        app.MapGet("/api/public/collection-points/{pointId:guid}/reviews",
                async (Guid pointId, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new ListReviewsForPointQuery(pointId), ct))
                    .Match(value => Results.Ok(value)))
            .AllowAnonymous()
            .WithTags("Público")
            .Produces<IReadOnlyList<ReviewResponse>>()
            .WithSummary("Lista as avaliações (nota + comentário) de um ponto de coleta.");

        app.MapPost("/api/public/collection-points/{pointId:guid}/reviews",
                async (Guid pointId, CreateReviewRequest request, ISender sender, CancellationToken ct) =>
                    (await sender.Send(
                        new CreateReviewCommand(pointId, request.AuthorName, request.Rating, request.Comment), ct))
                    .Match(value => Results.Created(
                        $"/api/public/collection-points/{pointId}/reviews/{value.Id}", value)))
            .AllowAnonymous()
            .WithTags("Público")
            .Produces<ReviewResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .WithSummary("Registra a avaliação da comunidade sobre um ponto de coleta.");

        app.MapPost("/api/public/reviews/{reviewId:guid}/report",
                async (Guid reviewId, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new ReportReviewCommand(reviewId), ct))
                    .Match(_ => Results.NoContent()))
            .AllowAnonymous()
            .WithTags("Público")
            .WithSummary("Denuncia uma avaliação inadequada para moderação.");

        return app;
    }
}

/// <summary>Corpo do POST de avaliação (o ponto vem da rota).</summary>
public sealed record CreateReviewRequest(string AuthorName, int Rating, string? Comment);
