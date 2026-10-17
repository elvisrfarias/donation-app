using DoacaoApp.Application.CollectionPoints;
using DoacaoApp.Application.CollectionPoints.Commands;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints;

public class RegisterWithdrawalHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly FakeClock _clock = new(TestData.Now);

    private RegisterWithdrawalHandler CreateSut() =>
        new(_pointRepo.Object, _beneficiaryRepo.Object, _unitOfWork.Object, _clock, _currentUser.Object);

    [Fact]
    public async Task Handle_BeneficiarioNaoVinculadoAoPonto_DeveBloquearENaoSalvar()
    {
        var (_, point, stockItem) = GivenClothingWithdrawalScenario();
        var unlinked = TestData.Beneficiary(cpf: TestData.ValidCpfB);
        _beneficiaryRepo.Setup(r => r.GetByIdAsync(unlinked.Id, It.IsAny<CancellationToken>())).ReturnsAsync(unlinked);

        var result = await CreateSut().Handle(
            ClothingWithdrawal(point, unlinked, stockItem, occurredAtUtc: null), CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.BeneficiaryNotLinkedToPoint");
        result.FirstError.Type.Should().Be(ErrorOr.ErrorType.Conflict);
        point.Withdrawals.Should().BeEmpty();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BeneficiarioInativo_DeveBloquearENaoSalvar()
    {
        var (beneficiary, point, stockItem) = GivenClothingWithdrawalScenario();
        beneficiary.Deactivate();

        var result = await CreateSut().Handle(
            ClothingWithdrawal(point, beneficiary, stockItem, occurredAtUtc: null), CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.BeneficiaryInactive");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SemDataInformada_UsaOAgora()
    {
        var (beneficiary, point, stockItem) = GivenClothingWithdrawalScenario();

        var result = await CreateSut().Handle(
            ClothingWithdrawal(point, beneficiary, stockItem, occurredAtUtc: null), CancellationToken.None);

        result.Value.OccurredAtUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task Handle_DataNoFuturoAlemDaTolerancia_DeveRecusar()
    {
        var (beneficiary, point, stockItem) = GivenClothingWithdrawalScenario();

        var result = await CreateSut().Handle(
            ClothingWithdrawal(point, beneficiary, stockItem, TestData.Now.AddMinutes(6)), CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.OccurredInTheFuture");
        result.FirstError.Type.Should().Be(ErrorOr.ErrorType.Validation);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DataLevementeAdiantadaDentroDaTolerancia_DeveAceitar()
    {
        var (beneficiary, point, stockItem) = GivenClothingWithdrawalScenario();

        var result = await CreateSut().Handle(
            ClothingWithdrawal(point, beneficiary, stockItem, TestData.Now.AddMinutes(4)), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GestorLancandoMaisDeSeteDiasNoPassado_DeveRecusar()
    {
        var (beneficiary, point, stockItem) = GivenClothingWithdrawalScenario();

        var result = await CreateSut().Handle(
            ClothingWithdrawal(point, beneficiary, stockItem, TestData.Now.AddDays(-8)), CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.OccurredTooLongAgo");
        _pointRepo.Verify(
            r => r.CountBeneficiaryBasketWithdrawalsAsync(
                It.IsAny<BeneficiaryId>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GestorLancandoDentroDosSeteDias_DeveAceitarComADataInformada()
    {
        var (beneficiary, point, stockItem) = GivenClothingWithdrawalScenario();
        var sixDaysAgo = TestData.Now.AddDays(-WithdrawalOccurrenceWindow.ManagerMaxDaysInThePast + 1);

        var result = await CreateSut().Handle(
            ClothingWithdrawal(point, beneficiary, stockItem, sixDaysAgo), CancellationToken.None);

        result.Value.OccurredAtUtc.Should().Be(sixDaysAgo);
    }

    [Fact]
    public async Task Handle_AdminLancandoRetiradaAntiga_DeveAceitar()
    {
        var (beneficiary, point, stockItem) = GivenClothingWithdrawalScenario();
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
        var threeMonthsAgo = TestData.Now.AddMonths(-3);

        var result = await CreateSut().Handle(
            ClothingWithdrawal(point, beneficiary, stockItem, threeMonthsAgo), CancellationToken.None);

        result.Value.OccurredAtUtc.Should().Be(threeMonthsAgo);
    }

    [Fact]
    public async Task Handle_AdminComDataNoFuturo_DeveRecusar()
    {
        var (beneficiary, point, stockItem) = GivenClothingWithdrawalScenario();
        _currentUser.Setup(u => u.IsAdmin).Returns(true);

        var result = await CreateSut().Handle(
            ClothingWithdrawal(point, beneficiary, stockItem, TestData.Now.AddDays(1)), CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.OccurredInTheFuture");
    }

    private (Beneficiary Beneficiary, CollectionPoint Point, StockItemId StockItem) GivenClothingWithdrawalScenario()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var stockItem = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);
        return (beneficiary, point, stockItem);
    }

    private static RegisterWithdrawalCommand ClothingWithdrawal(
        CollectionPoint point, Beneficiary beneficiary, StockItemId stockItem, DateTime? occurredAtUtc) =>
        new(point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(stockItem.Value, 1)], occurredAtUtc, null);

    [Fact]
    public async Task Handle_DentroDoLimite_DeveRegistrarESalvar()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.EnsureCategory(point, "Roupas");
        var stockItem = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: categoryId);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(
            point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(stockItem.Value, 1)], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.BeneficiaryId.Should().Be(beneficiary.Id.Value);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveDenormalizarNomeUnidadeECategoriaDoItemNaResposta()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = point.AddCategory("Roupas", "shirt").Value;
        var stockItem = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: categoryId);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(
            point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(stockItem.Value, 2)], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        var item = result.Value.Items.Should().ContainSingle().Subject;
        item.StockItemId.Should().Be(stockItem.Value);
        item.ItemName.Should().Be("Camiseta");
        item.Unit.Should().Be("un");
        item.CategoryId.Should().Be(categoryId.Value);
        item.CategoryName.Should().Be("Roupas");
        item.CategoryIcon.Should().Be("shirt");
        item.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ComItemInexistenteNoPonto_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(
            point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(Guid.NewGuid(), 1)], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
    }

    [Fact]
    public async Task Handle_ComEstoqueInsuficienteParaOItemLivre_DeveBloquearENaoSalvar()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var stockItem = TestData.AddStockItem(point, "Camiseta", "un", 3);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(
            point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(stockItem.Value, 4)], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.InsufficientStock");
        point.StockItems.Single(i => i.Id == stockItem).Quantity.Should().Be(3);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComItensLivres_DescontaEstoqueDeCadaItemEPersiste()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var foodId = TestData.AddStockItem(point, "Arroz", "kg", 10);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 5, categoryId: TestData.AddCategory(point, "Roupas"));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(
            point.Id.Value,
            beneficiary.Id.Value,
            [new WithdrawalItemEntry(foodId.Value, 2), new WithdrawalItemEntry(clothingId.Value, 1)],
            null,
            null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().HaveCount(2);
        point.StockItems.Single(i => i.Id == foodId).Quantity.Should().Be(8);
        point.StockItems.Single(i => i.Id == clothingId).Quantity.Should().Be(4);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComCestaEBeneficiarioAtingiuLimiteDoPontoSomandoOMunicipio_DeveBloquearENaoSalvar()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.EnsureCategory(point);
        var stockItem = TestData.AddStockItem(point, quantity: 100, categoryId: categoryId);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 1);

        var command = new RegisterWithdrawalCommand(
            point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(clothingId.Value, 1)], null, null,
            template.Value, 1);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComCesta_UsaOLimiteConfiguradoNoPontoDaRetirada()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 3);
        var categoryId = TestData.EnsureCategory(point);
        var stockItem = TestData.AddStockItem(point, quantity: 100, categoryId: categoryId);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 2);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(clothingId.Value, 1)], null, null,
                template.Value, 1),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ComCesta_ContaARetiradasComCestaDoBeneficiarioNoCicloDoPonto()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.EnsureCategory(point);
        var stockItem = TestData.AddStockItem(point, quantity: 100, categoryId: categoryId);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(clothingId.Value, 1)], null, null,
                template.Value, 1),
            CancellationToken.None);

        _pointRepo.Verify(
            r => r.CountBeneficiaryBasketWithdrawalsAsync(
                beneficiary.Id,
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ComCesta_DaBaixaNoEstoqueSegundoAQuantidadeDeCestas()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 3);
        var categoryId = TestData.EnsureCategory(point);
        var stockItem = TestData.AddStockItem(point, quantity: 10, categoryId: categoryId);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 3));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(clothingId.Value, 1)], null, null,
                template.Value, 2),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == stockItem).Quantity.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ComCestaSemEstoqueSuficiente_DeveBloquearSemAlterarEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 3);
        var categoryId = TestData.EnsureCategory(point);
        var stockItem = TestData.AddStockItem(point, quantity: 4, categoryId: categoryId);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 3));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(clothingId.Value, 1)], null, null,
                template.Value, 2),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.InsufficientStock");
        point.StockItems.Single(i => i.Id == stockItem).Quantity.Should().Be(4);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComReceitaInexistenteNoPonto_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 3);
        var clothingId = TestData.AddStockItem(point, "Camiseta", "un", 10, categoryId: TestData.AddCategory(point, "Roupas"));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(clothingId.Value, 1)], null, null,
                Guid.NewGuid(), 1),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.BasketTemplateNotFound");
    }

    [Fact]
    public async Task Handle_SemNenhumItem_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [], null, null),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.ItemsRequired");
    }

    [Theory]
    [InlineData("Roupas")]
    [InlineData("Calçados")]
    [InlineData("Alimentos")]
    public async Task Handle_SemCesta_RegistraMesmoComOLimiteDeCestasEsgotadoENaoContaNoCiclo(string categoryName)
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.EnsureCategory(point, categoryName);
        var stockItem = TestData.AddStockItem(point, "Item", "un", 10, categoryId: categoryId);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 5);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [new WithdrawalItemEntry(stockItem.Value, 1)], null, null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().ContainSingle(i => i.CategoryName == categoryName);
        _pointRepo.Verify(
            r => r.CountBeneficiaryBasketWithdrawalsAsync(
                It.IsAny<BeneficiaryId>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_BeneficiarioInexistente_DeveRetornarNotFound()
    {
        _beneficiaryRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Beneficiary?)null);

        var command = new RegisterWithdrawalCommand(
            Guid.NewGuid(), Guid.NewGuid(), [new WithdrawalItemEntry(Guid.NewGuid(), 1)], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        var beneficiary = TestData.Beneficiary();
        _beneficiaryRepo.Setup(r => r.GetByIdAsync(beneficiary.Id, It.IsAny<CancellationToken>())).ReturnsAsync(beneficiary);
        _pointRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var command = new RegisterWithdrawalCommand(
            Guid.NewGuid(), beneficiary.Id.Value, [new WithdrawalItemEntry(Guid.NewGuid(), 1)], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    private void SetupRepositories(Beneficiary beneficiary, CollectionPoint point, int withdrawalsInCycle)
    {
        if (!beneficiary.IsLinkedTo(point.Id))
        {
            beneficiary.LinkToPoint(point.Id, TestData.Now);
        }

        _beneficiaryRepo
            .Setup(r => r.GetByIdAsync(beneficiary.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(beneficiary);

        _pointRepo
            .Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(point);

        _pointRepo
            .Setup(r => r.CountBeneficiaryBasketWithdrawalsAsync(
                beneficiary.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(withdrawalsInCycle);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }
}
