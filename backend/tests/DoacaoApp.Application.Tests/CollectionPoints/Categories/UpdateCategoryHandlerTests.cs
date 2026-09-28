using DoacaoApp.Application.CollectionPoints.Commands.Categories;
using DoacaoApp.Application.Common.Security;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints.Categories;

public class UpdateCategoryHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public UpdateCategoryHandlerTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private UpdateCategoryHandler CreateSut() => new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_Valido_DeveAtualizarESalvar()
    {
        var point = GivenPoint();
        var categoryId = point.AddCategory("Alimentos", "food").Value;
        var command = new UpdateCategoryCommand(point.Id.Value, categoryId.Value, "Cestas básicas", "basket");

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var category = result.Value.Categories.Should().ContainSingle().Subject;
        category.Name.Should().Be("Cestas básicas");
        category.Icon.Should().Be("basket");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComCategoriaInexistente_DeveRetornarNotFound()
    {
        var point = GivenPoint();
        var command = new UpdateCategoryCommand(point.Id.Value, Guid.NewGuid(), "Alimentos", "food");

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNotFound");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComNomeDuplicadoDeOutraCategoria_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        point.AddCategory("Alimentos", "food");
        var clothingId = point.AddCategory("Roupas", "shirt").Value;
        var command = new UpdateCategoryCommand(point.Id.Value, clothingId.Value, "alimentos", "shirt");

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.DuplicateCategoryName");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ComNomeVazio_DeveBloquearENaoSalvar()
    {
        var point = GivenPoint();
        var categoryId = point.AddCategory("Alimentos", "food").Value;
        var command = new UpdateCategoryCommand(point.Id.Value, categoryId.Value, "   ", "food");

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNameRequired");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(
            new UpdateCategoryCommand(Guid.NewGuid(), Guid.NewGuid(), "Alimentos", "food"), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public void Command_EhEscopadoAoPonto()
    {
        var pointId = Guid.NewGuid();

        new UpdateCategoryCommand(pointId, Guid.NewGuid(), "Alimentos", "food")
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
