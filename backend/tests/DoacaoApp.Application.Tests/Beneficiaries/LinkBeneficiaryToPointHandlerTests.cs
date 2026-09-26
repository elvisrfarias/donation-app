using DoacaoApp.Application.Beneficiaries.Commands;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Beneficiaries;

public class LinkBeneficiaryToPointHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeClock _clock = new(TestData.Now);

    public LinkBeneficiaryToPointHandlerTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private LinkBeneficiaryToPointHandler CreateSut() =>
        new(_beneficiaryRepo.Object, _pointRepo.Object, _unitOfWork.Object, _clock);

    [Fact]
    public async Task Handle_BeneficiarioDeOutroPonto_VinculaAoPontoESalva()
    {
        var point = GivenPoint();
        var beneficiary = GivenBeneficiary(CollectionPointId.New());

        var result = await CreateSut().Handle(
            new LinkBeneficiaryToPointCommand(beneficiary.Id.Value, point.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.LinkedPointIds.Should().Contain(point.Id.Value).And.HaveCount(2);
        beneficiary.IsLinkedTo(point.Id).Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PontoJaVinculado_RetornaConflito()
    {
        var point = GivenPoint();
        var beneficiary = GivenBeneficiary(point.Id);

        var result = await CreateSut().Handle(
            new LinkBeneficiaryToPointCommand(beneficiary.Id.Value, point.Id.Value), CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.AlreadyLinkedToPoint");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BeneficiarioInexistente_RetornaNotFound()
    {
        var point = GivenPoint();
        _beneficiaryRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Beneficiary?)null);

        var result = await CreateSut().Handle(
            new LinkBeneficiaryToPointCommand(Guid.NewGuid(), point.Id.Value), CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    [Fact]
    public async Task Handle_PontoInexistente_RetornaNotFound()
    {
        var beneficiary = GivenBeneficiary(CollectionPointId.New());
        _pointRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new LinkBeneficiaryToPointCommand(beneficiary.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto_ParaQueGestorVinculeSoAoProprioPonto()
    {
        var pointId = Guid.NewGuid();

        new LinkBeneficiaryToPointCommand(Guid.NewGuid(), pointId).Should().BeAssignableTo<IScopedToCollectionPoint>()
            .Which.CollectionPointId.Should().Be(pointId);
    }

    private CollectionPoint GivenPoint()
    {
        var point = TestData.Point();
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        return point;
    }

    private Beneficiary GivenBeneficiary(CollectionPointId registeredByPointId)
    {
        var beneficiary = TestData.Beneficiary(pointId: registeredByPointId);
        _beneficiaryRepo.Setup(r => r.GetByIdAsync(beneficiary.Id, It.IsAny<CancellationToken>())).ReturnsAsync(beneficiary);
        return beneficiary;
    }
}
