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
    public async Task Handle_QuandoOPontoExiste_RetornaOPontoComOEstoque()
    {
        var point = BuildPoint();
        point.ReplaceStock([new KeyValuePair<ItemCategory, int>(ItemCategory.Food, 12)]);
        _repository
            .Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(point);

        var result = await CreateSut().Handle(new GetCollectionPointQuery(point.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(point.Id.Value);
        result.Value.Stock.Should().ContainSingle(entry => entry.Quantity == 12);
    }

    [Fact]
    public async Task Handle_DeveIncluirAsConfiguracoesDeEstoqueDoPonto()
    {
        var point = BuildPoint();
        point.ReplaceStockSettings([
            new KeyValuePair<ItemCategory, StockSetting>(ItemCategory.Food, new StockSetting(50, 10, true)),
        ]);
        _repository
            .Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(point);

        var result = await CreateSut().Handle(new GetCollectionPointQuery(point.Id.Value), CancellationToken.None);

        var setting = result.Value.StockSettings.Should().ContainSingle().Subject;
        (setting.Category, setting.Goal, setting.AlertThreshold, setting.IsTracked).Should().Be(("Food", 50, 10, true));
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
