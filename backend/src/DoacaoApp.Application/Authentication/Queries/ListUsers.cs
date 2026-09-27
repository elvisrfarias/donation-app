using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using MediatR;

namespace DoacaoApp.Application.Authentication.Queries;

/// <summary>
/// Lista os usuários gerenciais. Admin vê todos; um PointManager vê só os usuários do seu próprio ponto
/// de coleta (o que exclui os Admins, que não têm ponto).
/// </summary>
public sealed record ListUsersQuery : IRequest<ErrorOr<IReadOnlyList<UserResponse>>>;

public sealed class ListUsersHandler(IIdentityService identityService, ICurrentUser currentUser)
    : IRequestHandler<ListUsersQuery, ErrorOr<IReadOnlyList<UserResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<UserResponse>>> Handle(
        ListUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await identityService.ListUsersAsync(cancellationToken);
        if (users.IsError || currentUser.IsAdmin)
        {
            return users;
        }

        return users.Value.Where(u => u.CollectionPointId == currentUser.CollectionPointId).ToList();
    }
}
