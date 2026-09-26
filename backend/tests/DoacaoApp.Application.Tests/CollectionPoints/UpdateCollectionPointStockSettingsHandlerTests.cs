using DoacaoApp.Application.CollectionPoints.Commands;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints;

public class UpdateCollectionPointStockSettingsHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateCollectionPointStockSettingsHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private UpdateCollectionPointStockSettingsHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveSalvarEDevolverAsConfiguracoes()
    {
        var point = GivenPoint();
        var command = new UpdateCollectionPointStockSettingsCommand(point.Id.Value, [
            new StockSettingEntry(ItemCategory.Food, Goal: 50, AlertThreshold: 10, IsTracked: true),
            new StockSettingEntry(ItemCategory.Toys, Goal: 0, AlertThreshold: 5, IsTracked: false),
        ]);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.StockSettings.Should().HaveCount(2);
        var food = result.Value.StockSettings.Single(s => s.Category == "Food");
        (food.Goal, food.AlertThreshold, food.IsTracked).Should().Be((50, 10, true));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComRegraDeRetirada_DeveAtualizarOCicloEOMaximoDoPonto()
    {
        var point = GivenPoint();
        var command = new UpdateCollectionPointStockSettingsCommand(
            point.Id.Value, [], CycleType.Weekly, MaxWithdrawalsPerCycle: 2);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        point.WithdrawalRule.Type.Should().Be(CycleType.Weekly);
        point.WithdrawalRule.MaxWithdrawalsPerCycle.Should().Be(2);
        result.Value.WithdrawalRule.MaxWithdrawalsPerCycle.Should().Be(2);
    }

    [Fact]
    public async Task Handle_SemRegraDeRetirada_DeveManterARegraAtualDoPonto()
    {
        var point = GivenPoint();
        point.UpdateWithdrawalRule(WithdrawalCycle.Create(CycleType.Biweekly, 4).Value);
        var command = new UpdateCollectionPointStockSettingsCommand(point.Id.Value, []);

        await CreateSut().Handle(command, CancellationToken.None);

        point.WithdrawalRule.Type.Should().Be(CycleType.Biweekly);
        point.WithdrawalRule.MaxWithdrawalsPerCycle.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ComMaximoInvalido_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var command = new UpdateCollectionPointStockSettingsCommand(
            point.Id.Value, [], CycleType.Monthly, MaxWithdrawalsPerCycle: 0);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("WithdrawalCycle.InvalidMax");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AvisoAcimaDaMeta_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var command = new UpdateCollectionPointStockSettingsCommand(point.Id.Value, [
            new StockSettingEntry(ItemCategory.Food, Goal: 10, AlertThreshold: 20, IsTracked: true),
        ]);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
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
            new UpdateCollectionPointStockSettingsCommand(Guid.NewGuid(), []), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto_ParaQueGestorNaoConfigureOutroPonto()
    {
        var pointId = Guid.NewGuid();

        var command = new UpdateCollectionPointStockSettingsCommand(pointId, []);

        command.Should().BeAssignableTo<IScopedToCollectionPoint>()
            .Which.CollectionPointId.Should().Be(pointId);
    }

    private CollectionPoint GivenPoint()
    {
        var point = TestData.Point();
        _repository.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        return point;
    }
}
