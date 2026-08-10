using DoacaoApp.Domain.Storage;
using DoacaoApp.Domain.Storage.Events;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.Storage;

public class StoredFileTests
{
    private static StoredFile ValidFile() => StoredFile.Create(
        StorageProvider.MinIO,
        "doacao",
        "public/images/2026/06/foto.jpg",
        "foto.jpg",
        "image/jpeg",
        sizeBytes: 1024,
        FileVisibility.Public,
        TestData.Now).Value;

    [Fact]
    public void Create_Valido_ComecaDisponivelERaiseUploaded()
    {
        var result = StoredFile.Create(
            StorageProvider.MinIO,
            "doacao",
            "public/images/2026/06/foto.jpg",
            "Foto Bonita.JPG",
            "IMAGE/JPEG",
            sizeBytes: 2048,
            FileVisibility.Public,
            TestData.Now);

        result.IsError.Should().BeFalse();
        var file = result.Value;

        file.Status.Should().Be(StoredFileStatus.Available);
        file.Extension.Should().Be(".jpg");
        file.ContentType.Should().Be("image/jpeg");
        file.IsImage.Should().BeTrue();
        file.DeletedAtUtc.Should().BeNull();
        file.DomainEvents.Should().ContainSingle(e => e is StoredFileUploaded);
    }

    [Theory]
    [InlineData("", "chave.jpg")]
    [InlineData("bucket", "")]
    public void Create_BucketOuChaveVazios_DeveFalhar(string bucket, string objectKey)
    {
        var result = StoredFile.Create(
            StorageProvider.MinIO,
            bucket,
            objectKey,
            "foto.jpg",
            "image/jpeg",
            sizeBytes: 10,
            FileVisibility.Private,
            TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("StoredFile.KeyRequired");
    }

    [Fact]
    public void Create_SemNomeOriginal_DeveFalhar()
    {
        var result = StoredFile.Create(
            StorageProvider.MinIO,
            "doacao",
            "chave.jpg",
            "  ",
            "image/jpeg",
            sizeBytes: 10,
            FileVisibility.Private,
            TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("StoredFile.NameRequired");
    }

    [Fact]
    public void Create_SemContentType_DeveFalhar()
    {
        var result = StoredFile.Create(
            StorageProvider.MinIO,
            "doacao",
            "chave.jpg",
            "foto.jpg",
            " ",
            sizeBytes: 10,
            FileVisibility.Private,
            TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("StoredFile.ContentTypeRequired");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_TamanhoInvalido_DeveFalhar(long sizeBytes)
    {
        var result = StoredFile.Create(
            StorageProvider.MinIO,
            "doacao",
            "chave.jpg",
            "foto.jpg",
            "image/jpeg",
            sizeBytes,
            FileVisibility.Private,
            TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("StoredFile.Empty");
    }

    [Fact]
    public void MarkDeleted_MarcaStatusEDataERaiseDeleted()
    {
        var file = ValidFile();
        var deletedAt = TestData.Now.AddMinutes(5);

        file.MarkDeleted(deletedAt);

        file.Status.Should().Be(StoredFileStatus.Deleted);
        file.DeletedAtUtc.Should().Be(deletedAt);
        file.UpdatedAtUtc.Should().Be(deletedAt);
        file.DomainEvents.Should().ContainSingle(e => e is StoredFileDeleted);
    }

    [Fact]
    public void MarkDeleted_ChamadoDuasVezes_EIdempotente()
    {
        var file = ValidFile();
        var firstDeletedAt = TestData.Now.AddMinutes(5);
        var secondDeletedAt = TestData.Now.AddMinutes(10);

        file.MarkDeleted(firstDeletedAt);
        file.MarkDeleted(secondDeletedAt);

        file.DeletedAtUtc.Should().Be(firstDeletedAt);
        file.DomainEvents.Should().ContainSingle(e => e is StoredFileDeleted);
    }

    [Fact]
    public void Quarantine_E_MarkAvailable_AlternamStatus()
    {
        var file = ValidFile();

        file.Quarantine(TestData.Now.AddMinutes(1));
        file.Status.Should().Be(StoredFileStatus.Quarantined);

        file.MarkAvailable(TestData.Now.AddMinutes(2));
        file.Status.Should().Be(StoredFileStatus.Available);
    }
}
