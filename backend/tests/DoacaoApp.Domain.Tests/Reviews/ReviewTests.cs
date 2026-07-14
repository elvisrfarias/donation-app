using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Reviews;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.Reviews;

public class ReviewTests
{
    private static Review ValidReview() =>
        Review.Create(CollectionPointId.New(), "Maria", 5, "Ótimo atendimento", TestData.Now).Value;

    [Fact]
    public void Create_Valido_ComecaVisivelESemDenuncias()
    {
        var result = Review.Create(CollectionPointId.New(), "Ana", 4, "Bom", TestData.Now);

        result.IsError.Should().BeFalse();
        result.Value.IsHidden.Should().BeFalse();
        result.Value.ReportCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_NotaForaDoIntervalo_DeveFalhar(int rating)
    {
        var result = Review.Create(CollectionPointId.New(), "Ana", rating, null, TestData.Now);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void Create_SemAutor_DeveFalhar()
    {
        var result = Review.Create(CollectionPointId.New(), "  ", 5, null, TestData.Now);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void Hide_E_Restore_AlternamVisibilidade()
    {
        var review = ValidReview();

        review.Hide();
        review.IsHidden.Should().BeTrue();

        review.Restore();
        review.IsHidden.Should().BeFalse();
    }

    [Fact]
    public void Report_IncrementaContador()
    {
        var review = ValidReview();

        review.Report();
        review.Report();

        review.ReportCount.Should().Be(2);
    }
}
