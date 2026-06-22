using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.ValueObjects;

public class CpfTests
{
    [Theory]
    [InlineData("111.444.777-35", "11144477735")]
    [InlineData("52998224725", "52998224725")]
    public void Create_ComCpfValido_DeveNormalizarESalvarApenasDigitos(string input, string expected)
    {
        var result = Cpf.Create(input);

        result.IsError.Should().BeFalse();
        result.Value.Value.Should().Be(expected);
    }

    [Fact]
    public void Create_ComCpfValido_DeveFormatarComMascara()
    {
        var result = Cpf.Create("11144477735");

        result.Value.Formatted.Should().Be("111.444.777-35");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Vazio_DeveRetornarErroEmpty(string? input)
    {
        var result = Cpf.Create(input);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Cpf.Empty");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    public void Create_ComTamanhoInvalido_DeveRetornarErroDeFormato(string input)
    {
        var result = Cpf.Create(input);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Cpf.InvalidFormat");
    }

    [Theory]
    [InlineData("11111111111")] // todos iguais
    [InlineData("12345678900")] // dígitos verificadores incorretos
    [InlineData("11144477700")]
    public void Create_ComDigitosVerificadoresInvalidos_DeveRetornarErro(string input)
    {
        var result = Cpf.Create(input);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Cpf.InvalidCheckDigits");
    }
}