using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Infrastructure.Authentication;
using DoacaoApp.Infrastructure.Persistence;
using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Infrastructure.Identity;

/// <summary>
/// Implementação de <see cref="IIdentityService"/> sobre o ASP.NET Core Identity.
/// Orquestra criação/login de usuários, emissão de JWT e rotação de refresh tokens.
/// </summary>
public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    JwtTokenGenerator tokenGenerator,
    IDateTimeProvider clock,
    IOptions<JwtOptions> jwtOptions) : IIdentityService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<ErrorOr<AuthResult>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!Roles.IsValid(request.Role))
        {
            return AuthErrors.InvalidRole;
        }

        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return AuthErrors.EmailAlreadyUsed;
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            CollectionPointId = request.CollectionPointId,
            EmailConfirmed = true,
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return AuthErrors.RegistrationFailed(
                string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, request.Role);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<ErrorOr<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthErrors.AccountLockedOut;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return AuthErrors.InvalidCredentials;
        }

        await userManager.ResetAccessFailedCountAsync(user);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<ErrorOr<AuthResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var stored = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (stored is null || !stored.IsActive(now))
        {
            return AuthErrors.InvalidRefreshToken;
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        stored.RevokedAtUtc = now;

        return await IssueTokensAsync(user, cancellationToken, replaced: stored);
    }

    public async Task<ErrorOr<Success>> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (stored is null || stored.IsRevoked)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        stored.RevokedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    public async Task<ErrorOr<IReadOnlyList<UserResponse>>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.OrderBy(u => u.FullName).ToListAsync(cancellationToken);

        var responses = new List<UserResponse>(users.Count);
        foreach (var user in users)
        {
            responses.Add(await ToUserResponseAsync(user));
        }

        return ErrorOrFactory.From<IReadOnlyList<UserResponse>>(responses);
    }

    public async Task<ErrorOr<UserResponse>> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        user.FullName = request.FullName;
        user.CollectionPointId = request.CollectionPointId;

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, request.Role);

        await userManager.UpdateAsync(user);

        return await ToUserResponseAsync(user);
    }

    public async Task<ErrorOr<Success>> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        return Result.Success;
    }

    private async Task<UserResponse> ToUserResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var isActive = !await userManager.IsLockedOutAsync(user);

        return new UserResponse(user.Id, user.Email!, user.FullName, roles.ToList(), user.CollectionPointId, isActive);
    }

    private async Task<AuthResult> IssueTokensAsync(
        ApplicationUser user,
        CancellationToken cancellationToken,
        RefreshToken? replaced = null)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, accessExpires) = tokenGenerator.GenerateAccessToken(user, roles);

        var now = clock.UtcNow;
        var refreshValue = tokenGenerator.GenerateRefreshToken();
        var refreshExpires = now.AddHours(_jwtOptions.RefreshTokenHours);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshValue,
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpires,
        });

        if (replaced is not null)
        {
            replaced.ReplacedByToken = refreshValue;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            user.Id,
            user.Email!,
            user.FullName,
            roles.ToList(),
            user.CollectionPointId,
            accessToken,
            refreshValue,
            accessExpires,
            refreshExpires);
    }
}