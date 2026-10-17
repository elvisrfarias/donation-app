using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Sponsors.Contracts;
using DoacaoApp.Domain.Sponsors;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Sponsors.Queries;

/// <summary>Todos os patrocinadores, ativos e inativos, para a tela de gestão (Admin).</summary>
public sealed record ListSponsorsQuery : IRequest<ErrorOr<IReadOnlyList<SponsorResponse>>>;

public sealed class ListSponsorsHandler(
    ISponsorRepository repository,
    IStoredFileRepository fileRepository,
    IFileStorage storage)
    : IRequestHandler<ListSponsorsQuery, ErrorOr<IReadOnlyList<SponsorResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<SponsorResponse>>> Handle(
        ListSponsorsQuery query,
        CancellationToken cancellationToken)
    {
        var sponsors = await repository.ListAllAsync(cancellationToken);
        return ErrorOrFactory.From(await sponsors.ToResponsesAsync(fileRepository, storage, cancellationToken));
    }
}
