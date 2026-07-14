using DoacaoApp.Domain.Reviews;

namespace DoacaoApp.Application.Reviews.Contracts;

/// <summary>Representação pública de uma avaliação de ponto de coleta.</summary>
public sealed record ReviewResponse(
    Guid Id,
    Guid CollectionPointId,
    string AuthorName,
    int Rating,
    string? Comment,
    DateTime CreatedAtUtc);

public static class ReviewMappings
{
    public static ReviewResponse ToResponse(this Review review) =>
        new(
            review.Id.Value,
            review.PointId.Value,
            review.AuthorName,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc);
}
