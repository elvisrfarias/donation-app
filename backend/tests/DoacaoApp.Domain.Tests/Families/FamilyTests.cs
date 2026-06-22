using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families.Events;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.Families;

public class FamilyTests
{
    [Fact]
    public void AddBeneficiary_Valido_DeveVincularEDispararEvento()
    {
        var family = TestData.Family();
        var pointId = CollectionPointId.New();

        var result = family.AddBeneficiary("João", Cpf.Create(TestData.ValidCpfA).Value, null, pointId, TestData.Now);

        result.IsError.Should().BeFalse();
        family.Beneficiaries.Should().HaveCount(1);
        family.DomainEvents.Should().ContainSingle(e => e is BeneficiaryLinkedToFamily);
    }

    [Fact]
    public void AddBeneficiary_ComCpfDuplicadoNaFamilia_DeveBloquear()
    {
        var family = TestData.Family();
        var cpf = Cpf.Create(TestData.ValidCpfA).Value;
        family.AddBeneficiary("João", cpf, null, CollectionPointId.New(), TestData.Now);

        var result = family.AddBeneficiary("Maria", cpf, null, CollectionPointId.New(), TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Family.DuplicatedBeneficiaryCpf");
        family.Beneficiaries.Should().HaveCount(1);
    }

    [Fact]
    public void AddBeneficiary_SemNome_DeveRetornarErro()
    {
        var family = TestData.Family();

        var result = family.AddBeneficiary("  ", Cpf.Create(TestData.ValidCpfA).Value, null, CollectionPointId.New(), TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.NameRequired");
    }
}