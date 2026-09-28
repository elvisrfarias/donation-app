using DoacaoApp.Application.CollectionPoints.Commands.StockItems;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.StockItems;

public class UpdateStockItemSettingsHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateStockItemSettingsHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private UpdateStockItemSettingsHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveSalvarAsConfiguracoes()
    {
        var point = GivenPoint();
        var rice = point.AddStockItem("Arroz", "kg", TestData.EnsureCategory(point), 10, 0, 5, true).Value;

        var command = new UpdateStockItemSettingsCommand(
            point.Id.Value, [new StockItemSettingEntry(rice.Value, Goal: 50, AlertThreshold: 10, IsTracked: false)]);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var item = result.Value.StockItems.Should().ContainSingle().Subject;
        (item.Goal, item.AlertThreshold, item.IsTracked).Should().Be((50, 10, false));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AvisoAcimaDaMeta_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var rice = point.AddStockItem("Arroz", "kg", TestData.EnsureCategory(point), 10, 0, 5, true).Value;

        var command = new UpdateStockItemSettingsCommand(
            point.Id.Value, [new StockItemSettingEntry(rice.Value, Goal: 10, AlertThreshold: 20, IsTracked: true)]);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("StockSetting.AlertAboveGoal");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new UpdateStockItemSettingsCommand(Guid.NewGuid(), []), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new UpdateStockItemSettingsCommand(pointId, [])
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
