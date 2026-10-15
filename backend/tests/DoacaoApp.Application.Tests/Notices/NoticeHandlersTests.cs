using DoacaoApp.Application.Notices.Commands;
using DoacaoApp.Application.Notices.Queries;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Notices;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Notices;

public class NoticeHandlersTests
{
    private readonly Mock<INoticeRepository> _notices = new();
    private readonly Mock<ICollectionPointRepository> _points = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeClock _clock = new(TestData.Now);

    public NoticeHandlersTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    [Fact]
    public async Task Publish_Valido_AssinaComONomeDoPontoESalva()
    {
        var point = TestData.Point();
        _points.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        var sut = new PublishNoticeHandler(_notices.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new PublishNoticeCommand(point.Id.Value, "Falta leite", "Precisamos de leite", "Public"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.AuthorName.Should().Be(point.Name);
        result.Value.Audience.Should().Be("Public");
        _notices.Verify(r => r.Add(It.Is<PointNotice>(n => n.PointId == point.Id)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_PontoInexistente_DeveRetornarNotFound()
    {
        _points.Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);
        var sut = new PublishNoticeHandler(_notices.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new PublishNoticeCommand(Guid.NewGuid(), "T", "M", "Public"), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    [Fact]
    public async Task Publish_SemTitulo_NaoDeveSalvar()
    {
        var point = TestData.Point();
        _points.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        var sut = new PublishNoticeHandler(_notices.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new PublishNoticeCommand(point.Id.Value, " ", "M", "Managers"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Notice.TitleRequired");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Validator_RejeitaPublicoDesconhecido()
    {
        var result = new PublishNoticeValidator().Validate(
            new PublishNoticeCommand(Guid.NewGuid(), "T", "M", "Qualquer"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Archive_DoAvisoDoPonto_Arquiva()
    {
        var pointId = CollectionPointId.New();
        var notice = PointNotice.Publish(pointId, "T", "M", NoticeAudience.Public, "Maria", TestData.Now).Value;
        _notices.Setup(r => r.GetByIdAsync(notice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notice);
        var sut = new ArchiveNoticeHandler(_notices.Object, _unitOfWork.Object);

        var result = await sut.Handle(new ArchiveNoticeCommand(pointId.Value, notice.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        notice.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Archive_DeAvisoDeOutroPonto_DeveRetornarNotFound()
    {
        var notice = PointNotice.Publish(CollectionPointId.New(), "T", "M", NoticeAudience.Public, "Maria", TestData.Now).Value;
        _notices.Setup(r => r.GetByIdAsync(notice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notice);
        var sut = new ArchiveNoticeHandler(_notices.Object, _unitOfWork.Object);

        var result = await sut.Handle(
            new ArchiveNoticeCommand(Guid.NewGuid(), notice.Id.Value), CancellationToken.None);

        result.FirstError.Code.Should().Be("Notice.NotFound");
        notice.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task ListNetwork_TrazONomeDoPontoDeCadaAviso()
    {
        var point = TestData.Point();
        var notice = PointNotice.Publish(point.Id, "T", "M", NoticeAudience.Managers, "Maria", TestData.Now).Value;
        _notices.Setup(r => r.ListNetworkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([notice]);
        _points.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([point]);
        var sut = new ListNetworkNoticesHandler(_notices.Object, _points.Object);

        var result = await sut.Handle(new ListNetworkNoticesQuery(), CancellationToken.None);

        result.Value.Should().ContainSingle().Which.PointName.Should().Be(point.Name);
    }
}
