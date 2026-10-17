using DoacaoApp.Application.CollectionPoints.Queries;
using DoacaoApp.Application.Common.Abstractions;
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
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly FakeClock _clock = new(TestData.Now);

    public GetBeneficiaryWithdrawalHistoryHandlerTests() => _currentUser.Setup(u => u.IsAdmin).Returns(true);

    private GetBeneficiaryWithdrawalHistoryHandler CreateSut() =>
        new(_beneficiaryRepo.Object, _pointRepo.Object, _clock, _currentUser.Object);

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
    public async Task Handle_InformaQueOLimiteEPorCestaBasicaEListaTodasAsRetiradasComOItemDenormalizado()
    {
        var point = GivenPoint(maxPerCycle: 3);
        var beneficiary = GivenBeneficiaryWithUsage(point, usedInCycle: 1);
        var clothingCategory = point.AddCategory("Roupas", "shirt").Value;
        var foodCategory = point.AddCategory("Alimentos", "food").Value;
        var clothingItem = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: clothingCategory);
        var foodItem = TestData.AddStockItem(point, "Arroz", "kg", 10, categoryId: foodCategory);
        var clothing = point.RegisterWithdrawal(beneficiary, [TestData.Item(clothingItem)], 0, TestData.Now, null).Value;
        var food = point.RegisterWithdrawal(beneficiary, [TestData.Item(foodItem)], 0, TestData.Now.AddDays(-1), null).Value;
        GivenHistory(beneficiary, [clothing, food], point);

        var result = await CreateSut().Handle(
            new GetBeneficiaryWithdrawalHistoryQuery(beneficiary.Id.Value, point.Id.Value), CancellationToken.None);

        result.Value.LimitedCategory.Should().Be("Basket");
        result.Value.Withdrawals.SelectMany(w => w.Items).Select(i => i.CategoryName).Should().Equal("Roupas", "Alimentos");
    }

    [Fact]
    public async Task Handle_ResolveNomeUnidadeECategoriaDoItemQuandoAsRetiradasSaoDeDoisPontosDiferentes()
    {
        var pointA = GivenPoint(maxPerCycle: 3);
        var pointB = TestData.Point(maxPerCycle: 3);
        var beneficiary = GivenBeneficiaryWithUsage(pointA, usedInCycle: 0);

        var categoryA = pointA.AddCategory("Roupas", "shirt").Value;
        var itemA = TestData.AddStockItem(pointA, "Camiseta", "un", 10, categoryId: categoryA);
        var categoryB = pointB.AddCategory("Alimentos", "food").Value;
        var itemB = TestData.AddStockItem(pointB, "Arroz", "kg", 10, categoryId: categoryB);

        var withdrawalAtA = pointA.RegisterWithdrawal(beneficiary, [TestData.Item(itemA, 1)], 0, TestData.Now, null).Value;
        var withdrawalAtB = pointB.RegisterWithdrawal(
            beneficiary, [TestData.Item(itemB, 2)], 0, TestData.Now.AddDays(-1), null).Value;
        GivenHistory(beneficiary, [withdrawalAtA, withdrawalAtB], pointA, pointB);

        var result = await CreateSut().Handle(
            new GetBeneficiaryWithdrawalHistoryQuery(beneficiary.Id.Value, pointA.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        var items = result.Value.Withdrawals.SelectMany(w => w.Items).ToList();
        items.Should().ContainSingle(i =>
            i.StockItemId == itemA.Value && i.ItemName == "Camiseta" && i.Unit == "un"
            && i.CategoryName == "Roupas" && i.CategoryIcon == "shirt" && i.Quantity == 1);
        items.Should().ContainSingle(i =>
            i.StockItemId == itemB.Value && i.ItemName == "Arroz" && i.Unit == "kg"
            && i.CategoryName == "Alimentos" && i.CategoryIcon == "food" && i.Quantity == 2);
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
    public async Task Handle_GestorConsultandoBeneficiarioNaoVinculadoAoSeuPonto_RetornaNotFound()
    {
        var point = GivenPoint(maxPerCycle: 3);
        var beneficiary = GivenBeneficiaryWithUsage(point, usedInCycle: 0);
        GivenManagerOf(point);

        var result = await CreateSut().Handle(
            new GetBeneficiaryWithdrawalHistoryQuery(beneficiary.Id.Value, point.Id.Value), CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
        _pointRepo.Verify(
            r => r.GetBeneficiaryWithdrawalHistoryAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_GestorConsultandoBeneficiarioVinculadoAoSeuPonto_RetornaOHistorico()
    {
        var point = GivenPoint(maxPerCycle: 3);
        var beneficiary = GivenBeneficiaryWithUsage(point, usedInCycle: 0);
        beneficiary.LinkToPoint(point.Id, TestData.Now);
        GivenManagerOf(point);

        var result = await CreateSut().Handle(
            new GetBeneficiaryWithdrawalHistoryQuery(beneficiary.Id.Value, point.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
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

    private void GivenManagerOf(CollectionPoint point)
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(point.Id.Value);
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
        GivenHistory(beneficiary, [], point);
        return beneficiary;
    }

    /// <summary>
    /// Simula o repositório real: o histórico municipal devolve as retiradas tal como estão, e os
    /// itens/categorias resolvidos vêm de consultas separadas por ids, varrendo os catálogos de todos
    /// os pontos envolvidos no cenário (um só ponto no caso comum, vários no caso cruzado).
    /// </summary>
    private void GivenHistory(
        Beneficiary beneficiary, IReadOnlyList<Withdrawal> history, params CollectionPoint[] pointsInvolved)
    {
        _pointRepo
            .Setup(r => r.GetBeneficiaryWithdrawalHistoryAsync(beneficiary.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var stockItems = pointsInvolved.SelectMany(p => p.StockItems).ToList();
        var categories = pointsInvolved.SelectMany(p => p.Categories).ToList();

        _pointRepo
            .Setup(r => r.GetStockItemsByIdsAsync(It.IsAny<IReadOnlyCollection<StockItemId>>(), It.IsAny<CancellationToken>()))
            .Returns<IReadOnlyCollection<StockItemId>, CancellationToken>((ids, _) =>
                Task.FromResult<IReadOnlyList<StockItem>>([.. stockItems.Where(i => ids.Contains(i.Id))]));

        _pointRepo
            .Setup(r => r.GetCategoriesByIdsAsync(It.IsAny<IReadOnlyCollection<CategoryId>>(), It.IsAny<CancellationToken>()))
            .Returns<IReadOnlyCollection<CategoryId>, CancellationToken>((ids, _) =>
                Task.FromResult<IReadOnlyList<Category>>([.. categories.Where(c => ids.Contains(c.Id))]));
    }
}
