using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("(11) 98765-4321", "11987654321", "(11) 98765-4321")]
    [InlineData("1133334444", "1133334444", "(11) 3333-4444")]
    public void Create_ComTelefoneValido_DeveNormalizarEFormatar(string input, string digits, string formatted)
    {
        var result = PhoneNumber.Create(input);

        result.IsError.Should().BeFalse();
        result.Value.Value.Should().Be(digits);
        result.Value.Formatted.Should().Be(formatted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_Vazio_DeveRetornarErroEmpty(string? input)
    {
        var result = PhoneNumber.Create(input);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PhoneNumber.Empty");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("119876543210")] // 12 dígitos
    public void Create_ComTamanhoInvalido_DeveRetornarErro(string input)
    {
        var result = PhoneNumber.Create(input);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PhoneNumber.InvalidFormat");
    }
}