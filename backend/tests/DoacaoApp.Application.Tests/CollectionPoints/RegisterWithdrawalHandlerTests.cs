using DoacaoApp.Application.CollectionPoints.Commands;
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
    private readonly FakeClock _clock = new(TestData.Now);

    private RegisterWithdrawalHandler CreateSut() =>
        new(_pointRepo.Object, _beneficiaryRepo.Object, _unitOfWork.Object, _clock);

    [Fact]
    public async Task Handle_DentroDoLimite_DeveRegistrarESalvar()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [ItemCategory.Clothing], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.BeneficiaryId.Should().Be(beneficiary.Id.Value);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComCestaEBeneficiarioAtingiuLimiteDoPontoSomandoOMunicipio_DeveBloquearENaoSalvar()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var stockItem = TestData.AddStockItem(point, quantity: 100);
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 1);

        var command = new RegisterWithdrawalCommand(
            point.Id.Value, beneficiary.Id.Value, [ItemCategory.Food], null, null, template.Value, 1);

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
        var stockItem = TestData.AddStockItem(point, quantity: 100);
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 2);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [ItemCategory.Food], null, null, template.Value, 1),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ComCesta_ContaARetiradasComCestaDoBeneficiarioNoCicloDoPonto()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var stockItem = TestData.AddStockItem(point, quantity: 100);
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [ItemCategory.Food], null, null, template.Value, 1),
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
        var stockItem = TestData.AddStockItem(point, quantity: 10);
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 3));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [ItemCategory.Food], null, null, template.Value, 2),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == stockItem).Quantity.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ComCestaSemEstoqueSuficiente_DeveBloquearSemAlterarEstoque()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 3);
        var stockItem = TestData.AddStockItem(point, quantity: 4);
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 3));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [ItemCategory.Food], null, null, template.Value, 2),
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
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [ItemCategory.Food], null, null, Guid.NewGuid(), 1),
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

        result.FirstError.Code.Should().Be("Withdrawal.CategoriesRequired");
    }

    [Theory]
    [InlineData(ItemCategory.Clothing)]
    [InlineData(ItemCategory.Footwear)]
    [InlineData(ItemCategory.Food)]
    public async Task Handle_SemCesta_RegistraMesmoComOLimiteDeCestasEsgotadoENaoContaNoCiclo(ItemCategory category)
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 5);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [category], null, null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Categories.Should().Equal(category.ToString());
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

        var command = new RegisterWithdrawalCommand(Guid.NewGuid(), Guid.NewGuid(), [ItemCategory.Food], null, null);

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

        var command = new RegisterWithdrawalCommand(Guid.NewGuid(), beneficiary.Id.Value, [ItemCategory.Food], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    private void SetupRepositories(Beneficiary beneficiary, CollectionPoint point, int withdrawalsInCycle)
    {
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
