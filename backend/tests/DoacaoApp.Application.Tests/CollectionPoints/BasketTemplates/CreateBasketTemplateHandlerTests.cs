using DoacaoApp.Application.CollectionPoints.Commands.BasketTemplates;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.BasketTemplates;

public class CreateBasketTemplateHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public CreateBasketTemplateHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private CreateBasketTemplateHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveCriarESalvar()
    {
        var point = GivenPoint();
        var rice = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 5, true).Value;

        var command = new CreateBasketTemplateCommand(
            point.Id.Value, "Cesta básica", [new BasketTemplateItemEntry(rice.Value, 2)]);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var template = result.Value.BasketTemplates.Should().ContainSingle().Subject;
        template.Name.Should().Be("Cesta básica");
        template.IsActive.Should().BeTrue();
        template.Items.Should().ContainSingle(i => i.StockItemId == rice.Value && i.Quantity == 2);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComItemInexistenteNoPonto_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();

        var command = new CreateBasketTemplateCommand(
            point.Id.Value, "Cesta básica", [new BasketTemplateItemEntry(Guid.NewGuid(), 2)]);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SemItens_DeveBloquear()
    {
        var point = GivenPoint();

        var result = await CreateSut().Handle(
            new CreateBasketTemplateCommand(point.Id.Value, "Cesta básica", []), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.BasketTemplateItemsRequired");
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new CreateBasketTemplateCommand(Guid.NewGuid(), "Cesta", []), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new CreateBasketTemplateCommand(pointId, "Cesta", [])
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
