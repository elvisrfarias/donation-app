using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Sponsors.Contracts;
using DoacaoApp.Domain.Sponsors;
using DoacaoApp.Domain.Storage;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Sponsors.Queries;

/// <summary>Patrocinadores ativos para a página pública, do maior patrocínio (Diamante) ao menor (Prata).</summary>
public sealed record ListPublicSponsorsQuery : IRequest<ErrorOr<IReadOnlyList<SponsorResponse>>>;

public sealed class ListPublicSponsorsHandler(
    ISponsorRepository repository,
    IStoredFileRepository fileRepository,
    IFileStorage storage)
    : IRequestHandler<ListPublicSponsorsQuery, ErrorOr<IReadOnlyList<SponsorResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<SponsorResponse>>> Handle(
        ListPublicSponsorsQuery query,
        CancellationToken cancellationToken)
    {
        var sponsors = await repository.ListActiveAsync(cancellationToken);
        return ErrorOrFactory.From(await sponsors.ToResponsesAsync(fileRepository, storage, cancellationToken));
    }
}
