using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Reviews;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Reviews.Commands;

/// <summary>Denúncia pública de uma avaliação (incrementa o contador de denúncias).</summary>
public sealed record ReportReviewCommand(Guid ReviewId) : IRequest<ErrorOr<Success>>;

/// <summary>Oculta uma avaliação (moderação — Admin).</summary>
public sealed record HideReviewCommand(Guid ReviewId) : IRequest<ErrorOr<Success>>;

/// <summary>Reexibe uma avaliação previamente ocultada (Admin).</summary>
public sealed record RestoreReviewCommand(Guid ReviewId) : IRequest<ErrorOr<Success>>;

public sealed class ReportReviewHandler(IReviewRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<ReportReviewCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(ReportReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await repository.GetByIdAsync(ReviewId.From(command.ReviewId), cancellationToken);
        if (review is null)
        {
            return DomainErrors.Review.NotFound;
        }

        review.Report();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}

public sealed class HideReviewHandler(IReviewRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<HideReviewCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(HideReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await repository.GetByIdAsync(ReviewId.From(command.ReviewId), cancellationToken);
        if (review is null)
        {
            return DomainErrors.Review.NotFound;
        }

        review.Hide();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}

public sealed class RestoreReviewHandler(IReviewRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<RestoreReviewCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(RestoreReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await repository.GetByIdAsync(ReviewId.From(command.ReviewId), cancellationToken);
        if (review is null)
        {
            return DomainErrors.Review.NotFound;
        }

        review.Restore();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
