using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Reviews.Contracts;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Reviews;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace DoacaoApp.Application.Reviews.Commands;

/// <summary>
/// Registra a avaliação da comunidade sobre um ponto de coleta (nota + comentário).
/// Endpoint público: quem já doou/retirou avalia o ponto, alimentando o ranking.
/// </summary>
public sealed record CreateReviewCommand(
    Guid CollectionPointId,
    string AuthorName,
    int Rating,
    string? Comment) : IRequest<ErrorOr<ReviewResponse>>;

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.CollectionPointId).NotEmpty();
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(Review.MaxAuthorNameLength);
        RuleFor(x => x.Rating).InclusiveBetween(Review.MinRating, Review.MaxRating);
        RuleFor(x => x.Comment).MaximumLength(Review.MaxCommentLength);
    }
}

public sealed class CreateReviewHandler(
    IReviewRepository reviewRepository,
    ICollectionPointRepository pointRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock)
    : IRequestHandler<CreateReviewCommand, ErrorOr<ReviewResponse>>
{
    public async Task<ErrorOr<ReviewResponse>> Handle(
        CreateReviewCommand command,
        CancellationToken cancellationToken)
    {
        var pointId = CollectionPointId.From(command.CollectionPointId);

        var point = await pointRepository.GetByIdAsync(pointId, cancellationToken);
        if (point is null)
        {
            return DomainErrors.CollectionPoint.NotFound;
        }

        var reviewResult = Review.Create(
            pointId, command.AuthorName, command.Rating, command.Comment, clock.UtcNow);

        if (reviewResult.IsError)
        {
            return reviewResult.Errors;
        }

        reviewRepository.Add(reviewResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return reviewResult.Value.ToResponse();
    }
}
