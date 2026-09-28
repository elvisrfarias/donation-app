using DoacaoApp.Application.CollectionPoints.Commands.Categories;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using ErrorOr;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.Categories;

public class RemoveCategoryHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public RemoveCategoryHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private RemoveCategoryHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveRemoverESalvar()
    {
        var point = GivenPoint();
        var categoryId = point.AddCategory("Alimentos", "food").Value;

        var result = await CreateSut().Handle(new RemoveCategoryCommand(point.Id.Value, categoryId.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Deleted);
        point.Categories.Should().BeEmpty();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComCategoriaUsadaPorItemDeEstoque_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var categoryId = point.AddCategory("Alimentos", "food").Value;
        point.AddStockItem("Arroz", "kg", categoryId, 10, 0, 5, true);

        var result = await CreateSut().Handle(new RemoveCategoryCommand(point.Id.Value, categoryId.Value), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.CategoryInUseByStockItem");
        point.Categories.Should().ContainSingle(c => c.Id == categoryId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComCategoriaUsadaEmRetirada_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var categoryId = point.AddCategory("Roupas", "shirt").Value;
        point.RegisterWithdrawal(TestData.Beneficiary(), [categoryId], 0, TestData.Now, null);

        var result = await CreateSut().Handle(new RemoveCategoryCommand(point.Id.Value, categoryId.Value), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.CategoryInUseByWithdrawal");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComCategoriaInexistente_DeveRetornarNotFound()
    {
        var point = GivenPoint();

        var result = await CreateSut().Handle(new RemoveCategoryCommand(point.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNotFound");
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new RemoveCategoryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new RemoveCategoryCommand(pointId, Guid.NewGuid())
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
