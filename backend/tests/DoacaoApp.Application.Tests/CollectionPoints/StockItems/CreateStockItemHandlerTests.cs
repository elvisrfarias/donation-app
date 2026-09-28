using DoacaoApp.Application.CollectionPoints.Commands.StockItems;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.StockItems;

public class CreateStockItemHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public CreateStockItemHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private CreateStockItemHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveCadastrarESalvar()
    {
        var point = GivenPoint();
        var categoryId = TestData.EnsureCategory(point);
        var command = new CreateStockItemCommand(
            point.Id.Value, "Arroz 1kg", "kg", categoryId.Value, InitialQuantity: 10, Goal: 50, AlertThreshold: 5, IsTracked: true);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.StockItems.Should().ContainSingle(i => i.Name == "Arroz 1kg" && i.Quantity == 10);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveDenormalizarNomeEIconeDaCategoria()
    {
        var point = GivenPoint();
        var categoryId = point.AddCategory("Alimentos", "food").Value;
        var command = new CreateStockItemCommand(
            point.Id.Value, "Arroz 1kg", "kg", categoryId.Value, InitialQuantity: 10, Goal: 50, AlertThreshold: 5, IsTracked: true);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        var item = result.Value.StockItems.Should().ContainSingle().Subject;
        item.CategoryId.Should().Be(categoryId.Value);
        item.CategoryName.Should().Be("Alimentos");
        item.CategoryIcon.Should().Be("food");
    }

    [Fact]
    public async Task Handle_ComNomeDuplicado_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var categoryId = TestData.EnsureCategory(point);
        point.AddStockItem("Arroz 1kg", "kg", categoryId, 0, 0, 5, true);
        var command = new CreateStockItemCommand(
            point.Id.Value, "arroz 1kg", "kg", categoryId.Value, 10, 0, 5, true);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.DuplicateStockItemName");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComCategoriaInexistenteNoPonto_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var command = new CreateStockItemCommand(
            point.Id.Value, "Arroz 1kg", "kg", Guid.NewGuid(), 10, 0, 5, true);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNotFound");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new CreateStockItemCommand(Guid.NewGuid(), "Arroz", "kg", Guid.NewGuid(), 0, 0, 0, true),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new CreateStockItemCommand(pointId, "Arroz", "kg", Guid.NewGuid(), 0, 0, 0, true)
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
