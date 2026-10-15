using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Notices;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.Notices;

public class PointNoticeTests
{
    [Fact]
    public void Publish_Valido_ComecaAtivoComOPublicoInformado()
    {
        var result = PointNotice.Publish(
            CollectionPointId.New(), " Falta leite ", " Precisamos de leite em pó ", NoticeAudience.Public, "Maria", TestData.Now);

        result.IsError.Should().BeFalse();
        result.Value.Title.Should().Be("Falta leite");
        result.Value.Message.Should().Be("Precisamos de leite em pó");
        result.Value.Audience.Should().Be(NoticeAudience.Public);
        result.Value.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void Publish_SemTitulo_DeveFalhar()
    {
        var result = PointNotice.Publish(CollectionPointId.New(), " ", "msg", NoticeAudience.Public, "Maria", TestData.Now);

        result.FirstError.Code.Should().Be("Notice.TitleRequired");
    }

    [Fact]
    public void Publish_SemMensagem_DeveFalhar()
    {
        var result = PointNotice.Publish(CollectionPointId.New(), "Título", " ", NoticeAudience.Managers, "Maria", TestData.Now);

        result.FirstError.Code.Should().Be("Notice.MessageRequired");
    }

    [Fact]
    public void Publish_ComTituloLongo_DeveFalhar()
    {
        var result = PointNotice.Publish(
            CollectionPointId.New(), new string('t', PointNotice.MaxTitleLength + 1), "msg", NoticeAudience.Public, "Maria", TestData.Now);

        result.FirstError.Code.Should().Be("Notice.TitleTooLong");
    }

    [Fact]
    public void Publish_ComMensagemLonga_DeveFalhar()
    {
        var result = PointNotice.Publish(
            CollectionPointId.New(), "Título", new string('m', PointNotice.MaxMessageLength + 1), NoticeAudience.Public, "Maria", TestData.Now);

        result.FirstError.Code.Should().Be("Notice.MessageTooLong");
    }

    [Fact]
    public void Archive_RetiraOAvisoDoMural()
    {
        var notice = PointNotice.Publish(CollectionPointId.New(), "T", "M", NoticeAudience.Public, "Maria", TestData.Now).Value;

        notice.Archive();

        notice.IsArchived.Should().BeTrue();
    }
}
