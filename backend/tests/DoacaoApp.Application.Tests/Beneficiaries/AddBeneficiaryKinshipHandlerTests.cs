using DoacaoApp.Application.Beneficiaries.Commands;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Beneficiaries;

public class AddBeneficiaryKinshipHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly FakeClock _clock = new(TestData.Now);

    public AddBeneficiaryKinshipHandlerTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
    }

    private AddBeneficiaryKinshipHandler CreateSut() => new(_repo.Object, _unitOfWork.Object, _clock, _currentUser.Object);

    [Fact]
    public async Task Handle_Valido_DeveRegistrarOVinculoEspelhadoComTipoInverso()
    {
        var (subject, related) = GivenSubjectAndRelated();

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, TestData.ValidCpfB, KinshipType.Pai);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        subject.Kinships.Should().ContainSingle(k => k.RelatedBeneficiaryId == related.Id && k.Type == KinshipType.Pai);
        related.Kinships.Should().ContainSingle(k => k.RelatedBeneficiaryId == subject.Id && k.Type == KinshipType.Filho);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GestorDeOutroPonto_RetornaNotFoundSemTocarEmNenhumCadastro()
    {
        var (subject, related) = GivenSubjectAndRelated();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(CollectionPointId.New().Value);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, TestData.ValidCpfB, KinshipType.Pai);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
        subject.Kinships.Should().BeEmpty();
        related.Kinships.Should().BeEmpty();
        _repo.Verify(r => r.GetByCpfAsync(It.IsAny<Cpf>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GestorDoPontoVinculadoAoSujeito_RegistraOVinculo()
    {
        var pointId = CollectionPointId.New();
        var subject = TestData.Beneficiary("João", TestData.ValidCpfA, pointId);
        var related = TestData.Beneficiary("Maria", TestData.ValidCpfB);
        _repo.Setup(r => r.GetByIdAsync(subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _repo.Setup(r => r.GetByCpfAsync(related.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(related);
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(pointId.Value);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, TestData.ValidCpfB, KinshipType.Irmao);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        subject.Kinships.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ComSiMesmo_DeveBloquear()
    {
        var subject = TestData.Beneficiary();
        _repo.Setup(r => r.GetByIdAsync(subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _repo.Setup(r => r.GetByCpfAsync(subject.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(subject);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, TestData.ValidCpfA, KinshipType.Outro);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.SelfKinshipNotAllowed");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CpfRelacionadoNaoEncontrado_DeveRetornarErro()
    {
        var subject = TestData.Beneficiary();
        _repo.Setup(r => r.GetByIdAsync(subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _repo.Setup(r => r.GetByCpfAsync(It.IsAny<Cpf>(), It.IsAny<CancellationToken>())).ReturnsAsync((Beneficiary?)null);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, TestData.ValidCpfB, KinshipType.Outro);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.RelatedBeneficiaryNotFound");
    }

    [Fact]
    public async Task Handle_VinculoDuplicado_DeveBloquear()
    {
        var (subject, related) = GivenSubjectAndRelated();
        subject.AddKinship(related.Id, related.Name, related.Cpf, KinshipType.Irmao, TestData.Now);
        related.AddKinship(subject.Id, subject.Name, subject.Cpf, KinshipType.Irmao, TestData.Now);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, TestData.ValidCpfB, KinshipType.Irmao);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.KinshipAlreadyExists");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CpfInvalido_DeveRetornarErroDeValidacao()
    {
        var subject = TestData.Beneficiary();
        _repo.Setup(r => r.GetByIdAsync(subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(subject);

        var command = new AddBeneficiaryKinshipCommand(subject.Id.Value, "123", KinshipType.Outro);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Cpf.InvalidFormat");
    }

    [Fact]
    public async Task Handle_BeneficiarioSujeitoNaoEncontrado_DeveRetornarNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Beneficiary?)null);

        var command = new AddBeneficiaryKinshipCommand(Guid.NewGuid(), TestData.ValidCpfB, KinshipType.Outro);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    private (Beneficiary Subject, Beneficiary Related) GivenSubjectAndRelated()
    {
        var subject = TestData.Beneficiary("João", TestData.ValidCpfA);
        var related = TestData.Beneficiary("Maria", TestData.ValidCpfB);
        _repo.Setup(r => r.GetByIdAsync(subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _repo.Setup(r => r.GetByCpfAsync(related.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(related);
        return (subject, related);
    }
}
