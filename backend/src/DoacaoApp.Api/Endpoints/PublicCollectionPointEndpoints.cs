using DoacaoApp.Api.Common;
using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.CollectionPoints.Queries;
using DoacaoApp.Application.Donations.Commands;
using DoacaoApp.Application.Donations.Contracts;
using DoacaoApp.Application.Notices.Contracts;
using DoacaoApp.Application.Notices.Queries;
using DoacaoApp.Application.Reviews.Commands;
using DoacaoApp.Application.Reviews.Contracts;
using DoacaoApp.Application.Reviews.Queries;
using DoacaoApp.Application.Sponsors.Contracts;
using DoacaoApp.Application.Sponsors.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace DoacaoApp.Api.Endpoints;

/// <summary>
/// Parte pública do sistema: pontos de coleta, avaliações da comunidade, mural de avisos,
/// doações prometidas e patrocinadores do projeto (Minimal API, anônimo).
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

        app.MapGet("/api/public/sponsors",
                async (ISender sender, CancellationToken ct) =>
                    (await sender.Send(new ListPublicSponsorsQuery(), ct)).Match(value => Results.Ok(value)))
            .AllowAnonymous()
            .WithTags("Público")
            .Produces<IReadOnlyList<SponsorResponse>>()
            .WithSummary("Lista os patrocinadores ativos do projeto, do Diamante à Prata.");

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
                async (Guid reviewId, HttpContext http, IMemoryCache cache, ISender sender, CancellationToken ct) =>
                    ReviewReportDeduplicator.TryRegister(cache, http.Connection.RemoteIpAddress, reviewId)
                        ? (await sender.Send(new ReportReviewCommand(reviewId), ct)).Match(_ => Results.NoContent())
                        : Results.NoContent())
            .AllowAnonymous()
            .WithTags("Público")
            .WithSummary("Denuncia uma avaliação inadequada para moderação.")
            .WithDescription("Repetir a denúncia da mesma avaliação a partir do mesmo IP em 24 h não soma ao contador.");

        app.MapGet("/api/public/collection-points/{pointId:guid}/notices",
                async (Guid pointId, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new ListPublicNoticesQuery(pointId), ct))
                    .Match(value => Results.Ok(value)))
            .AllowAnonymous()
            .WithTags("Público")
            .Produces<IReadOnlyList<NoticeResponse>>()
            .WithSummary("Lista os avisos públicos do mural de um ponto de coleta.");

        app.MapPost("/api/public/collection-points/{pointId:guid}/donations",
                async (Guid pointId, PledgeDonationRequest request, ISender sender, CancellationToken ct) =>
                    (await sender.Send(
                        new PledgeDonationCommand(pointId, request.DonorName, request.DonorPhone, request.Description), ct))
                    .Match(value => Results.Created(
                        $"/api/collection-points/{pointId}/donations/{value.Id}", value)))
            .AllowAnonymous()
            .WithTags("Público")
            .Produces<DonationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .WithSummary("Avisa um ponto de coleta de que alguém pretende doar.");

        return app;
    }
}

/// <summary>Corpo do POST de avaliação (o ponto vem da rota).</summary>
public sealed record CreateReviewRequest(string AuthorName, int Rating, string? Comment);

/// <summary>Corpo do POST de doação prometida (o ponto vem da rota).</summary>
public sealed record PledgeDonationRequest(string DonorName, string? DonorPhone, string Description);
