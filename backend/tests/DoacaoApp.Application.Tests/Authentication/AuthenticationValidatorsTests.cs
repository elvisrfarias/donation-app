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
}
