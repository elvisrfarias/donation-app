using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.CollectionPoints;

public class CollectionPointStockSettingsTests
{
    private static KeyValuePair<ItemCategory, StockSetting> Setting(
        ItemCategory category, int goal, int alertThreshold, bool isTracked = true) =>
        new(category, new StockSetting(goal, alertThreshold, isTracked));

    [Fact]
    public void NovoPonto_NaoTemConfiguracaoDeEstoque()
    {
        var point = TestData.Point();

        point.StockSettings.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceStockSettings_Valido_DeveGuardarMetaAvisoEMonitoramento()
    {
        var point = TestData.Point();

        var result = point.ReplaceStockSettings([
            Setting(ItemCategory.Food, goal: 50, alertThreshold: 10),
            Setting(ItemCategory.Toys, goal: 0, alertThreshold: 3, isTracked: false),
        ]);

        result.IsError.Should().BeFalse();
        point.StockSettings[ItemCategory.Food].Should().Be(new StockSetting(50, 10, true));
        point.StockSettings[ItemCategory.Toys].Should().Be(new StockSetting(0, 3, false));
    }

    [Fact]
    public void ReplaceStockSettings_DeveSubstituirAsConfiguracoesAnteriores()
    {
        var point = TestData.Point();
        point.ReplaceStockSettings([Setting(ItemCategory.Food, 50, 10)]);

        point.ReplaceStockSettings([Setting(ItemCategory.Books, 20, 5)]);

        point.StockSettings.Keys.Should().BeEquivalentTo([ItemCategory.Books]);
    }

    [Fact]
    public void ReplaceStockSettings_ComCategoriaRepetida_PrevaleceAUltima()
    {
        var point = TestData.Point();

        point.ReplaceStockSettings([
            Setting(ItemCategory.Food, 50, 10),
            Setting(ItemCategory.Food, 80, 20),
        ]);

        point.StockSettings[ItemCategory.Food].Goal.Should().Be(80);
    }

    [Fact]
    public void ReplaceStockSettings_Nulo_DeveLimparAsConfiguracoes()
    {
        var point = TestData.Point();
        point.ReplaceStockSettings([Setting(ItemCategory.Food, 50, 10)]);

        point.ReplaceStockSettings(null);

        point.StockSettings.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(10, -1)]
    public void ReplaceStockSettings_ComValorNegativo_DeveBloquearEManterAsAnteriores(int goal, int alertThreshold)
    {
        var point = TestData.Point();
        point.ReplaceStockSettings([Setting(ItemCategory.Food, 50, 10)]);

        var result = point.ReplaceStockSettings([Setting(ItemCategory.Books, goal, alertThreshold)]);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("StockSetting.NegativeValue");
        point.StockSettings.Keys.Should().BeEquivalentTo([ItemCategory.Food]);
    }

    [Fact]
    public void ReplaceStockSettings_ComAvisoAcimaDaMeta_DeveBloquear()
    {
        var point = TestData.Point();

        var result = point.ReplaceStockSettings([Setting(ItemCategory.Food, goal: 10, alertThreshold: 11)]);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("StockSetting.AlertAboveGoal");
        point.StockSettings.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceStockSettings_SemMeta_PermiteAvisoQualquer()
    {
        var point = TestData.Point();

        var result = point.ReplaceStockSettings([Setting(ItemCategory.Food, goal: 0, alertThreshold: 30)]);

        result.IsError.Should().BeFalse();
    }
}
