using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.Families;

public class FamilyKinshipTests
{
    [Fact]
    public void AddKinship_Valido_DeveRegistrarSoNoLadoDoSujeito()
    {
        var family = TestData.Family();
        var subject = TestData.AddBeneficiary(family, TestData.ValidCpfA);
        var relatedId = BeneficiaryId.New();
        var relatedCpf = Cpf.Create(TestData.ValidCpfB).Value;

        var result = family.AddKinship(subject.Id, relatedId, "Maria", relatedCpf, KinshipType.Conjuge, TestData.Now);

        result.IsError.Should().BeFalse();
        subject.Kinships.Should().ContainSingle();
        var kinship = subject.Kinships[0];
        kinship.RelatedBeneficiaryId.Should().Be(relatedId);
        kinship.RelatedBeneficiaryName.Should().Be("Maria");
        kinship.RelatedBeneficiaryCpf.Should().Be(relatedCpf);
        kinship.Type.Should().Be(KinshipType.Conjuge);
        kinship.CreatedAtUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public void AddKinship_RelacionadoPodeSerDeOutraFamilia_NaoExigeQuePertencaAEstaFamilia()
    {
        var family = TestData.Family();
        var subject = TestData.AddBeneficiary(family, TestData.ValidCpfA);
        var relatedFromOtherFamily = BeneficiaryId.New();

        var result = family.AddKinship(
            subject.Id, relatedFromOtherFamily, "Pedro", Cpf.Create(TestData.ValidCpfB).Value, KinshipType.Irmao, TestData.Now);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void AddKinship_ComSiMesmo_DeveBloquear()
    {
        var family = TestData.Family();
        var subject = TestData.AddBeneficiary(family, TestData.ValidCpfA);

        var result = family.AddKinship(
            subject.Id, subject.Id, subject.Name, subject.Cpf, KinshipType.Irmao, TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.SelfKinshipNotAllowed");
        subject.Kinships.Should().BeEmpty();
    }

    [Fact]
    public void AddKinship_Duplicado_DeveBloquear()
    {
        var family = TestData.Family();
        var subject = TestData.AddBeneficiary(family, TestData.ValidCpfA);
        var relatedId = BeneficiaryId.New();
        var relatedCpf = Cpf.Create(TestData.ValidCpfB).Value;
        family.AddKinship(subject.Id, relatedId, "Maria", relatedCpf, KinshipType.Conjuge, TestData.Now);

        var result = family.AddKinship(subject.Id, relatedId, "Maria", relatedCpf, KinshipType.Conjuge, TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.KinshipAlreadyExists");
        subject.Kinships.Should().HaveCount(1);
    }

    [Fact]
    public void AddKinship_SujeitoNaoPertenceAFamilia_DeveRetornarNotFound()
    {
        var family = TestData.Family();

        var result = family.AddKinship(
            BeneficiaryId.New(), BeneficiaryId.New(), "Maria", Cpf.Create(TestData.ValidCpfB).Value, KinshipType.Outro, TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    [Fact]
    public void ReleaseBeneficiary_DeveRemoverDaListaERetornarOBeneficiario()
    {
        var family = TestData.Family();
        var beneficiary = TestData.AddBeneficiary(family, TestData.ValidCpfA);

        var released = family.ReleaseBeneficiary(beneficiary.Id);

        released.Should().Be(beneficiary);
        family.Beneficiaries.Should().BeEmpty();
    }

    [Fact]
    public void ReleaseBeneficiary_Inexistente_DeveRetornarNull()
    {
        var family = TestData.Family();

        var released = family.ReleaseBeneficiary(BeneficiaryId.New());

        released.Should().BeNull();
    }

    [Fact]
    public void AbsorbBeneficiary_DeveReatribuirFamilyIdEAdicionarNaLista()
    {
        var originFamily = TestData.Family();
        var beneficiary = TestData.AddBeneficiary(originFamily, TestData.ValidCpfA);
        originFamily.ReleaseBeneficiary(beneficiary.Id);
        var destinationFamily = Family.Create("Nova Família", TestData.Cycle(), TestData.Now).Value;

        destinationFamily.AbsorbBeneficiary(beneficiary);

        destinationFamily.Beneficiaries.Should().ContainSingle(b => b.Id == beneficiary.Id);
        beneficiary.FamilyId.Should().Be(destinationFamily.Id);
    }

    [Fact]
    public void FusaoPorConjuge_MoveAmbosParaNovaFamiliaEPreservaOsQueFicaram()
    {
        var silva = TestData.Family();
        var joao = TestData.AddBeneficiary(silva, TestData.ValidCpfA);
        var irmaDoJoao = silva.AddBeneficiary("Ana", Cpf.Create("529.982.247-25").Value, null, CollectionPointId.New(), TestData.Now).Value;

        var souza = Family.Create("Família Souza", TestData.Cycle(max: 2), TestData.Now).Value;
        var maria = souza.AddBeneficiary("Maria", Cpf.Create("874.980.700-10").Value, null, CollectionPointId.New(), TestData.Now).Value;

        var novaFamilia = Family.Create($"{joao.Name} & {maria.Name}", silva.Cycle, TestData.Now).Value;

        silva.ReleaseBeneficiary(joao.Id);
        souza.ReleaseBeneficiary(maria.Id);
        novaFamilia.AbsorbBeneficiary(joao);
        novaFamilia.AbsorbBeneficiary(maria);

        novaFamilia.Beneficiaries.Should().HaveCount(2);
        novaFamilia.Cycle.Should().Be(silva.Cycle);
        silva.Beneficiaries.Should().ContainSingle(b => b.Id == irmaDoJoao.Id);
        souza.Beneficiaries.Should().BeEmpty();
    }
}
