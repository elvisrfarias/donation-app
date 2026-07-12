using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families;
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

    [Fact]
    public void UpdateBeneficiary_DadosValidos_DeveAtualizarCampos()
    {
        var family = TestData.Family();
        var beneficiary = family.AddBeneficiary(
            "João", Cpf.Create(TestData.ValidCpfA).Value, null, CollectionPointId.New(), TestData.Now).Value;
        var newPointId = CollectionPointId.New();
        var newBirthDate = new DateOnly(1990, 1, 1);

        var result = family.UpdateBeneficiary(
            beneficiary.Id, "João Silva", Cpf.Create(TestData.ValidCpfB).Value, newBirthDate, newPointId);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("João Silva");
        result.Value.Cpf.Should().Be(Cpf.Create(TestData.ValidCpfB).Value);
        result.Value.BirthDate.Should().Be(newBirthDate);
        result.Value.RegisteredByPointId.Should().Be(newPointId);
    }

    [Fact]
    public void UpdateBeneficiary_BeneficiarioInexistente_DeveRetornarNotFound()
    {
        var family = TestData.Family();

        var result = family.UpdateBeneficiary(
            BeneficiaryId.New(), "João", Cpf.Create(TestData.ValidCpfA).Value, null, CollectionPointId.New());

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    [Fact]
    public void UpdateBeneficiary_ComCpfDeOutroBeneficiarioDaFamilia_DeveBloquear()
    {
        var family = TestData.Family();
        var cpfA = Cpf.Create(TestData.ValidCpfA).Value;
        var cpfB = Cpf.Create(TestData.ValidCpfB).Value;
        family.AddBeneficiary("João", cpfA, null, CollectionPointId.New(), TestData.Now);
        var beneficiaryB = family.AddBeneficiary("Maria", cpfB, null, CollectionPointId.New(), TestData.Now).Value;

        var result = family.UpdateBeneficiary(beneficiaryB.Id, "Maria", cpfA, null, CollectionPointId.New());

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Family.DuplicatedBeneficiaryCpf");
    }

    [Fact]
    public void UpdateBeneficiary_MantendoOProprioCpf_DevePermitir()
    {
        var family = TestData.Family();
        var cpf = Cpf.Create(TestData.ValidCpfA).Value;
        var beneficiary = family.AddBeneficiary("João", cpf, null, CollectionPointId.New(), TestData.Now).Value;

        var result = family.UpdateBeneficiary(beneficiary.Id, "João Silva", cpf, null, CollectionPointId.New());

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void UpdateBeneficiary_SemNome_DeveRetornarErro()
    {
        var family = TestData.Family();
        var beneficiary = family.AddBeneficiary(
            "João", Cpf.Create(TestData.ValidCpfA).Value, null, CollectionPointId.New(), TestData.Now).Value;

        var result = family.UpdateBeneficiary(
            beneficiary.Id, "  ", Cpf.Create(TestData.ValidCpfA).Value, null, CollectionPointId.New());

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.NameRequired");
    }
}