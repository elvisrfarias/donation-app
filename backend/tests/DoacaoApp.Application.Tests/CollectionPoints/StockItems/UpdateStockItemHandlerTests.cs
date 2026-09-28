using DoacaoApp.Application.CollectionPoints.Commands.StockItems;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.StockItems;

public class UpdateStockItemHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateStockItemHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private UpdateStockItemHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveAtualizarESalvar()
    {
        var point = GivenPoint();
        var categoryId = TestData.EnsureCategory(point);
        var itemId = point.AddStockItem("Arroz 1kg", "kg", categoryId, 10, 0, 5, true).Value;
        var command = new UpdateStockItemCommand(
            point.Id.Value, itemId.Value, "Arroz 5kg", "kg", categoryId.Value, Goal: 50, AlertThreshold: 10, IsTracked: false);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var item = result.Value.StockItems.Should().ContainSingle().Subject;
        (item.Name, item.Goal, item.AlertThreshold, item.IsTracked).Should().Be(("Arroz 5kg", 50, 10, false));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComOutraCategoria_DeveDenormalizarONomeEIconeDaNovaCategoria()
    {
        var point = GivenPoint();
        var foodCategory = point.AddCategory("Alimentos", "food").Value;
        var clothingCategory = point.AddCategory("Roupas", "shirt").Value;
        var itemId = point.AddStockItem("Arroz 1kg", "kg", foodCategory, 10, 0, 5, true).Value;
        var command = new UpdateStockItemCommand(
            point.Id.Value, itemId.Value, "Arroz 1kg", "kg", clothingCategory.Value, Goal: 0, AlertThreshold: 5, IsTracked: true);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        var item = result.Value.StockItems.Should().ContainSingle().Subject;
        item.CategoryId.Should().Be(clothingCategory.Value);
        item.CategoryName.Should().Be("Roupas");
        item.CategoryIcon.Should().Be("shirt");
    }

    [Fact]
    public async Task Handle_ComAvisoAcimaDaMeta_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var categoryId = TestData.EnsureCategory(point);
        var itemId = point.AddStockItem("Arroz 1kg", "kg", categoryId, 10, 0, 5, true).Value;
        var command = new UpdateStockItemCommand(
            point.Id.Value, itemId.Value, "Arroz 1kg", "kg", categoryId.Value, Goal: 10, AlertThreshold: 20, IsTracked: true);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("StockSetting.AlertAboveGoal");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComCategoriaInexistenteNoPonto_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var categoryId = TestData.EnsureCategory(point);
        var itemId = point.AddStockItem("Arroz 1kg", "kg", categoryId, 10, 0, 5, true).Value;
        var command = new UpdateStockItemCommand(
            point.Id.Value, itemId.Value, "Arroz 1kg", "kg", Guid.NewGuid(), Goal: 0, AlertThreshold: 5, IsTracked: true);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNotFound");
    }

    [Fact]
    public async Task Handle_ComItemInexistente_DeveRetornarNotFound()
    {
        var point = GivenPoint();
        var categoryId = TestData.EnsureCategory(point);
        var command = new UpdateStockItemCommand(
            point.Id.Value, Guid.NewGuid(), "Arroz", "kg", categoryId.Value, 0, 5, true);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new UpdateStockItemCommand(Guid.NewGuid(), Guid.NewGuid(), "Arroz", "kg", Guid.NewGuid(), 0, 5, true),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new UpdateStockItemCommand(pointId, Guid.NewGuid(), "Arroz", "kg", Guid.NewGuid(), 0, 5, true)
            .Should().BeAssignableTo<IScopedToCollectionPoint>()
            .Which.CollectionPointId.Should().Be(pointId);
    }

    private CollectionPoint GivenPoint()
    {
        var point = TestData.Point();
        _repository.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        return point;
    }
}
