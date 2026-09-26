using DoacaoApp.Application.CollectionPoints.Contracts;
using DoacaoApp.Application.CollectionPoints.Queries;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints;

public class GetCollectionPointDashboardHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly FakeClock _clock = new(TestData.Now);

    private GetCollectionPointDashboardHandler CreateSut() =>
        new(_pointRepo.Object, _beneficiaryRepo.Object, _clock);

    private Task<ErrorOr.ErrorOr<CollectionPointDashboardResponse>> Query(
        CollectionPoint point, DateOnly? from = null, DateOnly? to = null) =>
        CreateSut().Handle(new GetCollectionPointDashboardQuery(point.Id.Value, from, to), CancellationToken.None);

    [Fact]
    public async Task Handle_SemPeriodo_UsaOMesAtualCompleto()
    {
        var point = GivenPoint();

        var result = await Query(point);

        result.Value.PeriodStart.Should().Be(new DateOnly(2026, 6, 1));
        result.Value.PeriodEnd.Should().Be(new DateOnly(2026, 6, 30));
        result.Value.Granularity.Should().Be(DashboardGranularity.Day);
        result.Value.WithdrawalsSeries.Should().HaveCount(30);
        _pointRepo.Verify(r => r.ListWithdrawalsBetweenAsync(
            point.Id,
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Handle_PeriodoCurto_AgrupaAsRetiradasPorDiaComDiasSemRetiradaZerados()
    {
        var point = GivenPoint();
        GivenWithdrawals(point,
            WithdrawalAt(point, new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc)),
            WithdrawalAt(point, new DateTime(2026, 6, 2, 15, 0, 0, DateTimeKind.Utc)),
            WithdrawalAt(point, new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc)));

        var result = await Query(point);

        var series = result.Value.WithdrawalsSeries;
        series.Single(s => s.Date == new DateOnly(2026, 6, 2)).Count.Should().Be(2);
        series.Single(s => s.Date == new DateOnly(2026, 6, 15)).Count.Should().Be(1);
        series.Single(s => s.Date == new DateOnly(2026, 6, 3)).Count.Should().Be(0);
        result.Value.WithdrawalsInPeriod.Should().Be(3);
    }

    [Fact]
    public async Task Handle_PeriodoLongo_AgrupaAsRetiradasPorMes()
    {
        var point = GivenPoint();
        GivenWithdrawals(point,
            WithdrawalAt(point, new DateTime(2026, 1, 20, 10, 0, 0, DateTimeKind.Utc)),
            WithdrawalAt(point, new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc)),
            WithdrawalAt(point, new DateTime(2026, 6, 3, 10, 0, 0, DateTimeKind.Utc)));

        var result = await Query(point, new DateOnly(2026, 1, 15), new DateOnly(2026, 6, 10));

        result.Value.Granularity.Should().Be(DashboardGranularity.Month);
        result.Value.WithdrawalsSeries.Select(s => (s.Date, s.Count)).Should().Equal(
            (new DateOnly(2026, 1, 1), 1),
            (new DateOnly(2026, 2, 1), 0),
            (new DateOnly(2026, 3, 1), 0),
            (new DateOnly(2026, 4, 1), 0),
            (new DateOnly(2026, 5, 1), 0),
            (new DateOnly(2026, 6, 1), 2));
    }

    [Fact]
    public async Task Handle_ConsideraOUltimoDiaDoPeriodoInteiro()
    {
        var point = GivenPoint();

        await Query(point, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 12));

        _pointRepo.Verify(r => r.ListWithdrawalsBetweenAsync(
            point.Id,
            new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Handle_ContaOsBeneficiariosDistintosAtendidosNoPeriodo()
    {
        var point = GivenPoint();
        var maria = TestData.Beneficiary("Maria");
        var joao = TestData.Beneficiary("João", TestData.ValidCpfB);
        GivenWithdrawals(point,
            point.RegisterWithdrawal(maria, [ItemCategory.Food], 0, TestData.Now, null).Value,
            point.RegisterWithdrawal(maria, [ItemCategory.Food], 0, TestData.Now.AddHours(-2), null).Value,
            point.RegisterWithdrawal(joao, [ItemCategory.Food], 0, TestData.Now.AddHours(-3), null).Value);

        var result = await Query(point);

        result.Value.WithdrawalsInPeriod.Should().Be(3);
        result.Value.BeneficiariesServedInPeriod.Should().Be(2);
    }

    [Fact]
    public async Task Handle_RetornaOsTotaisGeraisDoPonto()
    {
        var point = GivenPoint();
        _pointRepo.Setup(r => r.CountWithdrawalsAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(42);
        _beneficiaryRepo
            .Setup(r => r.ListByCollectionPointAsync(point.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.Beneficiary(), TestData.Beneficiary("Ana", TestData.ValidCpfB)]);

        var result = await Query(point);

        result.Value.TotalWithdrawals.Should().Be(42);
        result.Value.RegisteredBeneficiaries.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ListaAsUltimasRetiradasDoPeriodoComONomeDoBeneficiario()
    {
        var point = GivenPoint();
        var beneficiary = TestData.Beneficiary("Maria Souza");
        GivenWithdrawals(point, point.RegisterWithdrawal(beneficiary, [ItemCategory.Clothing], 0, TestData.Now, null).Value);
        _beneficiaryRepo
            .Setup(r => r.ListByIdsAsync(It.IsAny<IReadOnlyCollection<BeneficiaryId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([beneficiary]);

        var result = await Query(point);

        var recent = result.Value.RecentWithdrawals.Should().ContainSingle().Subject;
        recent.BeneficiaryName.Should().Be("Maria Souza");
        recent.OccurredAtUtc.Should().Be(TestData.Now);
        recent.Categories.Should().Equal("Clothing");
    }

    [Fact]
    public async Task Handle_MostraNoMaximoCincoRetiradasRecentes()
    {
        var point = GivenPoint();
        GivenWithdrawals(point, Enumerable.Range(0, 8)
            .Select(i => WithdrawalAt(point, TestData.Now.AddHours(-i))).ToArray());

        var result = await Query(point);

        result.Value.RecentWithdrawals.Should().HaveCount(5);
        result.Value.WithdrawalsInPeriod.Should().Be(8);
    }

    [Fact]
    public async Task Handle_PontoInexistente_RetornaNotFound()
    {
        _pointRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new GetCollectionPointDashboardQuery(Guid.NewGuid(), null, null), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Query_EhEscopadaAoPonto_ParaQueGestorVejaSoOProprioPainel()
    {
        var pointId = Guid.NewGuid();

        new GetCollectionPointDashboardQuery(pointId, null, null).Should().BeAssignableTo<IScopedToCollectionPoint>()
            .Which.CollectionPointId.Should().Be(pointId);
    }

    [Theory]
    [InlineData(2026, 6, 10, 2026, 6, 9, false)]
    [InlineData(2026, 6, 10, 2026, 6, 10, true)]
    [InlineData(2026, 1, 1, 2026, 12, 31, true)]
    [InlineData(2025, 1, 1, 2026, 12, 31, false)]
    public void Validator_ExigePeriodoNaOrdemCertaELimitadoAUmAno(
        int fromYear, int fromMonth, int fromDay, int toYear, int toMonth, int toDay, bool expectedValid)
    {
        var query = new GetCollectionPointDashboardQuery(
            Guid.NewGuid(), new DateOnly(fromYear, fromMonth, fromDay), new DateOnly(toYear, toMonth, toDay));

        new GetCollectionPointDashboardValidator().Validate(query).IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Validator_SemPeriodo_EhValido()
    {
        new GetCollectionPointDashboardValidator()
            .Validate(new GetCollectionPointDashboardQuery(Guid.NewGuid(), null, null))
            .IsValid.Should().BeTrue();
    }

    private CollectionPoint GivenPoint()
    {
        var point = TestData.Point();
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        GivenWithdrawals(point);
        _beneficiaryRepo
            .Setup(r => r.ListByCollectionPointAsync(point.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _beneficiaryRepo
            .Setup(r => r.ListByIdsAsync(It.IsAny<IReadOnlyCollection<BeneficiaryId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return point;
    }

    private void GivenWithdrawals(CollectionPoint point, params Withdrawal[] withdrawals) =>
        _pointRepo
            .Setup(r => r.ListWithdrawalsBetweenAsync(
                point.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(withdrawals.OrderByDescending(w => w.OccurredAtUtc).ToList());

    private static Withdrawal WithdrawalAt(CollectionPoint point, DateTime occurredAtUtc) =>
        point.RegisterWithdrawal(TestData.Beneficiary(), [ItemCategory.Food], 0, occurredAtUtc, null).Value;
}
