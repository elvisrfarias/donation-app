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
        var categoryId = TestData.EnsureCategory(point, "Roupas");
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [categoryId.Value], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.BeneficiaryId.Should().Be(beneficiary.Id.Value);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveDenormalizarNomeEIconeDasCategoriasNaResposta()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = point.AddCategory("Roupas", "shirt").Value;
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [categoryId.Value], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        var category = result.Value.Categories.Should().ContainSingle().Subject;
        category.CategoryId.Should().Be(categoryId.Value);
        category.CategoryName.Should().Be("Roupas");
        category.CategoryIcon.Should().Be("shirt");
    }

    [Fact]
    public async Task Handle_ComCategoriaInexistenteNoPonto_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [Guid.NewGuid()], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNotFound");
    }

    [Fact]
    public async Task Handle_ComCestaEBeneficiarioAtingiuLimiteDoPontoSomandoOMunicipio_DeveBloquearENaoSalvar()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.EnsureCategory(point);
        var stockItem = TestData.AddStockItem(point, quantity: 100, categoryId: categoryId);
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 1);

        var command = new RegisterWithdrawalCommand(
            point.Id.Value, beneficiary.Id.Value, [categoryId.Value], null, null, template.Value, 1);

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
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 2);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [categoryId.Value], null, null, template.Value, 1),
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
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [categoryId.Value], null, null, template.Value, 1),
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
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 3));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [categoryId.Value], null, null, template.Value, 2),
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
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 3));
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [categoryId.Value], null, null, template.Value, 2),
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
        var categoryId = TestData.EnsureCategory(point);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [categoryId.Value], null, null, Guid.NewGuid(), 1),
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
    [InlineData("Roupas")]
    [InlineData("Calçados")]
    [InlineData("Alimentos")]
    public async Task Handle_SemCesta_RegistraMesmoComOLimiteDeCestasEsgotadoENaoContaNoCiclo(string categoryName)
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.EnsureCategory(point, categoryName);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 5);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [categoryId.Value], null, null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Categories.Should().ContainSingle(c => c.CategoryName == categoryName);
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

        var command = new RegisterWithdrawalCommand(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], null, null);

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

        var command = new RegisterWithdrawalCommand(Guid.NewGuid(), beneficiary.Id.Value, [Guid.NewGuid()], null, null);

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
