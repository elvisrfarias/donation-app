using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Authentication.Queries;

/// <summary>Lista todos os usuários gerenciais, para a grid administrativa.</summary>
public sealed record ListUsersQuery : IRequest<ErrorOr<IReadOnlyList<UserResponse>>>;

public sealed class ListUsersHandler(IIdentityService identityService)
    : IRequestHandler<ListUsersQuery, ErrorOr<IReadOnlyList<UserResponse>>>
{
    public Task<ErrorOr<IReadOnlyList<UserResponse>>> Handle(ListUsersQuery query, CancellationToken cancellationToken) =>
        identityService.ListUsersAsync(cancellationToken);
}
