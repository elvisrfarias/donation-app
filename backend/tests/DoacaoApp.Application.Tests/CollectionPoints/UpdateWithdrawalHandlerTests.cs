using DoacaoApp.Application.CollectionPoints.Commands;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints;

public class UpdateWithdrawalHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateWithdrawalHandlerTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private UpdateWithdrawalHandler CreateSut() => new(_pointRepo.Object, _beneficiaryRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_BeneficiarioDesvinculadoDoPonto_DeveBloquearENaoSalvar()
    {
        var (point, withdrawal, footwear) = GivenEditableClothingWithdrawal();
        GivenBeneficiaryOfWithdrawal(TestData.Beneficiary());

        var result = await CreateSut().Handle(
            new UpdateWithdrawalCommand(point.Id.Value, withdrawal.Id.Value, [new WithdrawalItemEntry(footwear.Value, 1)], null),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.BeneficiaryNotLinkedToPoint");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BeneficiarioInativo_DeveBloquearENaoSalvar()
    {
        var (point, withdrawal, footwear) = GivenEditableClothingWithdrawal();
        var inactive = TestData.Beneficiary(pointId: point.Id);
        inactive.Deactivate();
        GivenBeneficiaryOfWithdrawal(inactive);

        var result = await CreateSut().Handle(
            new UpdateWithdrawalCommand(point.Id.Value, withdrawal.Id.Value, [new WithdrawalItemEntry(footwear.Value, 1)], null),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.BeneficiaryInactive");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private (CollectionPoint Point, Withdrawal Withdrawal, StockItemId Footwear) GivenEditableClothingWithdrawal()
    {
        var point = TestData.Point();
        var clothing = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var footwear = TestData.AddStockItem(point, "Sandália", "par", 5, categoryId: TestData.AddCategory(point, "Calçados"));
        var withdrawal = point.RegisterWithdrawal(
            TestData.Beneficiary(), [TestData.Item(clothing)], 0, TestData.Now, null).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 0);
        return (point, withdrawal, footwear);
    }

    private void GivenBeneficiaryOfWithdrawal(Beneficiary beneficiary) =>
        _beneficiaryRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(beneficiary);

    [Fact]
    public async Task Handle_QuandoEAMaisRecenteDoBeneficiario_DeveAtualizarESalvar()
    {
        var point = TestData.Point();
        var clothingCategory = point.AddCategory("Roupas", "shirt").Value;
        var footwearCategory = point.AddCategory("Calçados", "footprints").Value;
        var clothing = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: clothingCategory);
        var footwear = TestData.AddStockItem(point, "Sandália", "par", 5, categoryId: footwearCategory);
        var withdrawal = point.RegisterWithdrawal(
            TestData.Beneficiary(), [TestData.Item(clothing)], 0, TestData.Now, "Original").Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 0);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [new WithdrawalItemEntry(footwear.Value, 1)], "Ajustado");

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var item = result.Value.Items.Should().ContainSingle().Subject;
        item.StockItemId.Should().Be(footwear.Value);
        item.CategoryId.Should().Be(footwearCategory.Value);
        item.CategoryName.Should().Be("Calçados");
        result.Value.Notes.Should().Be("Ajustado");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComItemInexistenteNoPonto_DeveBloquear()
    {
        var point = TestData.Point();
        var clothing = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var withdrawal = point.RegisterWithdrawal(TestData.Beneficiary(), [TestData.Item(clothing)], 0, TestData.Now, null).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 0);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [new WithdrawalItemEntry(Guid.NewGuid(), 1)], null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
    }

    [Fact]
    public async Task Handle_QuandoNaoEAMaisRecenteDoBeneficiario_DeveBloquearENaoSalvar()
    {
        var point = TestData.Point();
        var clothing = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var footwear = TestData.AddStockItem(point, "Sandália", "par", 5, categoryId: TestData.AddCategory(point, "Calçados"));
        var beneficiary = TestData.Beneficiary();
        var older = point.RegisterWithdrawal(beneficiary, [TestData.Item(clothing)], 0, TestData.Now.AddDays(-1), null).Value;
        var newer = point.RegisterWithdrawal(beneficiary, [TestData.Item(footwear)], 0, TestData.Now, null).Value;
        GivenRepositories(point, beneficiaryHistory: [newer, older], countInCycle: 0);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, older.Id.Value, [new WithdrawalItemEntry(clothing.Value, 1)], null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.OnlyMostRecentIsEditable");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RetiradaInexistenteNoPonto_DeveRetornarNotFound()
    {
        var point = TestData.Point();
        var clothing = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.EnsureCategory(point));
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, Guid.NewGuid(), [new WithdrawalItemEntry(clothing.Value, 1)], null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.NotFound");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _pointRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var command = new UpdateWithdrawalCommand(
            Guid.NewGuid(), Guid.NewGuid(), [new WithdrawalItemEntry(Guid.NewGuid(), 1)], null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public async Task Handle_ComCestaSemEstoqueSuficiente_DeveBloquear()
    {
        var point = TestData.Point();
        var categoryId = TestData.EnsureCategory(point);
        var stockItem = TestData.AddStockItem(point, quantity: 2, categoryId: categoryId);
        var clothing = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 3));
        var withdrawal = point.RegisterWithdrawal(TestData.Beneficiary(), [TestData.Item(clothing)], 0, TestData.Now, null).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 0);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [new WithdrawalItemEntry(clothing.Value, 1)], null, template.Value, 1);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.InsufficientStock");
    }

    [Fact]
    public async Task Handle_AoManterCestaNaEdicao_NaoContaARetiradaDuasVezesNoCiclo()
    {
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.EnsureCategory(point);
        var clothing = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var footwear = TestData.AddStockItem(point, "Sandália", "par", 10, categoryId: TestData.AddCategory(point, "Calçados"));
        var stockItem = TestData.AddStockItem(point, quantity: 10, categoryId: categoryId);
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        var withdrawal = point.RegisterWithdrawal(
            TestData.Beneficiary(), [TestData.Item(clothing)], 0, TestData.Now, null, (template, 1)).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 1);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value,
            [new WithdrawalItemEntry(clothing.Value, 1), new WithdrawalItemEntry(footwear.Value, 1)],
            "Adicionei calçados", template.Value, 1);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AoAdicionarCestaAUmaRetiradaQueNaoTinha_RespeitaOLimiteDasOutras()
    {
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.EnsureCategory(point);
        var stockItem = TestData.AddStockItem(point, quantity: 10, categoryId: categoryId);
        var clothing = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        var withdrawal = point.RegisterWithdrawal(TestData.Beneficiary(), [TestData.Item(clothing)], 0, TestData.Now, null).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 1);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [new WithdrawalItemEntry(clothing.Value, 1)], null, template.Value, 1);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
    }

    [Fact]
    public async Task Handle_AoTrocarDeReceita_DevolveOEstoqueDaAntigaAntesDeAplicarANova()
    {
        var point = TestData.Point(maxPerCycle: 3);
        var categoryId = TestData.EnsureCategory(point);
        var clothing = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var oldItem = TestData.AddStockItem(point, name: "Arroz", quantity: 10, categoryId: categoryId);
        var newItem = TestData.AddStockItem(point, name: "Feijão", quantity: 10, categoryId: categoryId);
        var oldTemplate = TestData.AddBasketTemplate(point, "Cesta A", (oldItem, 5));
        var newTemplate = TestData.AddBasketTemplate(point, "Cesta B", (newItem, 8));
        var withdrawal = point.RegisterWithdrawal(
            TestData.Beneficiary(), [TestData.Item(clothing)], 0, TestData.Now, null, (oldTemplate, 1)).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 1);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [new WithdrawalItemEntry(clothing.Value, 1)], null, newTemplate.Value, 1);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == oldItem).Quantity.Should().Be(10);
        point.StockItems.Single(i => i.Id == newItem).Quantity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_AoTrocarOsItensLivres_DevolveAReservaAntigaAntesDeAplicarANova()
    {
        var point = TestData.Point();
        var oldItem = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var newItem = TestData.AddStockItem(point, "Sandália", "par", 10, categoryId: TestData.AddCategory(point, "Calçados"));
        var withdrawal = point.RegisterWithdrawal(
            TestData.Beneficiary(), [TestData.Item(oldItem, 3)], 0, TestData.Now, null).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 0);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [new WithdrawalItemEntry(newItem.Value, 2)], null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == oldItem).Quantity.Should().Be(10);
        point.StockItems.Single(i => i.Id == newItem).Quantity.Should().Be(8);
    }

    [Fact]
    public void Command_EhEscopadoAoPonto_ParaQueGestorEditeSoDoProprioPonto()
    {
        var pointId = Guid.NewGuid();

        new UpdateWithdrawalCommand(pointId, Guid.NewGuid(), [new WithdrawalItemEntry(Guid.NewGuid(), 1)], null)
            .Should().BeAssignableTo<IScopedToCollectionPoint>()
            .Which.CollectionPointId.Should().Be(pointId);
    }

    private void GivenRepositories(CollectionPoint point, IReadOnlyList<Withdrawal> beneficiaryHistory, int countInCycle)
    {
        GivenBeneficiaryOfWithdrawal(TestData.Beneficiary(pointId: point.Id));
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        _pointRepo
            .Setup(r => r.GetBeneficiaryWithdrawalHistoryAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(beneficiaryHistory);
        _pointRepo
            .Setup(r => r.CountBeneficiaryBasketWithdrawalsAsync(
                It.IsAny<BeneficiaryId>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(countInCycle);
    }
}
