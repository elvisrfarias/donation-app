using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using ErrorOr;

namespace DoacaoApp.Domain.Reviews;

/// <summary>
/// Avaliação da comunidade sobre um ponto de coleta — Aggregate Root. Registra a nota
/// (estrelas) e um comentário de quem já doou/retirou no ponto, sustentando o objetivo de
/// identificar os pontos mais bem avaliados. Referencia o ponto por Id (outro agregado).
/// </summary>
public sealed class Review : AggregateRoot<ReviewId>
{
    /// <summary>Nota mínima que uma avaliação pode receber.</summary>
    public const int MinRating = 1;

    /// <summary>Nota máxima que uma avaliação pode receber.</summary>
    public const int MaxRating = 5;

    /// <summary>Tamanho máximo do comentário livre.</summary>
    public const int MaxCommentLength = 1000;

    /// <summary>Tamanho máximo do nome de quem avalia.</summary>
    public const int MaxAuthorNameLength = 120;

    private Review()
    {
        AuthorName = string.Empty;
    }

    private Review(
        ReviewId id,
        CollectionPointId pointId,
        string authorName,
        int rating,
        string? comment,
        DateTime createdAtUtc) : base(id)
    {
        PointId = pointId;
        AuthorName = authorName;
        Rating = rating;
        Comment = comment;
        CreatedAtUtc = createdAtUtc;
    }

    public CollectionPointId PointId { get; private set; }
    public string AuthorName { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Oculta a avaliação do público (moderação); não conta na média nem no ranking.</summary>
    public bool IsHidden { get; private set; }

    /// <summary>Quantas vezes a avaliação foi denunciada pela comunidade.</summary>
    public int ReportCount { get; private set; }

    /// <summary>Oculta a avaliação (ação de moderação do Admin).</summary>
    public void Hide() => IsHidden = true;

    /// <summary>Reexibe uma avaliação ocultada.</summary>
    public void Restore() => IsHidden = false;

    /// <summary>Registra uma denúncia da comunidade sobre a avaliação.</summary>
    public void Report() => ReportCount++;

    public static ErrorOr<Review> Create(
        CollectionPointId pointId,
        string? authorName,
        int rating,
        string? comment,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(authorName))
        {
            return DomainErrors.Review.AuthorRequired;
        }

        if (rating < MinRating || rating > MaxRating)
        {
            return DomainErrors.Review.InvalidRating(MinRating, MaxRating);
        }

        var trimmedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmedComment is not null && trimmedComment.Length > MaxCommentLength)
        {
            return DomainErrors.Review.CommentTooLong(MaxCommentLength);
        }

        return new Review(
            ReviewId.New(),
            pointId,
            authorName.Trim(),
            rating,
            trimmedComment,
            createdAtUtc);
    }
}
