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

public class UpdateBeneficiaryHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    public UpdateBeneficiaryHandlerTests()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private UpdateBeneficiaryHandler CreateSut() =>
        new(_beneficiaryRepo.Object, _pointRepo.Object, _unitOfWork.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_DadosValidos_DeveAtualizarESalvar()
    {
        var point = GivenPoint();
        var beneficiary = GivenBeneficiary(point.Id);

        var command = new UpdateBeneficiaryCommand(
            beneficiary.Id.Value, "João Silva", TestData.ValidCpfB, null, point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("João Silva");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CpfJaCadastradoEmOutroBeneficiario_DeveBloquear()
    {
        var point = GivenPoint();
        var beneficiary = GivenBeneficiary(point.Id);
        _beneficiaryRepo
            .Setup(r => r.ExistsWithCpfAsync(It.IsAny<Cpf>(), beneficiary.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new UpdateBeneficiaryCommand(
            beneficiary.Id.Value, "João Silva", TestData.ValidCpfB, null, point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.CpfAlreadyRegistered");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CpfInvalido_DeveRetornarErroDeValidacao()
    {
        var command = new UpdateBeneficiaryCommand(Guid.NewGuid(), "João", "123", null, Guid.NewGuid());

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Cpf.InvalidFormat");
    }

    [Fact]
    public async Task Handle_BeneficiarioInexistente_DeveRetornarNotFound()
    {
        _beneficiaryRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Beneficiary?)null);

        var command = new UpdateBeneficiaryCommand(Guid.NewGuid(), "João", TestData.ValidCpfA, null, Guid.NewGuid());

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_QuandoGestorEditaBeneficiarioDeOutroPonto_DeveRetornarNotFound()
    {
        var ownPoint = GivenPoint();
        var beneficiaryOfOtherPoint = GivenBeneficiary(CollectionPointId.New());
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPoint.Id.Value);

        var command = new UpdateBeneficiaryCommand(
            beneficiaryOfOtherPoint.Id.Value, "João", TestData.ValidCpfA, null, ownPoint.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private CollectionPoint GivenPoint()
    {
        var point = TestData.Point();
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        return point;
    }

    private Beneficiary GivenBeneficiary(CollectionPointId pointId)
    {
        var beneficiary = TestData.Beneficiary(pointId: pointId);
        _beneficiaryRepo.Setup(r => r.GetByIdAsync(beneficiary.Id, It.IsAny<CancellationToken>())).ReturnsAsync(beneficiary);
        return beneficiary;
    }
}
