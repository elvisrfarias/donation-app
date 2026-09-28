using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointCategoryTests
{
    [Fact]
    public void NovoPonto_NaoTemCategorias()
    {
        var point = TestData.Point();

        point.Categories.Should().BeEmpty();
    }

    [Fact]
    public void AddCategory_Valido_DeveAdicionarACategoria()
    {
        var point = TestData.Point();

        var result = point.AddCategory("Alimentos", "food");

        result.IsError.Should().BeFalse();
        var category = point.Categories.Single(c => c.Id == result.Value);
        category.Name.Should().Be("Alimentos");
        category.Icon.Should().Be("food");
    }

    [Fact]
    public void AddCategory_ComNomeVazio_DeveBloquear()
    {
        var point = TestData.Point();

        var result = point.AddCategory("   ", "food");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNameRequired");
    }

    [Fact]
    public void AddCategory_ComIconeVazio_DeveBloquear()
    {
        var point = TestData.Point();

        var result = point.AddCategory("Alimentos", "   ");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.CategoryIconRequired");
    }

    [Fact]
    public void AddCategory_ComNomeDuplicadoIgnorandoMaiusculasEMinusculas_DeveBloquear()
    {
        var point = TestData.Point();
        point.AddCategory("Alimentos", "food");

        var result = point.AddCategory("ALIMENTOS", "other-icon");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.DuplicateCategoryName");
        point.Categories.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateCategory_Valido_DeveAtualizarNomeEIcone()
    {
        var point = TestData.Point();
        var categoryId = point.AddCategory("Alimentos", "food").Value;

        var result = point.UpdateCategory(categoryId, "Cestas básicas", "basket");

        result.IsError.Should().BeFalse();
        var category = point.Categories.Single(c => c.Id == categoryId);
        category.Name.Should().Be("Cestas básicas");
        category.Icon.Should().Be("basket");
    }

    [Fact]
    public void UpdateCategory_Inexistente_DeveRetornarNotFound()
    {
        var point = TestData.Point();

        var result = point.UpdateCategory(CategoryId.New(), "Alimentos", "food");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNotFound");
    }

    [Fact]
    public void UpdateCategory_ComNomeVazio_DeveBloquear()
    {
        var point = TestData.Point();
        var categoryId = point.AddCategory("Alimentos", "food").Value;

        var result = point.UpdateCategory(categoryId, "   ", "food");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNameRequired");
    }

    [Fact]
    public void UpdateCategory_ComIconeVazio_DeveBloquear()
    {
        var point = TestData.Point();
        var categoryId = point.AddCategory("Alimentos", "food").Value;

        var result = point.UpdateCategory(categoryId, "Alimentos", "   ");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.CategoryIconRequired");
    }

    [Fact]
    public void UpdateCategory_ComNomeDuplicadoDeOutraCategoria_DeveBloquear()
    {
        var point = TestData.Point();
        point.AddCategory("Alimentos", "food");
        var clothingId = point.AddCategory("Roupas", "shirt").Value;

        var result = point.UpdateCategory(clothingId, "alimentos", "shirt");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.DuplicateCategoryName");
    }

    [Fact]
    public void UpdateCategory_MantendoOMesmoNome_NaoBloqueiaPorDuplicidade()
    {
        var point = TestData.Point();
        var categoryId = point.AddCategory("Alimentos", "food").Value;

        var result = point.UpdateCategory(categoryId, "Alimentos", "basket");

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void RemoveCategory_SemUso_DeveRemover()
    {
        var point = TestData.Point();
        var categoryId = point.AddCategory("Alimentos", "food").Value;

        var result = point.RemoveCategory(categoryId);

        result.IsError.Should().BeFalse();
        point.Categories.Should().BeEmpty();
    }

    [Fact]
    public void RemoveCategory_Inexistente_DeveRetornarNotFound()
    {
        var point = TestData.Point();

        var result = point.RemoveCategory(CategoryId.New());

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.CategoryNotFound");
    }

    [Fact]
    public void RemoveCategory_ReferenciadaEmItemDeEstoque_DeveBloquear()
    {
        var point = TestData.Point();
        var categoryId = point.AddCategory("Alimentos", "food").Value;
        point.AddStockItem("Arroz", "kg", categoryId, 10, 0, 0, true);

        var result = point.RemoveCategory(categoryId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.CategoryInUseByStockItem");
        point.Categories.Should().ContainSingle(c => c.Id == categoryId);
    }

    [Fact]
    public void RemoveCategory_ReferenciadaEmRetirada_DeveBloquear()
    {
        var point = TestData.Point();
        var categoryId = point.AddCategory("Roupas", "shirt").Value;
        var beneficiary = TestData.Beneficiary();
        point.RegisterWithdrawal(beneficiary, [categoryId], beneficiaryWithdrawalsInCurrentCycle: 0, TestData.Now, null);

        var result = point.RemoveCategory(categoryId);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CollectionPoint.CategoryInUseByWithdrawal");
        point.Categories.Should().ContainSingle(c => c.Id == categoryId);
    }
}
