using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointExactLocationTests
{
    private static Address AddressWithoutCoordinates() =>
        Address.Create("Rua das Flores", "100", null, "Centro", "São Paulo", "SP", "01001000").Value;

    private static Address AddressWithCoordinates(double latitude = -27.2342, double longitude = -52.0278) =>
        Address.Create("Rua Nova", "10", null, "Centro", "Concórdia", "SC", "89700000", latitude, longitude).Value;

    [Fact]
    public void Create_SemCoordenadas_DeveFalhar()
    {
        var result = CollectionPoint.Create(
            "Ponto", CollectionPointType.Church, AddressWithoutCoordinates(), TestData.Contact(), TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.ExactLocationRequired");
    }

    [Fact]
    public void Create_ComCoordenadas_GuardaALocalizacaoExata()
    {
        var result = CollectionPoint.Create(
            "Ponto", CollectionPointType.Church, AddressWithCoordinates(), TestData.Contact(), TestData.Now);

        result.IsError.Should().BeFalse();
        result.Value.Address.HasCoordinates.Should().BeTrue();
        result.Value.Address.Latitude.Should().Be(-27.2342);
    }

    [Fact]
    public void UpdateAddress_SemCoordenadas_DeveFalharEManterOEnderecoAnterior()
    {
        var point = TestData.Point();
        var original = point.Address;

        var result = point.UpdateAddress(AddressWithoutCoordinates());

        result.FirstError.Code.Should().Be("CollectionPoint.ExactLocationRequired");
        point.Address.Should().Be(original);
    }

    [Fact]
    public void UpdateAddress_ComCoordenadas_TrocaOEndereco()
    {
        var point = TestData.Point();
        var moved = AddressWithCoordinates(-27.1, -52.1);

        var result = point.UpdateAddress(moved);

        result.IsError.Should().BeFalse();
        point.Address.Latitude.Should().Be(-27.1);
        point.Address.Longitude.Should().Be(-52.1);
    }
}
