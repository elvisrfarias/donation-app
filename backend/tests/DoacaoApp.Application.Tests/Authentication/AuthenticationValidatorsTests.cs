using DoacaoApp.Application.Authentication.Commands;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Application.Tests.Authentication;

public class AuthenticationValidatorsTests
{
    [Fact]
    public void LoginValidator_DadosValidos_DevePassar()
    {
        var result = new LoginValidator().Validate(new LoginCommand("admin@doacao.local", "Admin@123456"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "senha123")]
    [InlineData("nao-e-email", "senha123")]
    [InlineData("admin@doacao.local", "")]
    public void LoginValidator_DadosInvalidos_DeveFalhar(string email, string password)
    {
        var result = new LoginValidator().Validate(new LoginCommand(email, password));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterUserValidator_DadosValidos_DevePassar()
    {
        var command = new RegisterUserCommand("novo@doacao.local", "SenhaForte1", "Novo Usuário", "Admin", null);

        var result = new RegisterUserValidator().Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterUserValidator_RoleInvalida_DeveFalhar()
    {
        var command = new RegisterUserCommand("novo@doacao.local", "SenhaForte1", "Novo Usuário", "SuperAdmin", null);

        var result = new RegisterUserValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterUserCommand.Role));
    }

    [Fact]
    public void RegisterUserValidator_SenhaCurta_DeveFalhar()
    {
        var command = new RegisterUserCommand("novo@doacao.local", "curta1", "Novo Usuário", "Admin", null);

        var result = new RegisterUserValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterUserCommand.Password));
    }

    [Fact]
    public void RegisterUserValidator_AdminComPontoInformado_DeveFalhar()
    {
        var command = new RegisterUserCommand(
            "novo@doacao.local", "SenhaForte1", "Novo Usuário", "Admin", Guid.NewGuid());

        var result = new RegisterUserValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterUserCommand.CollectionPointId));
    }

    [Theory]
    [InlineData("PointManager")]
    [InlineData("Viewer")]
    public void RegisterUserValidator_GestorOuAjudanteSemPonto_DeveFalhar(string role)
    {
        var command = new RegisterUserCommand("novo@doacao.local", "SenhaForte1", "Novo Usuário", role, null);

        var result = new RegisterUserValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterUserCommand.CollectionPointId));
    }

    [Theory]
    [InlineData("PointManager")]
    [InlineData("Viewer")]
    public void RegisterUserValidator_GestorOuAjudanteComPonto_DevePassar(string role)
    {
        var command = new RegisterUserCommand(
            "novo@doacao.local", "SenhaForte1", "Novo Usuário", role, Guid.NewGuid());

        var result = new RegisterUserValidator().Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateUserValidator_AdminComPontoInformado_DeveFalhar()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), "Fulano", "Admin", Guid.NewGuid());

        var result = new UpdateUserValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.CollectionPointId));
    }

    [Theory]
    [InlineData("PointManager")]
    [InlineData("Viewer")]
    public void UpdateUserValidator_GestorOuAjudanteSemPonto_DeveFalhar(string role)
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), "Fulano", role, null);

        var result = new UpdateUserValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserCommand.CollectionPointId));
    }

    [Fact]
    public void UpdateUserValidator_DadosValidos_DevePassar()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), "Fulano", "Admin", null);

        var result = new UpdateUserValidator().Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RefreshTokenValidator_TokenVazio_DeveFalhar()
    {
        var result = new RefreshTokenValidator().Validate(new RefreshTokenCommand(""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RevokeTokenValidator_TokenVazio_DeveFalhar()
    {
        var result = new RevokeTokenValidator().Validate(new RevokeTokenCommand(""));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterUserValidator_SenhaSemNumero_DeveFalhar()
    {
        var command = new RegisterUserCommand("novo@doacao.local", "SenhaSemNumero", "Novo Usuário", "Admin", null);

        var result = new RegisterUserValidator().Validate(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterUserCommand.Password));
    }

    [Fact]
    public void ChangePasswordValidator_DadosValidos_DevePassar()
    {
        var result = new ChangePasswordValidator().Validate(new ChangePasswordCommand("SenhaAtual1", "SenhaNova2"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "SenhaNova2", nameof(ChangePasswordCommand.CurrentPassword))]
    [InlineData("SenhaAtual1", "curta1", nameof(ChangePasswordCommand.NewPassword))]
    [InlineData("SenhaAtual1", "SenhaSemNumero", nameof(ChangePasswordCommand.NewPassword))]
    [InlineData("SenhaAtual1", "SenhaAtual1", nameof(ChangePasswordCommand.NewPassword))]
    public void ChangePasswordValidator_DadosInvalidos_DeveFalhar(string currentPassword, string newPassword, string property)
    {
        var result = new ChangePasswordValidator().Validate(new ChangePasswordCommand(currentPassword, newPassword));

        result.Errors.Should().Contain(e => e.PropertyName == property);
    }

    [Fact]
    public void ResetUserPasswordValidator_DadosValidos_DevePassar()
    {
        var result = new ResetUserPasswordValidator().Validate(new ResetUserPasswordCommand(Guid.NewGuid(), "SenhaNova2"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("curta1")]
    [InlineData("SenhaSemNumero")]
    public void ResetUserPasswordValidator_SenhaForaDaPolitica_DeveFalhar(string newPassword)
    {
        var result = new ResetUserPasswordValidator().Validate(new ResetUserPasswordCommand(Guid.NewGuid(), newPassword));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetUserPasswordCommand.NewPassword));
    }

    [Fact]
    public void ResetUserPasswordValidator_SemUsuario_DeveFalhar()
    {
        var result = new ResetUserPasswordValidator().Validate(new ResetUserPasswordCommand(Guid.Empty, "SenhaNova2"));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetUserPasswordCommand.UserId));
    }
}
