using DoacaoApp.Application.CollectionPoints.Commands.StockItems;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.StockItems;

public class UpdateWithdrawalRuleHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateWithdrawalRuleHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private UpdateWithdrawalRuleHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveAtualizarOCicloEOMaximoDoPonto()
    {
        var point = GivenPoint();
        var command = new UpdateWithdrawalRuleCommand(point.Id.Value, CycleType.Weekly, MaxWithdrawalsPerCycle: 2);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        point.WithdrawalRule.Type.Should().Be(CycleType.Weekly);
        point.WithdrawalRule.MaxWithdrawalsPerCycle.Should().Be(2);
        result.Value.WithdrawalRule.MaxWithdrawalsPerCycle.Should().Be(2);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComMaximoInvalido_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var command = new UpdateWithdrawalRuleCommand(point.Id.Value, CycleType.Monthly, MaxWithdrawalsPerCycle: 0);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("WithdrawalCycle.InvalidMax");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new UpdateWithdrawalRuleCommand(Guid.NewGuid(), CycleType.Monthly, 3), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new UpdateWithdrawalRuleCommand(pointId, CycleType.Monthly, 3)
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
