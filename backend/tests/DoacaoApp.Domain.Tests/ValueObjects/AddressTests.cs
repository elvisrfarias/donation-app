using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Create_ComDadosValidos_DeveNormalizarUfECep()
    {
        var result = Address.Create("Rua A", "100", "Apto 2", "Centro", "São Paulo", "sp", "01001-000");

        result.IsError.Should().BeFalse();
        result.Value.State.Should().Be("SP");
        result.Value.ZipCode.Should().Be("01001000");
        result.Value.FormattedZipCode.Should().Be("01001-000");
    }

    [Fact]
    public void Create_SemNumero_DeveAssumirSemNumero()
    {
        var result = Address.Create("Rua A", null, null, "Centro", "São Paulo", "SP", "01001000");

        result.Value.Number.Should().Be("S/N");
    }

    [Theory]
    [InlineData(null, "São Paulo", "SP", "01001000")]
    [InlineData("Rua A", "", "SP", "01001000")]
    public void Create_Incompleto_DeveRetornarErro(string? street, string city, string state, string zip)
    {
        var result = Address.Create(street, "1", null, "Centro", city, state, zip);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Address.Incomplete");
    }

    [Fact]
    public void Create_ComUfInvalida_DeveRetornarErro()
    {
        var result = Address.Create("Rua A", "1", null, "Centro", "São Paulo", "ABC", "01001000");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Address.InvalidState");
    }

    [Fact]
    public void Create_ComCepInvalido_DeveRetornarErro()
    {
        var result = Address.Create("Rua A", "1", null, "Centro", "São Paulo", "SP", "123");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Address.InvalidZipCode");
    }
}