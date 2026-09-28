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
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateWithdrawalHandlerTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private UpdateWithdrawalHandler CreateSut() => new(_pointRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_QuandoEAMaisRecenteDoBeneficiario_DeveAtualizarESalvar()
    {
        var point = TestData.Point();
        var clothing = point.AddCategory("Roupas", "shirt").Value;
        var footwear = point.AddCategory("Calçados", "footprints").Value;
        var withdrawal = point.RegisterWithdrawal(
            TestData.Beneficiary(), [clothing], 0, TestData.Now, "Original").Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 0);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [footwear.Value], "Ajustado");

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var category = result.Value.Categories.Should().ContainSingle().Subject;
        category.CategoryId.Should().Be(footwear.Value);
        category.CategoryName.Should().Be("Calçados");
        result.Value.Notes.Should().Be("Ajustado");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComCategoriaInexistenteNoPonto_DeveBloquear()
    {
        var point = TestData.Point();
        var clothing = point.AddCategory("Roupas", "shirt").Value;
        var withdrawal = point.RegisterWithdrawal(TestData.Beneficiary(), [clothing], 0, TestData.Now, null).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 0);

        var command = new UpdateWithdrawalCommand(point.Id.Value, withdrawal.Id.Value, [Guid.NewGuid()], null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNotFound");
    }

    [Fact]
    public async Task Handle_QuandoNaoEAMaisRecenteDoBeneficiario_DeveBloquearENaoSalvar()
    {
        var point = TestData.Point();
        var clothing = point.AddCategory("Roupas", "shirt").Value;
        var footwear = point.AddCategory("Calçados", "footprints").Value;
        var beneficiary = TestData.Beneficiary();
        var older = point.RegisterWithdrawal(beneficiary, [clothing], 0, TestData.Now.AddDays(-1), null).Value;
        var newer = point.RegisterWithdrawal(beneficiary, [footwear], 0, TestData.Now, null).Value;
        GivenRepositories(point, beneficiaryHistory: [newer, older], countInCycle: 0);

        var command = new UpdateWithdrawalCommand(point.Id.Value, older.Id.Value, [clothing.Value], null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.OnlyMostRecentIsEditable");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RetiradaInexistenteNoPonto_DeveRetornarNotFound()
    {
        var point = TestData.Point();
        var categoryId = TestData.EnsureCategory(point);
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);

        var command = new UpdateWithdrawalCommand(point.Id.Value, Guid.NewGuid(), [categoryId.Value], null);

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

        var command = new UpdateWithdrawalCommand(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public async Task Handle_ComCestaSemEstoqueSuficiente_DeveBloquear()
    {
        var point = TestData.Point();
        var categoryId = TestData.EnsureCategory(point);
        var stockItem = TestData.AddStockItem(point, quantity: 2, categoryId: categoryId);
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 3));
        var withdrawal = point.RegisterWithdrawal(TestData.Beneficiary(), [categoryId], 0, TestData.Now, null).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 0);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [categoryId.Value], null, template.Value, 1);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.InsufficientStock");
    }

    [Fact]
    public async Task Handle_AoManterCestaNaEdicao_NaoContaARetiradaDuasVezesNoCiclo()
    {
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.EnsureCategory(point);
        var clothing = point.AddCategory("Roupas", "shirt").Value;
        var stockItem = TestData.AddStockItem(point, quantity: 10, categoryId: categoryId);
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        var withdrawal = point.RegisterWithdrawal(
            TestData.Beneficiary(), [categoryId], 0, TestData.Now, null, (template, 1)).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 1);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [categoryId.Value, clothing.Value], "Adicionei roupas",
            template.Value, 1);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AoAdicionarCestaAUmaRetiradaQueNaoTinha_RespeitaOLimiteDasOutras()
    {
        var point = TestData.Point(maxPerCycle: 1);
        var categoryId = TestData.EnsureCategory(point);
        var stockItem = TestData.AddStockItem(point, quantity: 10, categoryId: categoryId);
        var template = TestData.AddBasketTemplate(point, "Cesta", (stockItem, 5));
        var withdrawal = point.RegisterWithdrawal(TestData.Beneficiary(), [categoryId], 0, TestData.Now, null).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 1);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [categoryId.Value], null, template.Value, 1);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
    }

    [Fact]
    public async Task Handle_AoTrocarDeReceita_DevolveOEstoqueDaAntigaAntesDeAplicarANova()
    {
        var point = TestData.Point(maxPerCycle: 3);
        var categoryId = TestData.EnsureCategory(point);
        var oldItem = TestData.AddStockItem(point, name: "Arroz", quantity: 10, categoryId: categoryId);
        var newItem = TestData.AddStockItem(point, name: "Feijão", quantity: 10, categoryId: categoryId);
        var oldTemplate = TestData.AddBasketTemplate(point, "Cesta A", (oldItem, 5));
        var newTemplate = TestData.AddBasketTemplate(point, "Cesta B", (newItem, 8));
        var withdrawal = point.RegisterWithdrawal(
            TestData.Beneficiary(), [categoryId], 0, TestData.Now, null, (oldTemplate, 1)).Value;
        GivenRepositories(point, beneficiaryHistory: [withdrawal], countInCycle: 1);

        var command = new UpdateWithdrawalCommand(
            point.Id.Value, withdrawal.Id.Value, [categoryId.Value], null, newTemplate.Value, 1);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == oldItem).Quantity.Should().Be(10);
        point.StockItems.Single(i => i.Id == newItem).Quantity.Should().Be(2);
    }

    [Fact]
    public void Command_EhEscopadoAoPonto_ParaQueGestorEditeSoDoProprioPonto()
    {
        var pointId = Guid.NewGuid();

        new UpdateWithdrawalCommand(pointId, Guid.NewGuid(), [Guid.NewGuid()], null)
            .Should().BeAssignableTo<IScopedToCollectionPoint>()
            .Which.CollectionPointId.Should().Be(pointId);
    }

    private void GivenRepositories(CollectionPoint point, IReadOnlyList<Withdrawal> beneficiaryHistory, int countInCycle)
    {
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
