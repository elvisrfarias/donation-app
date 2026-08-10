using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Authentication.Commands;
using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Authentication;

public class AuthenticationHandlersTests
{
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IIdentityService> _identityService = new();

    private static AuthResult BuildAuthResult() => new(
        Guid.NewGuid(),
        "admin@doacao.local",
        "Admin",
        ["Admin"],
        "access-token",
        "refresh-token",
        Now.AddMinutes(15),
        Now.AddDays(7));

    [Fact]
    public async Task LoginHandler_RepassaCredenciaisEDevolveResultadoDoIdentityService()
    {
        var authResult = BuildAuthResult();
        _identityService
            .Setup(s => s.LoginAsync("admin@doacao.local", "Admin@123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        var handler = new LoginHandler(_identityService.Object);
        var result = await handler.Handle(
            new LoginCommand("admin@doacao.local", "Admin@123456"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(authResult);
    }

    [Fact]
    public async Task LoginHandler_QuandoIdentityServiceRetornaErro_PropagaOErroSemAlterar()
    {
        _identityService
            .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthErrors.AccountLockedOut);

        var handler = new LoginHandler(_identityService.Object);
        var result = await handler.Handle(
            new LoginCommand("admin@doacao.local", "senha-errada"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.AccountLockedOut");
    }

    [Fact]
    public async Task RegisterUserHandler_MapeiaTodosOsCamposParaORegisterRequest()
    {
        var pointId = Guid.NewGuid();
        _identityService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAuthResult());

        var handler = new RegisterUserHandler(_identityService.Object);
        var command = new RegisterUserCommand("novo@doacao.local", "SenhaForte1", "Fulano", "PointManager", pointId);

        await handler.Handle(command, CancellationToken.None);

        _identityService.Verify(s => s.RegisterAsync(
            It.Is<RegisterRequest>(r =>
                r.Email == "novo@doacao.local" &&
                r.Password == "SenhaForte1" &&
                r.FullName == "Fulano" &&
                r.Role == "PointManager" &&
                r.CollectionPointId == pointId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenHandler_RepassaOTokenEDevolveResultado()
    {
        var authResult = BuildAuthResult();
        _identityService
            .Setup(s => s.RefreshAsync("token-antigo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        var handler = new RefreshTokenHandler(_identityService.Object);
        var result = await handler.Handle(new RefreshTokenCommand("token-antigo"), CancellationToken.None);

        result.Value.Should().Be(authResult);
    }

    [Fact]
    public async Task RevokeTokenHandler_RepassaOTokenERetornaSucesso()
    {
        _identityService
            .Setup(s => s.RevokeAsync("token-a-revogar", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var handler = new RevokeTokenHandler(_identityService.Object);
        var result = await handler.Handle(new RevokeTokenCommand("token-a-revogar"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _identityService.Verify(s => s.RevokeAsync("token-a-revogar", It.IsAny<CancellationToken>()), Times.Once);
    }
}
