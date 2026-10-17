using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Errors;
using DoacaoApp.Domain.Sponsors;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Sponsors.Commands;

/// <summary>Remove o patrocinador de vez. Para só tirá-lo do site por um tempo, desative-o.</summary>
public sealed record DeleteSponsorCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;

public sealed class DeleteSponsorHandler(ISponsorRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteSponsorCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeleteSponsorCommand command, CancellationToken cancellationToken)
    {
        var sponsor = await repository.GetByIdAsync(SponsorId.From(command.Id), cancellationToken);
        if (sponsor is null)
        {
            return DomainErrors.Sponsor.NotFound;
        }

        repository.Remove(sponsor);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
