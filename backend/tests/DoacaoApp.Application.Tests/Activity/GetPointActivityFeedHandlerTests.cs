using DoacaoApp.Application.Activity.Queries;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Donations;
using DoacaoApp.Domain.Notices;
using DoacaoApp.Domain.Reviews;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Activity;

public class GetPointActivityFeedHandlerTests
{
    private readonly Mock<ICollectionPointRepository> _points = new();
    private readonly Mock<IDonationRepository> _donations = new();
    private readonly Mock<INoticeRepository> _notices = new();
    private readonly Mock<IReviewRepository> _reviews = new();
    private readonly FakeClock _clock = new(TestData.Now.AddDays(1));

    private GetPointActivityFeedHandler CreateSut() =>
        new(_points.Object, _donations.Object, _notices.Object, _reviews.Object, _clock);

    private CollectionPoint GivenPoint()
    {
        var point = TestData.Point();
        _points.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        _donations.Setup(r => r.ListByPointAsync(point.Id, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _notices.Setup(r => r.ListByPointAsync(point.Id, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _reviews.Setup(r => r.ListByPointAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        return point;
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _points.Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var result = await CreateSut().Handle(new GetPointActivityFeedQuery(Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public async Task Handle_SemMovimento_RetornaListaVazia()
    {
        var point = GivenPoint();

        var result = await CreateSut().Handle(new GetPointActivityFeedQuery(point.Id.Value), CancellationToken.None);

        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MisturaFontesEOrdenaDoMaisRecenteParaOMaisAntigo()
    {
        var point = GivenPoint();
        var donation = Donation.Pledge(point.Id, "Carlos", null, "Arroz", TestData.Now.AddHours(1)).Value;
        var notice = PointNotice.Publish(point.Id, "Aviso", "Msg", NoticeAudience.Public, "Maria", TestData.Now.AddHours(3)).Value;
        var review = Review.Create(point.Id, "Ana", 5, "Ótimo", TestData.Now.AddHours(2)).Value;
        _donations.Setup(r => r.ListByPointAsync(point.Id, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([donation]);
        _notices.Setup(r => r.ListByPointAsync(point.Id, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([notice]);
        _reviews.Setup(r => r.ListByPointAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync([review]);

        var result = await CreateSut().Handle(new GetPointActivityFeedQuery(point.Id.Value), CancellationToken.None);

        result.Value.Items.Select(i => i.Kind).Should().Equal("Notice", "Review", "Donation");
    }

    [Fact]
    public async Task Handle_ItemAcompanhadoNoLimiteDoAviso_GeraAlertaDeEstoqueBaixo()
    {
        var point = GivenPoint();
        var categoryId = TestData.EnsureCategory(point);
        point.AddStockItem("Leite", "L", categoryId, initialQuantity: 2, goal: 20, alertThreshold: 5, isTracked: true);
        point.AddStockItem("Arroz", "kg", categoryId, initialQuantity: 50, goal: 20, alertThreshold: 5, isTracked: true);
        point.AddStockItem("Sal", "kg", categoryId, initialQuantity: 0, goal: 0, alertThreshold: 5, isTracked: false);

        var result = await CreateSut().Handle(new GetPointActivityFeedQuery(point.Id.Value), CancellationToken.None);

        var alert = result.Value.Items.Should().ContainSingle().Subject;
        alert.Kind.Should().Be("LowStock");
        alert.Title.Should().Contain("Leite");
        alert.OccurredAtUtc.Should().Be(_clock.UtcNow);
    }
}
