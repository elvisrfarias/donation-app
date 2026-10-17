using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Authentication.Commands;
using DoacaoApp.Application.Authentication.Queries;
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
    private readonly Mock<ICurrentUser> _currentUser = new();

    public AuthenticationHandlersTests()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
    }

    private static AuthResult BuildAuthResult() => new(
        Guid.NewGuid(),
        "admin@doacao.local",
        "Admin",
        ["Admin"],
        null,
        "access-token",
        "refresh-token",
        Now.AddMinutes(15),
        Now.AddDays(7));

    private static UserResponse BuildUserResponse(Guid? collectionPointId = null, string role = "PointManager") =>
        new(Guid.NewGuid(), "novo@doacao.local", "Fulano", [role], collectionPointId, true);

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
            .ReturnsAsync(AuthErrors.InvalidCredentials);

        var handler = new LoginHandler(_identityService.Object);
        var result = await handler.Handle(
            new LoginCommand("admin@doacao.local", "senha-errada"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task RegisterUserHandler_MapeiaTodosOsCamposParaORegisterRequest()
    {
        var pointId = Guid.NewGuid();
        _identityService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildUserResponse());

        var handler = new RegisterUserHandler(_identityService.Object, _currentUser.Object);
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
    public async Task RegisterUserHandler_QuandoPointManagerTentaCriarAdmin_DeveBloquear()
    {
        var ownPointId = Guid.NewGuid();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId);

        var handler = new RegisterUserHandler(_identityService.Object, _currentUser.Object);
        var command = new RegisterUserCommand("novo@doacao.local", "SenhaForte1", "Fulano", "Admin", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Auth.InsufficientPermissionsToAssignRole");
        _identityService.Verify(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUserHandler_QuandoPointManagerCriaUsuarioParaOutroPonto_DeveBloquear()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(Guid.NewGuid());

        var handler = new RegisterUserHandler(_identityService.Object, _currentUser.Object);
        var command = new RegisterUserCommand("novo@doacao.local", "SenhaForte1", "Fulano", "Viewer", Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Scope.CollectionPointForbidden");
        _identityService.Verify(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUserHandler_QuandoPointManagerCriaViewerNoProprioPonto_DevePermitir()
    {
        var ownPointId = Guid.NewGuid();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId);
        _identityService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildUserResponse());

        var handler = new RegisterUserHandler(_identityService.Object, _currentUser.Object);
        var command = new RegisterUserCommand("novo@doacao.local", "SenhaForte1", "Fulano", "Viewer", ownPointId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        _identityService.Verify(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterUserHandler_DevolveSoOUsuarioCriado_SemTokensDeSessao()
    {
        var createdUser = BuildUserResponse(Guid.NewGuid());
        _identityService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        var handler = new RegisterUserHandler(_identityService.Object, _currentUser.Object);
        var command = new RegisterUserCommand(
            "novo@doacao.local", "SenhaForte1", "Fulano", "PointManager", createdUser.CollectionPointId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.Should().Be(createdUser);
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

    [Fact]
    public async Task ListUsersHandler_DevolveOsUsuariosDoIdentityService()
    {
        var users = new List<UserResponse> { new(Guid.NewGuid(), "a@x.com", "Fulano", ["Admin"], null, true) };
        _identityService.Setup(s => s.ListUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var handler = new ListUsersHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new ListUsersQuery(), CancellationToken.None);

        result.Value.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task ListUsersHandler_QuandoPointManager_DeveFiltrarPeloProprioPonto()
    {
        var ownPointId = Guid.NewGuid();
        var users = new List<UserResponse>
        {
            new(Guid.NewGuid(), "admin@x.com", "Admin", ["Admin"], null, true),
            new(Guid.NewGuid(), "mesmoponto@x.com", "Do Meu Ponto", ["Viewer"], ownPointId, true),
            new(Guid.NewGuid(), "outroponto@x.com", "De Outro Ponto", ["PointManager"], Guid.NewGuid(), true),
        };
        _identityService.Setup(s => s.ListUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId);

        var handler = new ListUsersHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new ListUsersQuery(), CancellationToken.None);

        result.Value.Should().ContainSingle().Which.Email.Should().Be("mesmoponto@x.com");
    }

    [Fact]
    public async Task UpdateUserHandler_MapeiaTodosOsCamposParaOUpdateUserRequest()
    {
        var userId = Guid.NewGuid();
        var pointId = Guid.NewGuid();
        _identityService
            .Setup(s => s.UpdateUserAsync(It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse(userId, "a@x.com", "Fulano", ["PointManager"], pointId, true));

        var handler = new UpdateUserHandler(_identityService.Object, _currentUser.Object);
        var command = new UpdateUserCommand(
            userId, "fulano@doacao.local", "Fulano", "(49) 99876-5432", "PointManager", pointId);

        await handler.Handle(command, CancellationToken.None);

        _identityService.Verify(s => s.UpdateUserAsync(
            It.Is<UpdateUserRequest>(r =>
                r.UserId == userId
                && r.Email == "fulano@doacao.local"
                && r.FullName == "Fulano"
                && r.PhoneNumber!.Value == "49998765432"
                && r.Role == "PointManager"
                && r.CollectionPointId == pointId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserHandler_TelefoneVazio_ApagaOTelefone()
    {
        _identityService
            .Setup(s => s.UpdateUserAsync(It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildUserResponse(role: "Admin"));

        var handler = new UpdateUserHandler(_identityService.Object, _currentUser.Object);
        await handler.Handle(
            new UpdateUserCommand(Guid.NewGuid(), "fulano@doacao.local", "Fulano", "  ", "Admin", null),
            CancellationToken.None);

        _identityService.Verify(s => s.UpdateUserAsync(
            It.Is<UpdateUserRequest>(r => r.PhoneNumber == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterUserHandler_RepassaOTelefoneSoComOsDigitos()
    {
        _identityService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildUserResponse(role: "Admin"));

        var handler = new RegisterUserHandler(_identityService.Object, _currentUser.Object);
        await handler.Handle(
            new RegisterUserCommand("novo@doacao.local", "SenhaForte1", "Fulano", "Admin", null, "(49) 3442-1234"),
            CancellationToken.None);

        _identityService.Verify(s => s.RegisterAsync(
            It.Is<RegisterRequest>(r => r.PhoneNumber!.Value == "4934421234"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserHandler_QuandoPointManagerEditaUsuarioDeOutroPonto_DeveBloquear()
    {
        var userId = Guid.NewGuid();
        var ownPointId = Guid.NewGuid();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId);
        _identityService
            .Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse(userId, "a@x.com", "Fulano", ["Viewer"], Guid.NewGuid(), true));

        var handler = new UpdateUserHandler(_identityService.Object, _currentUser.Object);
        var command = new UpdateUserCommand(userId, "fulano@doacao.local", "Fulano", null, "Viewer", ownPointId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Scope.CollectionPointForbidden");
        _identityService.Verify(s => s.UpdateUserAsync(It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserHandler_QuandoPointManagerTentaPromoverAAdmin_DeveBloquear()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(Guid.NewGuid());

        var handler = new UpdateUserHandler(_identityService.Object, _currentUser.Object);
        var command = new UpdateUserCommand(Guid.NewGuid(), "fulano@doacao.local", "Fulano", null, "Admin", null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Auth.InsufficientPermissionsToAssignRole");
        _identityService.Verify(s => s.UpdateUserAsync(It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateUserHandler_RepassaOUserIdERetornaSucesso()
    {
        var userId = Guid.NewGuid();
        _identityService.Setup(s => s.DeactivateUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success);

        var handler = new DeactivateUserHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new DeactivateUserCommand(userId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _identityService.Verify(s => s.DeactivateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateUserHandler_QuandoPointManagerDesativaUsuarioDeOutroPonto_DeveBloquear()
    {
        var userId = Guid.NewGuid();
        var ownPointId = Guid.NewGuid();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId);
        _identityService
            .Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse(userId, "a@x.com", "Fulano", ["PointManager"], Guid.NewGuid(), true));

        var handler = new DeactivateUserHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new DeactivateUserCommand(userId), CancellationToken.None);

        result.FirstError.Code.Should().Be("Scope.CollectionPointForbidden");
        _identityService.Verify(s => s.DeactivateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateUserHandler_QuandoPointManagerDesativaUsuarioDoProprioPonto_DevePermitir()
    {
        var userId = Guid.NewGuid();
        var ownPointId = Guid.NewGuid();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId);
        _identityService
            .Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResponse(userId, "a@x.com", "Fulano", ["Viewer"], ownPointId, true));
        _identityService.Setup(s => s.DeactivateUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success);

        var handler = new DeactivateUserHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new DeactivateUserCommand(userId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _identityService.Verify(s => s.DeactivateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordHandler_UsaOUsuarioAutenticadoEDevolveNovosTokens()
    {
        var userId = Guid.NewGuid();
        var authResult = BuildAuthResult();
        _currentUser.Setup(u => u.UserId).Returns(userId);
        _identityService
            .Setup(s => s.ChangePasswordAsync(It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        var handler = new ChangePasswordHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new ChangePasswordCommand("SenhaAtual1", "SenhaNova2"), CancellationToken.None);

        result.Value.Should().Be(authResult);
        _identityService.Verify(s => s.ChangePasswordAsync(
            It.Is<ChangePasswordRequest>(r =>
                r.UserId == userId && r.CurrentPassword == "SenhaAtual1" && r.NewPassword == "SenhaNova2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordHandler_SemUsuarioAutenticado_DeveRecusar()
    {
        _currentUser.Setup(u => u.UserId).Returns((Guid?)null);

        var handler = new ChangePasswordHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new ChangePasswordCommand("SenhaAtual1", "SenhaNova2"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Auth.NotAuthenticated");
        _identityService.Verify(
            s => s.ChangePasswordAsync(It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordHandler_QuandoSenhaAtualNaoConfere_PropagaOErro()
    {
        _currentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _identityService
            .Setup(s => s.ChangePasswordAsync(It.IsAny<ChangePasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthErrors.InvalidCurrentPassword);

        var handler = new ChangePasswordHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new ChangePasswordCommand("errada123", "SenhaNova2"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Auth.InvalidCurrentPassword");
    }

    [Fact]
    public async Task ResetUserPasswordHandler_QuandoAdmin_RedefineSemConsultarOPonto()
    {
        var userId = Guid.NewGuid();
        _identityService
            .Setup(s => s.ResetPasswordAsync(userId, "SenhaNova2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var handler = new ResetUserPasswordHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new ResetUserPasswordCommand(userId, "SenhaNova2"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _identityService.Verify(s => s.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _identityService.Verify(s => s.ResetPasswordAsync(userId, "SenhaNova2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetUserPasswordHandler_QuandoPointManagerRedefineUsuarioDoProprioPonto_DevePermitir()
    {
        var ownPointId = Guid.NewGuid();
        var target = BuildUserResponse(ownPointId, "Viewer");
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId);
        _identityService.Setup(s => s.GetUserByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        _identityService
            .Setup(s => s.ResetPasswordAsync(target.Id, "SenhaNova2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var handler = new ResetUserPasswordHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new ResetUserPasswordCommand(target.Id, "SenhaNova2"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _identityService.Verify(s => s.ResetPasswordAsync(target.Id, "SenhaNova2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetUserPasswordHandler_QuandoPointManagerRedefineUsuarioDeOutroPonto_DeveBloquear()
    {
        var target = BuildUserResponse(Guid.NewGuid(), "Viewer");
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(Guid.NewGuid());
        _identityService.Setup(s => s.GetUserByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);

        var handler = new ResetUserPasswordHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new ResetUserPasswordCommand(target.Id, "SenhaNova2"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Scope.CollectionPointForbidden");
        _identityService.Verify(
            s => s.ResetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetUserPasswordHandler_QuandoPointManagerSemPontoTentaRedefinirAdmin_DeveBloquear()
    {
        var admin = BuildUserResponse(null, "Admin");
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns((Guid?)null);
        _identityService.Setup(s => s.GetUserByIdAsync(admin.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admin);

        var handler = new ResetUserPasswordHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new ResetUserPasswordCommand(admin.Id, "SenhaNova2"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Scope.CollectionPointForbidden");
        _identityService.Verify(
            s => s.ResetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetUserPasswordHandler_QuandoUsuarioNaoExiste_PropagaNotFound()
    {
        var userId = Guid.NewGuid();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(Guid.NewGuid());
        _identityService
            .Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthErrors.UserNotFound);

        var handler = new ResetUserPasswordHandler(_identityService.Object, _currentUser.Object);
        var result = await handler.Handle(new ResetUserPasswordCommand(userId, "SenhaNova2"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Auth.UserNotFound");
    }
}
