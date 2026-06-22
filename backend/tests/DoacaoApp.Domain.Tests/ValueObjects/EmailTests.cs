using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("  USER@Example.COM ", "user@example.com")]
    public void Create_ComEmailValido_DeveNormalizar(string input, string expected)
    {
        var result = Email.Create(input);

        result.IsError.Should().BeFalse();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_Vazio_DeveRetornarErroEmpty(string? input)
    {
        var result = Email.Create(input);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Email.Empty");
    }

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("a@b")]
    [InlineData("a@@b.com")]
    [InlineData("espaco @x.com")]
    public void Create_ComFormatoInvalido_DeveRetornarErro(string input)
    {
        var result = Email.Create(input);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Email.InvalidFormat");
    }
}