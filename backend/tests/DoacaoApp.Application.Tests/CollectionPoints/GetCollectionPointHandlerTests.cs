using DoacaoApp.Application.CollectionPoints.Queries;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints;

public class GetCollectionPointHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ICollectionPointRepository> _repository = new();

    private GetCollectionPointHandler CreateSut() => new(_repository.Object);

    [Fact]
    public async Task Handle_QuandoOPontoExiste_RetornaOCatalogoDeEstoqueComOStatusCalculado()
    {
        var point = BuildPoint();
        point.AddStockItem("Arroz 1kg", "kg", ItemCategory.Food, initialQuantity: 0, goal: 0, alertThreshold: 5, isTracked: true);
        point.AddStockItem("Feijão 1kg", "kg", ItemCategory.Food, initialQuantity: 3, goal: 0, alertThreshold: 5, isTracked: true);
        point.AddStockItem("Óleo", "L", ItemCategory.Food, initialQuantity: 50, goal: 50, alertThreshold: 5, isTracked: true);
        point.AddStockItem("Açúcar", "kg", ItemCategory.Food, initialQuantity: 20, goal: 50, alertThreshold: 5, isTracked: true);
        _repository
            .Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(point);

        var result = await CreateSut().Handle(new GetCollectionPointQuery(point.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.StockItems.Should().HaveCount(4);
        result.Value.StockItems.Single(i => i.Name == "Arroz 1kg").Status.Should().Be("out");
        result.Value.StockItems.Single(i => i.Name == "Feijão 1kg").Status.Should().Be("low");
        result.Value.StockItems.Single(i => i.Name == "Óleo").Status.Should().Be("goal");
        result.Value.StockItems.Single(i => i.Name == "Açúcar").Status.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_DeveIncluirAsReceitasDeCestaComAFeasibilidadeCalculada()
    {
        var point = BuildPoint();
        var rice = point.AddStockItem("Arroz 1kg", "kg", ItemCategory.Food, 10, 0, 5, true).Value;
        var beans = point.AddStockItem("Feijão 1kg", "kg", ItemCategory.Food, 4, 0, 5, true).Value;
        var template = point.CreateBasketTemplate("Cesta básica", [(rice, 2), (beans, 1)]).Value;
        _repository
            .Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(point);

        var result = await CreateSut().Handle(new GetCollectionPointQuery(point.Id.Value), CancellationToken.None);

        var response = result.Value.BasketTemplates.Should().ContainSingle().Subject;
        response.Id.Should().Be(template.Value);
        response.Name.Should().Be("Cesta básica");
        response.IsActive.Should().BeTrue();
        response.Items.Should().HaveCount(2);
        response.Feasibility.CompleteBaskets.Should().Be(4);
        response.Feasibility.Missing.Should().Contain(gap => gap.ItemName == "Feijão 1kg" && gap.NeededForNextBasket == 1);
    }

    [Fact]
    public async Task Handle_QuandoOPontoNaoExiste_RetornaNotFound()
    {
        var missingId = CollectionPointId.New();
        _repository
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(new GetCollectionPointQuery(missingId.Value), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Query_EhEscopadaAoPontoConsultado_ParaQueGestorNaoLeiaOutroPonto()
    {
        var pointId = Guid.NewGuid();

        var query = new GetCollectionPointQuery(pointId);

        query.Should().BeAssignableTo<IScopedToCollectionPoint>()
            .Which.CollectionPointId.Should().Be(pointId);
    }

    private static CollectionPoint BuildPoint()
    {
        var address = Address.Create("Rua das Flores", "100", null, "Centro", "Concórdia", "SC", "89700000").Value;
        var contact = ContactInfo.Create("Maria Souza", "maria@doacao.org", "49987654321").Value;
        return CollectionPoint.Create("Casa Solidária", CollectionPointType.Association, address, contact, Now).Value;
    }
}
