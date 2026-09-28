using DoacaoApp.Application.CollectionPoints.Commands.StockItems;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.StockItems;

public class UpdateStockQuantitiesHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateStockQuantitiesHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private UpdateStockQuantitiesHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveAtualizarAsQuantidadesESalvar()
    {
        var point = GivenPoint();
        var categoryId = TestData.EnsureCategory(point);
        var rice = point.AddStockItem("Arroz", "kg", categoryId, 10, 0, 5, true).Value;
        var beans = point.AddStockItem("Feijão", "kg", categoryId, 5, 0, 5, true).Value;

        var command = new UpdateStockQuantitiesCommand(
            point.Id.Value, [new StockQuantityEntry(rice.Value, 20), new StockQuantityEntry(beans.Value, 8)]);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == rice).Quantity.Should().Be(20);
        point.StockItems.Single(i => i.Id == beans).Quantity.Should().Be(8);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComQuantidadeNegativa_DeveBloquearSemAlterarNadaENaoSalvar()
    {
        var point = GivenPoint();
        var rice = point.AddStockItem("Arroz", "kg", TestData.EnsureCategory(point), 10, 0, 5, true).Value;

        var command = new UpdateStockQuantitiesCommand(point.Id.Value, [new StockQuantityEntry(rice.Value, -1)]);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NegativeQuantity");
        point.StockItems.Single(i => i.Id == rice).Quantity.Should().Be(10);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new UpdateStockQuantitiesCommand(Guid.NewGuid(), []), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new UpdateStockQuantitiesCommand(pointId, [])
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
