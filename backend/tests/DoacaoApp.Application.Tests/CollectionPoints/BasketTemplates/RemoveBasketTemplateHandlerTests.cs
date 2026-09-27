using DoacaoApp.Application.CollectionPoints.Commands.BasketTemplates;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using ErrorOr;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.BasketTemplates;

public class RemoveBasketTemplateHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public RemoveBasketTemplateHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private RemoveBasketTemplateHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveRemoverESalvar()
    {
        var point = GivenPoint();
        var rice = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 5, true).Value;
        var templateId = point.CreateBasketTemplate("Cesta", [(rice, 2)]).Value;

        var result = await CreateSut().Handle(
            new RemoveBasketTemplateCommand(point.Id.Value, templateId.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        point.BasketTemplates.Should().BeEmpty();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComReceitaJaUsadaEmRetirada_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var rice = point.AddStockItem("Arroz", "kg", ItemCategory.Food, 10, 0, 5, true).Value;
        var templateId = point.CreateBasketTemplate("Cesta", [(rice, 2)]).Value;
        point.RegisterWithdrawal(
            TestData.Beneficiary(), [ItemCategory.Food], 0, TestData.Now, null, (templateId, 1));

        var result = await CreateSut().Handle(
            new RemoveBasketTemplateCommand(point.Id.Value, templateId.Value), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.BasketTemplateInUseByWithdrawal");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new RemoveBasketTemplateCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new RemoveBasketTemplateCommand(pointId, Guid.NewGuid())
            .Should().BeAssignableTo<IScopedToCollectionPoint>()
            .Which.CollectionPointId.Should().Be(pointId);
    }

    private CollectionPoint GivenPoint()
    {
        var point = TestData.Point(maxPerCycle: 5);
        _repository.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        return point;
    }
}
