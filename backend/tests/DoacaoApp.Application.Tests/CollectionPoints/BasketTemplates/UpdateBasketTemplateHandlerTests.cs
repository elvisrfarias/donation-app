using DoacaoApp.Application.CollectionPoints.Commands.BasketTemplates;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.BasketTemplates;

public class UpdateBasketTemplateHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateBasketTemplateHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private UpdateBasketTemplateHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveAtualizarESalvar()
    {
        var point = GivenPoint();
        var categoryId = TestData.EnsureCategory(point);
        var rice = point.AddStockItem("Arroz", "kg", categoryId, 10, 0, 5, true).Value;
        var beans = point.AddStockItem("Feijão", "kg", categoryId, 10, 0, 5, true).Value;
        var templateId = point.CreateBasketTemplate("Cesta", [(rice, 2)]).Value;

        var command = new UpdateBasketTemplateCommand(
            point.Id.Value, templateId.Value, "Cesta atualizada", [new BasketTemplateItemEntry(beans.Value, 3)], IsActive: false);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var template = result.Value.BasketTemplates.Should().ContainSingle().Subject;
        template.Name.Should().Be("Cesta atualizada");
        template.IsActive.Should().BeFalse();
        template.Items.Should().ContainSingle(i => i.StockItemId == beans.Value && i.Quantity == 3);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComReceitaInexistente_DeveRetornarNotFound()
    {
        var point = GivenPoint();

        var result = await CreateSut().Handle(
            new UpdateBasketTemplateCommand(point.Id.Value, Guid.NewGuid(), "Cesta", [], true), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.BasketTemplateNotFound");
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new UpdateBasketTemplateCommand(Guid.NewGuid(), Guid.NewGuid(), "Cesta", [], true), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new UpdateBasketTemplateCommand(pointId, Guid.NewGuid(), "Cesta", [], true)
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
