using DoacaoApp.Application.CollectionPoints.Commands.StockItems;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using ErrorOr;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.StockItems;

public class RemoveStockItemHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public RemoveStockItemHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private RemoveStockItemHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveRemoverESalvar()
    {
        var point = GivenPoint();
        var itemId = point.AddStockItem("Arroz 1kg", "kg", TestData.EnsureCategory(point), 10, 0, 5, true).Value;

        var result = await CreateSut().Handle(new RemoveStockItemCommand(point.Id.Value, itemId.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        point.StockItems.Should().BeEmpty();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComItemUsadoPorReceitaDeCesta_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var itemId = point.AddStockItem("Arroz 1kg", "kg", TestData.EnsureCategory(point), 10, 0, 5, true).Value;
        point.CreateBasketTemplate("Cesta", [(itemId, 1)]);

        var result = await CreateSut().Handle(new RemoveStockItemCommand(point.Id.Value, itemId.Value), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.StockItemInUseByBasketTemplate");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new RemoveStockItemCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new RemoveStockItemCommand(pointId, Guid.NewGuid())
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
