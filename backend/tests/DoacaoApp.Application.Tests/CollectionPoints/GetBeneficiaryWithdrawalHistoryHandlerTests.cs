using DoacaoApp.Application.CollectionPoints.Queries;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints;

public class GetBeneficiaryWithdrawalHistoryHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly FakeClock _clock = new(TestData.Now);

    private GetBeneficiaryWithdrawalHistoryHandler CreateSut() =>
        new(_beneficiaryRepo.Object, _pointRepo.Object, _clock);

    [Fact]
    public async Task Handle_RetornaOSaldoDoCicloSegundoARegraDoPontoConsultado()
    {
        var point = GivenPoint(maxPerCycle: 3);
        var beneficiary = GivenBeneficiaryWithUsage(point, usedInCycle: 1);

        var result = await CreateSut().Handle(
            new GetBeneficiaryWithdrawalHistoryQuery(beneficiary.Id.Value, point.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.MaxPerCycle.Should().Be(3);
        result.Value.UsedInCurrentCycle.Should().Be(1);
        result.Value.RemainingInCurrentCycle.Should().Be(2);
    }

    [Fact]
    public async Task Handle_InformaQueOLimiteEPorCestaBasicaEListaTodasAsRetiradas()
    {
        var point = GivenPoint(maxPerCycle: 3);
        var beneficiary = GivenBeneficiaryWithUsage(point, usedInCycle: 1);
        var clothing = point.RegisterWithdrawal(beneficiary, [ItemCategory.Clothing], 0, TestData.Now, null).Value;
        var food = point.RegisterWithdrawal(beneficiary, [ItemCategory.Food], 0, TestData.Now.AddDays(-1), null).Value;
        _pointRepo
            .Setup(r => r.GetBeneficiaryWithdrawalHistoryAsync(beneficiary.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([clothing, food]);

        var result = await CreateSut().Handle(
            new GetBeneficiaryWithdrawalHistoryQuery(beneficiary.Id.Value, point.Id.Value), CancellationToken.None);

        result.Value.LimitedCategory.Should().Be("Basket");
        result.Value.Withdrawals.SelectMany(w => w.Categories).Should().Equal("Clothing", "Food");
    }

    [Fact]
    public async Task Handle_QuandoOUsoPassaDoLimite_ORestanteNuncaFicaNegativo()
    {
        var point = GivenPoint(maxPerCycle: 1);
        var beneficiary = GivenBeneficiaryWithUsage(point, usedInCycle: 2);

        var result = await CreateSut().Handle(
            new GetBeneficiaryWithdrawalHistoryQuery(beneficiary.Id.Value, point.Id.Value), CancellationToken.None);

        result.Value.RemainingInCurrentCycle.Should().Be(0);
    }

    [Fact]
    public async Task Handle_BeneficiarioInexistente_RetornaNotFound()
    {
        var point = GivenPoint(maxPerCycle: 1);
        _beneficiaryRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Beneficiary?)null);

        var result = await CreateSut().Handle(
            new GetBeneficiaryWithdrawalHistoryQuery(Guid.NewGuid(), point.Id.Value), CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    [Fact]
    public async Task Handle_PontoInexistente_RetornaNotFound()
    {
        var beneficiary = TestData.Beneficiary();
        _beneficiaryRepo.Setup(r => r.GetByIdAsync(beneficiary.Id, It.IsAny<CancellationToken>())).ReturnsAsync(beneficiary);
        _pointRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new GetBeneficiaryWithdrawalHistoryQuery(beneficiary.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Query_EhEscopadaAoPonto_ParaQueGestorConsulteSoPeloProprioPonto()
    {
        var pointId = Guid.NewGuid();

        var query = new GetBeneficiaryWithdrawalHistoryQuery(Guid.NewGuid(), pointId);

        query.Should().BeAssignableTo<IScopedToCollectionPoint>()
            .Which.CollectionPointId.Should().Be(pointId);
    }

    private CollectionPoint GivenPoint(int maxPerCycle)
    {
        var point = TestData.Point(maxPerCycle);
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        return point;
    }

    private Beneficiary GivenBeneficiaryWithUsage(CollectionPoint point, int usedInCycle)
    {
        var beneficiary = TestData.Beneficiary();
        _beneficiaryRepo.Setup(r => r.GetByIdAsync(beneficiary.Id, It.IsAny<CancellationToken>())).ReturnsAsync(beneficiary);
        _pointRepo
            .Setup(r => r.CountBeneficiaryBasketWithdrawalsAsync(
                beneficiary.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usedInCycle);
        _pointRepo
            .Setup(r => r.GetBeneficiaryWithdrawalHistoryAsync(beneficiary.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return beneficiary;
    }
}
