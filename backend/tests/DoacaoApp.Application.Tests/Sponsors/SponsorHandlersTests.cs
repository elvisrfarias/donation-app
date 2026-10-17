using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Sponsors.Commands;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Sponsors;
using DoacaoApp.Domain.Storage;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Sponsors;

public class SponsorHandlersTests
{
    private static readonly DateTime Now = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ISponsorRepository> _repository = new();
    private readonly Mock<IStoredFileRepository> _fileRepository = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    public SponsorHandlersTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _storage
            .Setup(s => s.GetPresignedDownloadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Uri("https://arquivos.dooa.com.br/logo.png"));
    }

    private CreateSponsorHandler CreateHandler() =>
        new(_repository.Object, _fileRepository.Object, _storage.Object, _unitOfWork.Object, _clock.Object);

    private UpdateSponsorHandler UpdateHandler() =>
        new(_repository.Object, _fileRepository.Object, _storage.Object, _unitOfWork.Object, _clock.Object);

    private static StoredFile BuildFile(string contentType = "image/png") =>
        StoredFile.Create(
            StorageProvider.MinIO, "doacao", $"logos/{Guid.NewGuid():N}", "logo.png", contentType, 1024,
            FileVisibility.Public, Now).Value;

    private void GivenFile(StoredFile file) =>
        _fileRepository.Setup(r => r.GetByIdAsync(file.Id, It.IsAny<CancellationToken>())).ReturnsAsync(file);

    [Fact]
    public async Task CreateSponsor_ComLogoValida_CadastraAtivoEVinculaALogo()
    {
        var logo = BuildFile();
        GivenFile(logo);

        var result = await CreateHandler().Handle(
            new CreateSponsorCommand("Paróquia São José", SponsorTier.Diamond, "https://paroquia.org.br", logo.Id.Value),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Tier.Should().Be("Diamond");
        result.Value.IsActive.Should().BeTrue();
        result.Value.LogoUrl.Should().Be("https://arquivos.dooa.com.br/logo.png");
        logo.OwnerType.Should().Be(nameof(Sponsor));
        _repository.Verify(r => r.Add(It.IsAny<Sponsor>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSponsor_ComArquivoQueNaoEImagem_DeveFalharSemSalvar()
    {
        var pdf = BuildFile("application/pdf");
        GivenFile(pdf);

        var result = await CreateHandler().Handle(
            new CreateSponsorCommand("Igreja", SponsorTier.Silver, null, pdf.Id.Value), CancellationToken.None);

        result.IsError.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSponsor_ComLogoInexistente_DeveFalhar()
    {
        var result = await CreateHandler().Handle(
            new CreateSponsorCommand("Igreja", SponsorTier.Silver, null, Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("StoredFile.NotFound");
    }

    [Fact]
    public async Task UpdateSponsor_TrocaCategoriaELogo()
    {
        var oldLogo = BuildFile();
        var newLogo = BuildFile();
        GivenFile(oldLogo);
        GivenFile(newLogo);
        _fileRepository
            .Setup(r => r.ListByIdsAsync(It.IsAny<IReadOnlyCollection<StoredFileId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([newLogo]);
        var sponsor = Sponsor.Create("Igreja", SponsorTier.Silver, null, oldLogo.Id, Now).Value;
        _repository.Setup(r => r.GetByIdAsync(sponsor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sponsor);

        var result = await UpdateHandler().Handle(
            new UpdateSponsorCommand(sponsor.Id.Value, "Igreja", SponsorTier.Gold, null, true, newLogo.Id.Value),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Tier.Should().Be("Gold");
        sponsor.LogoFileId.Should().Be(newLogo.Id);
        result.Value.LogoUrl.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSponsor_Inexistente_DeveRetornarNotFound()
    {
        var result = await UpdateHandler().Handle(
            new UpdateSponsorCommand(Guid.NewGuid(), "Igreja", SponsorTier.Gold, null, true, null),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("Sponsor.NotFound");
    }

    [Fact]
    public async Task DeleteSponsor_RemoveESalva()
    {
        var sponsor = Sponsor.Create("Igreja", SponsorTier.Silver, null, StoredFileId.New(), Now).Value;
        _repository.Setup(r => r.GetByIdAsync(sponsor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(sponsor);

        var result = await new DeleteSponsorHandler(_repository.Object, _unitOfWork.Object)
            .Handle(new DeleteSponsorCommand(sponsor.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _repository.Verify(r => r.Remove(sponsor), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
