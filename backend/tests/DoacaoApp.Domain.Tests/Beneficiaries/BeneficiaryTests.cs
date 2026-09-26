using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.Beneficiaries.Events;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.Beneficiaries;

public class BeneficiaryTests
{
    private static Cpf CpfA => Cpf.Create(TestData.ValidCpfA).Value;
    private static Cpf CpfB => Cpf.Create(TestData.ValidCpfB).Value;
    private static readonly DateOnly Birth = new(1990, 5, 20);
    private static Email EmailA => Email.Create("joao@example.com").Value;
    private static Email EmailB => Email.Create("novo@example.com").Value;
    private static PhoneNumber Phone(string digits) => PhoneNumber.Create(digits).Value;

    private static Address HomeAddress() =>
        Address.Create("Rua das Flores", "100", "Apto 2", "Centro", "Concórdia", "SC", "89700-000").Value;

    [Fact]
    public void Create_Valido_DeveCriarAtivoEDispararEvento()
    {
        var pointId = CollectionPointId.New();

        var result = Beneficiary.Create("  João  ", CpfA, EmailA, [Phone("11987654321")], null, new DateOnly(1990, 5, 20), pointId, TestData.Now);

        result.IsError.Should().BeFalse();
        var beneficiary = result.Value;
        beneficiary.Name.Should().Be("João");
        beneficiary.Cpf.Should().Be(CpfA);
        beneficiary.Email.Should().Be(EmailA);
        beneficiary.Phones.Should().Equal(Phone("11987654321"));
        beneficiary.BirthDate.Should().Be(new DateOnly(1990, 5, 20));
        beneficiary.RegisteredByPointId.Should().Be(pointId);
        beneficiary.LinkedPointIds.Should().Equal(pointId);
        beneficiary.IsActive.Should().BeTrue();
        beneficiary.CreatedAtUtc.Should().Be(TestData.Now);
        beneficiary.DomainEvents.Should().ContainSingle(e => e is BeneficiaryRegistered);
    }

    [Fact]
    public void Create_SemTelefones_DeveRetornarErroPoisOContatoPrincipalEObrigatorio()
    {
        var result = Beneficiary.Create("João", CpfA, EmailA, [], null, Birth, CollectionPointId.New(), TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.PhoneRequired");
    }

    [Fact]
    public void Create_ComApenasOContatoPrincipal_DeveCriar()
    {
        var result = Beneficiary.Create(
            "João", CpfA, EmailA, [Phone("11987654321")], null, Birth, CollectionPointId.New(), TestData.Now);

        result.IsError.Should().BeFalse();
        result.Value.Phones.Should().HaveCount(1);
    }

    [Fact]
    public void Create_ComDoisTelefones_DeveGuardarOsDois()
    {
        var result = Beneficiary.Create(
            "João", CpfA, EmailA, [Phone("11987654321"), Phone("1134567890")], null, Birth, CollectionPointId.New(), TestData.Now);

        result.Value.Phones.Should().HaveCount(2);
    }

    [Fact]
    public void Create_ComMaisDeDoisTelefones_DeveRetornarErro()
    {
        var result = Beneficiary.Create(
            "João",
            CpfA,
            EmailA,
            [Phone("11987654321"), Phone("1134567890"), Phone("21987654321")],
            null,
            Birth,
            CollectionPointId.New(),
            TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.TooManyPhones");
    }

    [Fact]
    public void Create_ComTelefonesRepetidos_DeveGuardarUmaVezSo()
    {
        var result = Beneficiary.Create(
            "João", CpfA, EmailA, [Phone("11987654321"), Phone("11987654321")], null, Birth, CollectionPointId.New(), TestData.Now);

        result.Value.Phones.Should().Equal(Phone("11987654321"));
    }

    [Fact]
    public void UpdateDetails_SemTelefones_DeveRetornarErroEManterOsContatos()
    {
        var beneficiary = TestData.Beneficiary();
        var phonesBefore = beneficiary.Phones.ToList();

        var result = beneficiary.UpdateDetails("João", CpfB, EmailB, [], null, Birth);

        result.FirstError.Code.Should().Be("Beneficiary.PhoneRequired");
        beneficiary.Phones.Should().Equal(phonesBefore);
    }

    [Fact]
    public void UpdateDetails_ComMaisDeDoisTelefones_DeveRetornarErroEManterOsContatos()
    {
        var beneficiary = TestData.Beneficiary();
        var emailBefore = beneficiary.Email;

        var result = beneficiary.UpdateDetails(
            "João", CpfB, EmailB, [Phone("11987654321"), Phone("1134567890"), Phone("21987654321")], null, Birth);

        result.FirstError.Code.Should().Be("Beneficiary.TooManyPhones");
        beneficiary.Email.Should().Be(emailBefore);
        beneficiary.Phones.Should().HaveCount(1);
    }

    [Fact]
    public void Create_ComEndereco_DeveGuardarOEndereco()
    {
        var address = HomeAddress();

        var result = Beneficiary.Create(
            "João", CpfA, EmailA, [Phone("11987654321")], address, Birth, CollectionPointId.New(), TestData.Now);

        result.Value.Address.Should().Be(address);
    }

    [Fact]
    public void Create_SemEndereco_DeveManterOEnderecoNulo()
    {
        var result = Beneficiary.Create(
            "João", CpfA, EmailA, [Phone("11987654321")], null, Birth, CollectionPointId.New(), TestData.Now);

        result.Value.Address.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_DeveSubstituirEPermitirRemoverOEndereco()
    {
        var beneficiary = TestData.Beneficiary();
        var address = HomeAddress();

        beneficiary.UpdateDetails("João", CpfA, EmailA, [Phone("11987654321")], address, Birth);
        beneficiary.Address.Should().Be(address);

        beneficiary.UpdateDetails("João", CpfA, EmailA, [Phone("11987654321")], null, Birth);
        beneficiary.Address.Should().BeNull();
    }

    [Fact]
    public void Create_SemDataDeNascimento_DeveRetornarErro()
    {
        var result = Beneficiary.Create(
            "João", CpfA, EmailA, [Phone("11987654321")], null, null, CollectionPointId.New(), TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.BirthDateRequired");
    }

    [Fact]
    public void UpdateDetails_SemDataDeNascimento_DeveRetornarErroEManterOsDados()
    {
        var beneficiary = TestData.Beneficiary();
        var birthDateBefore = beneficiary.BirthDate;

        var result = beneficiary.UpdateDetails("Outro Nome", CpfB, EmailB, [Phone("11987654321")], null, null);

        result.FirstError.Code.Should().Be("Beneficiary.BirthDateRequired");
        beneficiary.BirthDate.Should().Be(birthDateBefore);
        beneficiary.Name.Should().Be("João Silva");
    }

    [Fact]
    public void Create_SemNome_DeveRetornarErro()
    {
        var result = Beneficiary.Create("  ", CpfA, EmailA, [Phone("11987654321")], null, Birth, CollectionPointId.New(), TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.NameRequired");
    }

    [Fact]
    public void UpdateDetails_DadosValidos_DeveAtualizarCampos()
    {
        var beneficiary = TestData.Beneficiary();
        var originalPointId = beneficiary.RegisteredByPointId;
        var newBirthDate = new DateOnly(1990, 1, 1);

        var result = beneficiary.UpdateDetails("João Silva", CpfB, EmailB, [Phone("1134567890")], null, newBirthDate);

        result.IsError.Should().BeFalse();
        beneficiary.Name.Should().Be("João Silva");
        beneficiary.Cpf.Should().Be(CpfB);
        beneficiary.Email.Should().Be(EmailB);
        beneficiary.Phones.Should().Equal(Phone("1134567890"));
        beneficiary.BirthDate.Should().Be(newBirthDate);
        beneficiary.RegisteredByPointId.Should().Be(originalPointId);
        beneficiary.LinkedPointIds.Should().Equal(originalPointId);
    }

    [Fact]
    public void UpdateDetails_SemNome_DeveRetornarErroEManterOsDados()
    {
        var beneficiary = TestData.Beneficiary();

        var result = beneficiary.UpdateDetails("  ", CpfB, EmailB, [Phone("11987654321")], null, Birth);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.NameRequired");
        beneficiary.Name.Should().Be("João Silva");
        beneficiary.Cpf.Should().Be(CpfA);
    }

    [Fact]
    public void LinkToPoint_PontoNovo_DeveVincularSemPerderOsAnteriores()
    {
        var beneficiary = TestData.Beneficiary();
        var originalPointId = beneficiary.RegisteredByPointId;
        var otherPointId = CollectionPointId.New();

        var result = beneficiary.LinkToPoint(otherPointId, TestData.Now);

        result.IsError.Should().BeFalse();
        beneficiary.LinkedPointIds.Should().BeEquivalentTo([originalPointId, otherPointId]);
        beneficiary.IsLinkedTo(otherPointId).Should().BeTrue();
        beneficiary.RegisteredByPointId.Should().Be(originalPointId);
    }

    [Fact]
    public void LinkToPoint_PontoJaVinculado_DeveRetornarConflito()
    {
        var beneficiary = TestData.Beneficiary();

        var result = beneficiary.LinkToPoint(beneficiary.RegisteredByPointId, TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.AlreadyLinkedToPoint");
        beneficiary.LinkedPointIds.Should().HaveCount(1);
    }

    [Fact]
    public void IsLinkedTo_PontoSemVinculo_DeveSerFalso()
    {
        TestData.Beneficiary().IsLinkedTo(CollectionPointId.New()).Should().BeFalse();
    }

    [Fact]
    public void Deactivate_DeveMarcarComoInativo()
    {
        var beneficiary = TestData.Beneficiary();

        beneficiary.Deactivate();

        beneficiary.IsActive.Should().BeFalse();
    }

    [Fact]
    public void AddKinship_Valido_DeveRegistrarOVinculo()
    {
        var beneficiary = TestData.Beneficiary(TestData.ValidCpfA);
        var relatedId = BeneficiaryId.New();

        var result = beneficiary.AddKinship(relatedId, "Maria", CpfB, KinshipType.Conjuge, TestData.Now);

        result.IsError.Should().BeFalse();
        var kinship = beneficiary.Kinships.Should().ContainSingle().Subject;
        kinship.RelatedBeneficiaryId.Should().Be(relatedId);
        kinship.RelatedBeneficiaryName.Should().Be("Maria");
        kinship.RelatedBeneficiaryCpf.Should().Be(CpfB);
        kinship.Type.Should().Be(KinshipType.Conjuge);
        kinship.CreatedAtUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public void AddKinship_ComSiMesmo_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();

        var result = beneficiary.AddKinship(beneficiary.Id, beneficiary.Name, beneficiary.Cpf, KinshipType.Irmao, TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.SelfKinshipNotAllowed");
        beneficiary.Kinships.Should().BeEmpty();
    }

    [Fact]
    public void AddKinship_ComOMesmoParenteDuasVezes_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var relatedId = BeneficiaryId.New();
        beneficiary.AddKinship(relatedId, "Maria", CpfB, KinshipType.Conjuge, TestData.Now);

        var result = beneficiary.AddKinship(relatedId, "Maria", CpfB, KinshipType.Irmao, TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.KinshipAlreadyExists");
        beneficiary.Kinships.Should().HaveCount(1);
    }
}
