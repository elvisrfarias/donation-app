using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Infrastructure.Authentication;
using DoacaoApp.Infrastructure.Persistence;
using System.Buffers.Text;
using System.Security.Cryptography;
using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Infrastructure.Identity;

/// <summary>
/// Implementação de <see cref="IIdentityService"/> sobre o ASP.NET Core Identity.
/// Orquestra criação/login de usuários, emissão de JWT e rotação de refresh tokens. Refresh tokens são
/// persistidos só como hash SHA-256; qualquer mudança sensível (senha, papel, ponto, desativação) troca o
/// security stamp e revoga todos os refresh tokens do usuário, derrubando as sessões existentes. Tokens de
/// "esqueci minha senha" seguem a mesma regra: só o hash vai para o banco, e cada um vale uma única vez.
/// </summary>
public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    JwtTokenGenerator tokenGenerator,
    IDateTimeProvider clock,
    IOptions<JwtOptions> jwtOptions,
    IOptions<SecurityOptions> securityOptions) : IIdentityService
{
    private const string DummyPasswordForTimingEqualization = "senha-inexistente-para-equalizar-tempo-1";

    private static readonly TimeSpan ConcurrentRotationGracePeriod = TimeSpan.FromSeconds(30);

    private const int PasswordResetTokenBytes = 32;

    private const int DeactivationLockoutMinimumYears = 100;

    private static string? _dummyPasswordHash;

    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    private readonly SecurityOptions _securityOptions = securityOptions.Value;

    public async Task<ErrorOr<UserResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
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

        return await ToUserResponseAsync(user);
    }

    public async Task<ErrorOr<AuthResult>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            VerifyAgainstDummyHash(password);
            return AuthErrors.InvalidCredentials;
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

        var stored = await FindRefreshTokenAsync(refreshToken, cancellationToken);
        if (stored is null)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        if (stored.IsRevoked)
        {
            if (!IsConcurrentRotationRace(stored, now))
            {
                await RevokeAllRefreshTokensAsync(stored.UserId, cancellationToken);
            }

            return AuthErrors.InvalidRefreshToken;
        }

        if (!stored.IsActive(now))
        {
            return AuthErrors.InvalidRefreshToken;
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return AuthErrors.InvalidRefreshToken;
        }

        stored.RevokedAtUtc = now;

        return await IssueTokensAsync(user, cancellationToken, replaced: stored);
    }

    public async Task<ErrorOr<Success>> RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await FindRefreshTokenAsync(refreshToken, cancellationToken);
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

    public async Task<ErrorOr<UserResponse>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        return await ToUserResponseAsync(user);
    }

    public async Task<ErrorOr<UserResponse>> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var accessChanged = user.CollectionPointId != request.CollectionPointId
            || currentRoles.Count != 1
            || currentRoles[0] != request.Role;

        user.FullName = request.FullName;
        user.CollectionPointId = request.CollectionPointId;

        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, request.Role);

        await userManager.UpdateAsync(user);

        if (accessChanged)
        {
            await EndAllSessionsAsync(user, cancellationToken);
        }

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
        await EndAllSessionsAsync(user, cancellationToken);

        return Result.Success;
    }

    public async Task<ErrorOr<AuthResult>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return AuthErrors.NotAuthenticated;
        }

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            await userManager.AccessFailedAsync(user);
            return AuthErrors.InvalidCurrentPassword;
        }

        var passwordSet = await ReplacePasswordAsync(user, request.NewPassword);
        if (passwordSet.IsError)
        {
            return passwordSet.Errors;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<ErrorOr<Success>> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var passwordSet = await ReplacePasswordAsync(user, newPassword);
        if (passwordSet.IsError)
        {
            return passwordSet.Errors;
        }

        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

        return Result.Success;
    }

    public async Task<PasswordResetTicket?> CreatePasswordResetTicketAsync(string email, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var user = await userManager.FindByEmailAsync(email);
        if (user is null || IsDeactivated(user, now) || await WasPasswordResetRecentlyRequestedAsync(user.Id, now, cancellationToken))
        {
            return null;
        }

        await InvalidatePasswordResetTokensAsync(user.Id, now, cancellationToken);

        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(PasswordResetTokenBytes));
        var expiresAt = now.AddMinutes(_securityOptions.PasswordResetTokenMinutes);

        dbContext.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = OpaqueTokenHasher.Hash(token),
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PasswordResetTicket(user.Email!, user.FullName, token, expiresAt);
    }

    public async Task<ErrorOr<Success>> ResetForgottenPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var claimed = await ClaimPasswordResetTokenAsync(token, now, cancellationToken);
        if (claimed is null)
        {
            return AuthErrors.InvalidPasswordResetToken;
        }

        var user = await userManager.FindByIdAsync(claimed.UserId.ToString());
        if (user is null || IsDeactivated(user, now))
        {
            return AuthErrors.InvalidPasswordResetToken;
        }

        var passwordSet = await ReplacePasswordAsync(user, newPassword);
        if (passwordSet.IsError)
        {
            await ReleasePasswordResetTokenAsync(claimed.Id, cancellationToken);
            return passwordSet.Errors;
        }

        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);
        await InvalidatePasswordResetTokensAsync(user.Id, now, cancellationToken);
        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);

        return Result.Success;
    }

    public async Task<bool> IsSessionValidAsync(Guid userId, string securityStamp, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        // Só a desativação (lockout permanente) derruba a sessão a cada requisição. O lockout
        // temporário por senhas erradas não: senão qualquer um que soubesse o e-mail de alguém
        // poderia expulsá-lo errando a senha 5 vezes. Ele continua barrando login e refresh.
        return user is not null
            && user.LockoutEnd != DateTimeOffset.MaxValue
            && string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal);
    }

    /// <summary>
    /// Valida a nova senha contra a política do Identity antes de tocar no hash (para nunca deixar o usuário
    /// sem senha) e grava hash + novo security stamp numa única atualização.
    /// </summary>
    private async Task<ErrorOr<Success>> ReplacePasswordAsync(ApplicationUser user, string newPassword)
    {
        var validationErrors = new List<IdentityError>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(userManager, user, newPassword);
            validationErrors.AddRange(validation.Errors);
        }

        if (validationErrors.Count > 0)
        {
            return AuthErrors.PasswordRejected(string.Join("; ", validationErrors.Select(e => e.Description)));
        }

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, newPassword);
        await userManager.UpdateSecurityStampAsync(user);

        return Result.Success;
    }

    private async Task EndAllSessionsAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        await userManager.UpdateSecurityStampAsync(user);
        await RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
    }

    private Task<int> RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        return dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, now), cancellationToken);
    }

    /// <summary>
    /// Marca o token como usado numa única atualização condicional, para dois envios simultâneos do mesmo link
    /// não trocarem a senha duas vezes. Devolve null se o token não existe, expirou ou já foi usado.
    /// </summary>
    private async Task<PasswordResetToken?> ClaimPasswordResetTokenAsync(string token, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var tokenHash = OpaqueTokenHasher.Hash(token);
        var stored = await dbContext.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        if (stored is null || !stored.IsUsable(nowUtc))
        {
            return null;
        }

        var claimedRows = await dbContext.PasswordResetTokens
            .Where(t => t.Id == stored.Id && t.UsedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAtUtc, nowUtc), cancellationToken);

        return claimedRows == 1 ? stored : null;
    }

    private Task<int> ReleasePasswordResetTokenAsync(Guid tokenId, CancellationToken cancellationToken) =>
        dbContext.PasswordResetTokens
            .Where(t => t.Id == tokenId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAtUtc, (DateTime?)null), cancellationToken);

    private Task<int> InvalidatePasswordResetTokensAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken) =>
        dbContext.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAtUtc == null && t.ExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAtUtc, nowUtc), cancellationToken);

    private Task<bool> WasPasswordResetRecentlyRequestedAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var cooldownStart = nowUtc.AddSeconds(-_securityOptions.PasswordResetCooldownSeconds);
        return dbContext.PasswordResetTokens.AnyAsync(t => t.UserId == userId && t.CreatedAtUtc > cooldownStart, cancellationToken);
    }

    /// <summary>
    /// Usuário desativado tem bloqueio sem fim; o bloqueio temporário por tentativas erradas de login não conta,
    /// e é justamente desfeito ao redefinir a senha.
    /// </summary>
    private static bool IsDeactivated(ApplicationUser user, DateTime nowUtc) =>
        user.LockoutEnd is { } lockoutEnd && lockoutEnd.UtcDateTime > nowUtc.AddYears(DeactivationLockoutMinimumYears);

    /// <summary>
    /// Reuso de um token já revogado indica roubo, e derruba todas as sessões do usuário. A exceção é um token
    /// recém-rotacionado apresentado de novo dentro de poucos segundos: é a corrida de duas abas renovando ao
    /// mesmo tempo, e só aquela requisição é recusada.
    /// </summary>
    private static bool IsConcurrentRotationRace(RefreshToken token, DateTime nowUtc) =>
        token.ReplacedByToken is not null
        && token.RevokedAtUtc is { } revokedAt
        && nowUtc - revokedAt < ConcurrentRotationGracePeriod;

    private Task<RefreshToken?> FindRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = OpaqueTokenHasher.Hash(refreshToken);
        return dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenHash, cancellationToken);
    }

    /// <summary>
    /// Gasta o mesmo custo de BCrypt de uma senha real quando o e-mail não existe ou a conta está bloqueada,
    /// para o tempo de resposta não revelar qual dos casos aconteceu.
    /// </summary>
    private void VerifyAgainstDummyHash(string password)
    {
        var dummyUser = new ApplicationUser();
        _dummyPasswordHash ??= userManager.PasswordHasher.HashPassword(dummyUser, DummyPasswordForTimingEqualization);
        userManager.PasswordHasher.VerifyHashedPassword(dummyUser, _dummyPasswordHash, password);
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
        if (string.IsNullOrEmpty(user.SecurityStamp))
        {
            await userManager.UpdateSecurityStampAsync(user);
        }

        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, accessExpires) = tokenGenerator.GenerateAccessToken(user, roles);

        var now = clock.UtcNow;
        var refreshValue = tokenGenerator.GenerateRefreshToken();
        var refreshHash = OpaqueTokenHasher.Hash(refreshValue);
        var refreshExpires = now.AddHours(_jwtOptions.RefreshTokenHours);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpires,
        });

        if (replaced is not null)
        {
            replaced.ReplacedByToken = refreshHash;
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