using DoacaoApp.Application.CollectionPoints.Commands.StockItems;
using DoacaoApp.Application.Donations.Commands;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Donations;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Donations;

public class DonationHandlersTests
{
    private readonly Mock<IDonationRepository> _donations = new();
    private readonly Mock<ICollectionPointRepository> _points = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeClock _clock = new(TestData.Now);

    public DonationHandlersTests() =>
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    private CollectionPoint GivenPoint()
    {
        var point = TestData.Point();
        _points.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        return point;
    }

    private Donation GivenPledgedDonation(CollectionPointId pointId)
    {
        var donation = Donation.Pledge(pointId, "Carlos", null, "10kg de arroz", TestData.Now).Value;
        _donations.Setup(r => r.GetByIdAsync(donation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(donation);
        return donation;
    }

    [Fact]
    public async Task Pledge_Valida_DeveAdicionarESalvar()
    {
        var point = GivenPoint();
        var sut = new PledgeDonationHandler(_donations.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new PledgeDonationCommand(point.Id.Value, "Carlos", "11987654321", "10kg de arroz"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be("Pledged");
        _donations.Verify(r => r.Add(It.Is<Donation>(d => d.PointId == point.Id)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pledge_PontoInexistente_DeveRetornarNotFound()
    {
        _points.Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);
        var sut = new PledgeDonationHandler(_donations.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new PledgeDonationCommand(Guid.NewGuid(), "Carlos", null, "Roupas"), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
        _donations.Verify(r => r.Add(It.IsAny<Donation>()), Times.Never);
    }

    [Fact]
    public async Task Pledge_PontoInativo_DeveBloquear()
    {
        var point = GivenPoint();
        point.Deactivate();
        var sut = new PledgeDonationHandler(_donations.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new PledgeDonationCommand(point.Id.Value, "Carlos", null, "Roupas"), CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.Inactive");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pledge_ComDadosInvalidos_NaoDeveSalvar()
    {
        var point = GivenPoint();
        var sut = new PledgeDonationHandler(_donations.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new PledgeDonationCommand(point.Id.Value, " ", null, "Roupas"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Donation.DonorRequired");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Receive_SemItens_MarcaComoRecebidaSemMexerNoEstoque()
    {
        var point = GivenPoint();
        var rice = TestData.AddStockItem(point, "Arroz", quantity: 10);
        var donation = GivenPledgedDonation(point.Id);
        var sut = new ReceiveDonationHandler(_donations.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new ReceiveDonationCommand(point.Id.Value, donation.Id.Value, null), CancellationToken.None);

        result.IsError.Should().BeFalse();
        donation.Status.Should().Be(DonationStatus.Received);
        point.StockItems.Single(i => i.Id == rice).Quantity.Should().Be(10);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Receive_ComItens_SomaNoEstoqueDoPonto()
    {
        var point = GivenPoint();
        var rice = TestData.AddStockItem(point, "Arroz", quantity: 10);
        var donation = GivenPledgedDonation(point.Id);
        var sut = new ReceiveDonationHandler(_donations.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new ReceiveDonationCommand(point.Id.Value, donation.Id.Value, [new StockQuantityEntry(rice.Value, 10)]),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        point.StockItems.Single(i => i.Id == rice).Quantity.Should().Be(20);
    }

    [Fact]
    public async Task Receive_ComItemInvalido_NaoFechaADoacaoNemSalva()
    {
        var point = GivenPoint();
        var donation = GivenPledgedDonation(point.Id);
        var sut = new ReceiveDonationHandler(_donations.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new ReceiveDonationCommand(point.Id.Value, donation.Id.Value, [new StockQuantityEntry(Guid.NewGuid(), 3)]),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.StockItemNotFound");
        donation.Status.Should().Be(DonationStatus.Pledged);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Receive_DoacaoDeOutroPonto_DeveRetornarNotFound()
    {
        var point = GivenPoint();
        var donation = GivenPledgedDonation(CollectionPointId.New());
        var sut = new ReceiveDonationHandler(_donations.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new ReceiveDonationCommand(point.Id.Value, donation.Id.Value, null), CancellationToken.None);

        result.FirstError.Code.Should().Be("Donation.NotFound");
        donation.Status.Should().Be(DonationStatus.Pledged);
    }

    [Fact]
    public async Task Receive_JaCancelada_DeveBloquear()
    {
        var point = GivenPoint();
        var donation = GivenPledgedDonation(point.Id);
        donation.Cancel(TestData.Now);
        var sut = new ReceiveDonationHandler(_donations.Object, _points.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new ReceiveDonationCommand(point.Id.Value, donation.Id.Value, null), CancellationToken.None);

        result.FirstError.Code.Should().Be("Donation.AlreadyClosed");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_Valida_FechaComoCancelada()
    {
        var pointId = CollectionPointId.New();
        var donation = GivenPledgedDonation(pointId);
        var sut = new CancelDonationHandler(_donations.Object, _unitOfWork.Object, _clock);

        var result = await sut.Handle(
            new CancelDonationCommand(pointId.Value, donation.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
        donation.Status.Should().Be(DonationStatus.Cancelled);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Commands_GerenciaveisSaoEscopadosAoPonto()
    {
        var pointId = Guid.NewGuid();

        ((DoacaoApp.Application.Common.Security.IScopedToCollectionPoint)new ReceiveDonationCommand(pointId, Guid.NewGuid(), null))
            .CollectionPointId.Should().Be(pointId);
        ((DoacaoApp.Application.Common.Security.IScopedToCollectionPoint)new CancelDonationCommand(pointId, Guid.NewGuid()))
            .CollectionPointId.Should().Be(pointId);
    }
}
