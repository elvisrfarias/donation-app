using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.CollectionPoints.Queries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Reviews;
using DoacaoApp.Domain.Storage;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints;

public class GetPublicCollectionPointHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ICollectionPointRepository> _repository = new();
    private readonly Mock<IReviewRepository> _reviewRepository = new();
    private readonly Mock<IStoredFileRepository> _fileRepository = new();
    private readonly Mock<IFileStorage> _storage = new();

    public GetPublicCollectionPointHandlerTests()
    {
        _reviewRepository
            .Setup(r => r.GetSummariesAsync(It.IsAny<IReadOnlyCollection<CollectionPointId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repository
            .Setup(r => r.GetBeneficiariesServedAsync(It.IsAny<IReadOnlyCollection<CollectionPointId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CollectionPointId, int>());
    }

    private GetPublicCollectionPointHandler CreateSut() =>
        new(_repository.Object, _reviewRepository.Object, _fileRepository.Object, _storage.Object);

    private static CollectionPoint BuildPoint()
    {
        var address = Address.Create("Rua das Flores", "100", null, "Centro", "Concórdia", "SC", "89700000", -27.23, -52.02).Value;
        var contact = ContactInfo.Create("Maria", "maria@doacao.org", "49998765432").Value;
        return CollectionPoint.Create("Casa Solidária", CollectionPointType.Church, address, contact, Now).Value;
    }

    [Fact]
    public async Task Handle_PontoAtivo_DevolveOsDadosComOSlugDaPagina()
    {
        var point = BuildPoint();
        _repository.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);

        var result = await CreateSut().Handle(new GetPublicCollectionPointQuery(point.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Casa Solidária");
        result.Value.Slug.Should().Be("casa-solidaria-concordia-sc");
        result.Value.StockItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PontoInexistente_DevolveNotFound()
    {
        var result = await CreateSut().Handle(new GetPublicCollectionPointQuery(Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public async Task Handle_PontoInativo_RespondeComoInexistente()
    {
        var point = BuildPoint();
        point.Deactivate();
        _repository.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);

        var result = await CreateSut().Handle(new GetPublicCollectionPointQuery(point.Id.Value), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }
}
