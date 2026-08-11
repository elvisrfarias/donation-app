using DoacaoApp.Application.Families.Commands;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Families;

public class AddBeneficiaryKinshipHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
    private const string SubjectCpf = "111.444.777-35";
    private const string RelatedCpf = "529.982.247-25";

    private readonly Mock<IFamilyRepository> _familyRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeClock _clock = new(Now);

    private AddBeneficiaryKinshipHandler CreateSut() =>
        new(_familyRepo.Object, _unitOfWork.Object, _clock);

    [Fact]
    public async Task Handle_MesmaFamilia_DeveRegistrarVinculoEspelhadoSemMoverNinguem()
    {
        var family = BuildFamily("Família Silva");
        var subject = family.AddBeneficiary("João", Cpf.Create(SubjectCpf).Value, null, CollectionPointId.New(), Now).Value;
        var related = family.AddBeneficiary("Maria", Cpf.Create(RelatedCpf).Value, null, CollectionPointId.New(), Now).Value;
        SetupLookup(subject.Id, family, cpf: Cpf.Create(RelatedCpf).Value, relatedFamily: family);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, RelatedCpf, KinshipType.Irmao);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        subject.Kinships.Should().ContainSingle(k => k.RelatedBeneficiaryId == related.Id && k.Type == KinshipType.Irmao);
        related.Kinships.Should().ContainSingle(k => k.RelatedBeneficiaryId == subject.Id && k.Type == KinshipType.Irmao);
        family.Beneficiaries.Should().HaveCount(2);
        _familyRepo.Verify(r => r.Add(It.IsAny<Family>()), Times.Never);
        _familyRepo.Verify(r => r.Remove(It.IsAny<Family>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ParentescoInformativoEntreFamiliasDiferentes_NaoMoveNinguemDeFamilia()
    {
        var silva = BuildFamily("Família Silva");
        var subject = silva.AddBeneficiary("João", Cpf.Create(SubjectCpf).Value, null, CollectionPointId.New(), Now).Value;
        var souza = BuildFamily("Família Souza");
        var related = souza.AddBeneficiary("Maria", Cpf.Create(RelatedCpf).Value, null, CollectionPointId.New(), Now).Value;
        SetupLookup(subject.Id, silva, Cpf.Create(RelatedCpf).Value, souza);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, RelatedCpf, KinshipType.Primo);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        subject.Kinships.Should().ContainSingle(k => k.Type == KinshipType.Primo);
        related.Kinships.Should().ContainSingle(k => k.Type == KinshipType.Primo);
        silva.Beneficiaries.Should().ContainSingle(b => b.Id == subject.Id);
        souza.Beneficiaries.Should().ContainSingle(b => b.Id == related.Id);
        _familyRepo.Verify(r => r.Add(It.IsAny<Family>()), Times.Never);
        _familyRepo.Verify(r => r.Remove(It.IsAny<Family>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConjugeDeFamiliasDiferentes_FormaNovaFamiliaComCotaDoMaisAntigo()
    {
        var silva = BuildFamily("Família Silva", max: 1);
        var joao = silva.AddBeneficiary("João", Cpf.Create(SubjectCpf).Value, null, CollectionPointId.New(), Now).Value;
        var irmaDoJoao = silva.AddBeneficiary("Ana", Cpf.Create("874.980.700-10").Value, null, CollectionPointId.New(), Now).Value;

        var souza = BuildFamily("Família Souza", max: 5);
        var maria = souza.AddBeneficiary("Maria", Cpf.Create(RelatedCpf).Value, null, CollectionPointId.New(), Now.AddDays(1)).Value;

        SetupLookup(joao.Id, silva, Cpf.Create(RelatedCpf).Value, souza);
        Family? capturedNewFamily = null;
        _familyRepo.Setup(r => r.Add(It.IsAny<Family>())).Callback<Family>(f => capturedNewFamily = f);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AddBeneficiaryKinshipCommand(joao.Id.Value, RelatedCpf, KinshipType.Conjuge);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        capturedNewFamily.Should().NotBeNull();
        capturedNewFamily!.Cycle.MaxWithdrawalsPerCycle.Should().Be(1, "João é mais antigo que Maria, a cota da nova família deve ser a dele");
        capturedNewFamily.Beneficiaries.Should().HaveCount(2);
        joao.FamilyId.Should().Be(capturedNewFamily.Id);
        maria.FamilyId.Should().Be(capturedNewFamily.Id);
        silva.Beneficiaries.Should().ContainSingle(b => b.Id == irmaDoJoao.Id, "quem ficou na família Silva continua nela");
        souza.Beneficiaries.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ConjugeDeixaFamiliaDeOrigemVazia_DeveRemoveLa()
    {
        var silva = BuildFamily("Família Silva");
        var joao = silva.AddBeneficiary("João", Cpf.Create(SubjectCpf).Value, null, CollectionPointId.New(), Now).Value;
        var souza = BuildFamily("Família Souza");
        var maria = souza.AddBeneficiary("Maria", Cpf.Create(RelatedCpf).Value, null, CollectionPointId.New(), Now).Value;
        SetupLookup(joao.Id, silva, Cpf.Create(RelatedCpf).Value, souza);
        _familyRepo.Setup(r => r.Add(It.IsAny<Family>()));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new AddBeneficiaryKinshipCommand(joao.Id.Value, RelatedCpf, KinshipType.Conjuge);

        await CreateSut().Handle(command, CancellationToken.None);

        _familyRepo.Verify(r => r.Remove(silva), Times.Once);
        _familyRepo.Verify(r => r.Remove(souza), Times.Once);
    }

    [Fact]
    public async Task Handle_ComSiMesmo_DeveBloquear()
    {
        var family = BuildFamily("Família Silva");
        var subject = family.AddBeneficiary("João", Cpf.Create(SubjectCpf).Value, null, CollectionPointId.New(), Now).Value;
        SetupLookup(subject.Id, family, Cpf.Create(SubjectCpf).Value, family);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, SubjectCpf, KinshipType.Outro);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.SelfKinshipNotAllowed");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CpfRelacionadoNaoEncontrado_DeveRetornarErro()
    {
        var family = BuildFamily("Família Silva");
        var subject = family.AddBeneficiary("João", Cpf.Create(SubjectCpf).Value, null, CollectionPointId.New(), Now).Value;
        _familyRepo.Setup(r => r.GetByBeneficiaryIdAsync(subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(family);
        _familyRepo.Setup(r => r.GetByBeneficiaryCpfAsync(It.IsAny<Cpf>(), It.IsAny<CancellationToken>())).ReturnsAsync((Family?)null);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, RelatedCpf, KinshipType.Outro);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.RelatedBeneficiaryNotFound");
    }

    [Fact]
    public async Task Handle_VinculoDuplicado_DeveBloquear()
    {
        var family = BuildFamily("Família Silva");
        var subject = family.AddBeneficiary("João", Cpf.Create(SubjectCpf).Value, null, CollectionPointId.New(), Now).Value;
        var related = family.AddBeneficiary("Maria", Cpf.Create(RelatedCpf).Value, null, CollectionPointId.New(), Now).Value;
        family.AddKinship(subject.Id, related.Id, related.Name, related.Cpf, KinshipType.Irmao, Now);
        family.AddKinship(related.Id, subject.Id, subject.Name, subject.Cpf, KinshipType.Irmao, Now);
        SetupLookup(subject.Id, family, Cpf.Create(RelatedCpf).Value, family);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, RelatedCpf, KinshipType.Irmao);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.KinshipAlreadyExists");
    }

    [Fact]
    public async Task Handle_CpfInvalido_DeveRetornarErroDeValidacao()
    {
        var family = BuildFamily("Família Silva");
        var subject = family.AddBeneficiary("João", Cpf.Create(SubjectCpf).Value, null, CollectionPointId.New(), Now).Value;
        _familyRepo.Setup(r => r.GetByBeneficiaryIdAsync(subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, "123", KinshipType.Outro);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Cpf.InvalidFormat");
    }

    [Fact]
    public async Task Handle_BeneficiarioSujeitoNaoEncontrado_DeveRetornarNotFound()
    {
        _familyRepo.Setup(r => r.GetByBeneficiaryIdAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Family?)null);

        var command = new AddBeneficiaryKinshipCommand(Guid.NewGuid(), RelatedCpf, KinshipType.Outro);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    private void SetupLookup(BeneficiaryId subjectId, Family subjectFamily, Cpf cpf, Family relatedFamily)
    {
        _familyRepo.Setup(r => r.GetByBeneficiaryIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(subjectFamily);
        _familyRepo.Setup(r => r.GetByBeneficiaryCpfAsync(cpf, It.IsAny<CancellationToken>())).ReturnsAsync(relatedFamily);
    }

    private static Family BuildFamily(string name, int max = 1) =>
        Family.Create(name, WithdrawalCycle.Create(CycleType.Monthly, max).Value, Now).Value;

}
