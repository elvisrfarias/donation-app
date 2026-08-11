using DoacaoApp.Domain.Families;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.Families;

public class KinshipTypeTests
{
    [Theory]
    [InlineData(KinshipType.Conjuge, KinshipType.Conjuge)]
    [InlineData(KinshipType.Pai, KinshipType.Filho)]
    [InlineData(KinshipType.Filho, KinshipType.Pai)]
    [InlineData(KinshipType.Irmao, KinshipType.Irmao)]
    [InlineData(KinshipType.Avo, KinshipType.Neto)]
    [InlineData(KinshipType.Neto, KinshipType.Avo)]
    [InlineData(KinshipType.Tio, KinshipType.Sobrinho)]
    [InlineData(KinshipType.Sobrinho, KinshipType.Tio)]
    [InlineData(KinshipType.Primo, KinshipType.Primo)]
    [InlineData(KinshipType.Sogro, KinshipType.GenroOuNora)]
    [InlineData(KinshipType.GenroOuNora, KinshipType.Sogro)]
    [InlineData(KinshipType.Agregado, KinshipType.Agregado)]
    [InlineData(KinshipType.Outro, KinshipType.Outro)]
    public void Inverse_DeveRetornarOParentescoOpostoCorreto(KinshipType type, KinshipType expectedInverse)
    {
        type.Inverse().Should().Be(expectedInverse);
    }

    [Fact]
    public void Inverse_AplicadoDuasVezes_DeveVoltarAoTipoOriginal()
    {
        foreach (var type in Enum.GetValues<KinshipType>())
        {
            type.Inverse().Inverse().Should().Be(type, because: $"{type} deveria ser simétrico ao aplicar Inverse duas vezes");
        }
    }
}
