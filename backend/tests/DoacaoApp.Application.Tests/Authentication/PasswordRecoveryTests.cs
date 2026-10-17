using DoacaoApp.Application.Authentication;
using DoacaoApp.Application.Authentication.Commands;
using DoacaoApp.Application.Common.Abstractions;
using ErrorOr;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Authentication;

public class PasswordRecoveryTests
{
    private const string Email = "gestor@doacao.local";

    private readonly Mock<IIdentityService> _identityService = new();
    private readonly Mock<IPasswordResetNotifier> _notifier = new();

    private RequestPasswordResetHandler CreateRequestHandler() => new(_identityService.Object, _notifier.Object);

    [Fact]
    public async Task RequestPasswordResetHandler_QuandoOUsuarioExiste_EnviaOLinkParaEle()
    {
        var ticket = new PasswordResetTicket(Email, "Gestor", "token-aleatorio", DateTime.UtcNow.AddHours(1));
        _identityService
            .Setup(s => s.CreatePasswordResetTicketAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var result = await CreateRequestHandler().Handle(new RequestPasswordResetCommand(Email), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _notifier.Verify(n => n.NotifyAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetHandler_QuandoOUsuarioNaoExiste_RespondeSucessoSemEnviarNada()
    {
        _identityService
            .Setup(s => s.CreatePasswordResetTicketAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetTicket?)null);

        var result = await CreateRequestHandler().Handle(
            new RequestPasswordResetCommand("ninguem@doacao.local"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _notifier.Verify(
            n => n.NotifyAsync(It.IsAny<PasswordResetTicket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompletePasswordResetHandler_RepassaTokenENovaSenha()
    {
        _identityService
            .Setup(s => s.ResetForgottenPasswordAsync("token-aleatorio", "SenhaNova2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        var handler = new CompletePasswordResetHandler(_identityService.Object);
        var result = await handler.Handle(
            new CompletePasswordResetCommand("token-aleatorio", "SenhaNova2"), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task CompletePasswordResetHandler_QuandoOTokenEInvalido_PropagaOErro()
    {
        _identityService
            .Setup(s => s.ResetForgottenPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthErrors.InvalidPasswordResetToken);

        var handler = new CompletePasswordResetHandler(_identityService.Object);
        var result = await handler.Handle(
            new CompletePasswordResetCommand("token-usado", "SenhaNova2"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Auth.InvalidPasswordResetToken");
    }

    [Theory]
    [InlineData("")]
    [InlineData("nao-e-email")]
    public void RequestPasswordResetValidator_EmailInvalido_DeveFalhar(string email)
    {
        new RequestPasswordResetValidator().Validate(new RequestPasswordResetCommand(email)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("token", "SenhaNova2", true)]
    [InlineData("", "SenhaNova2", false)]
    [InlineData("token", "curta1", false)]
    [InlineData("token", "semnumeros", false)]
    public void CompletePasswordResetValidator_AplicaAPoliticaDeSenha(string token, string password, bool expectedValid)
    {
        new CompletePasswordResetValidator()
            .Validate(new CompletePasswordResetCommand(token, password))
            .IsValid.Should().Be(expectedValid);
    }
}
