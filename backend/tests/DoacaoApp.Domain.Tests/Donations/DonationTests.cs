using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Donations;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.Donations;

public class DonationTests
{
    private static Donation PledgedDonation() =>
        Donation.Pledge(CollectionPointId.New(), "Carlos", "11987654321", "10kg de arroz", TestData.Now).Value;

    [Fact]
    public void Pledge_Valida_ComecaComoPrometida()
    {
        var result = Donation.Pledge(CollectionPointId.New(), " Carlos ", "(11) 98765-4321", " 10kg de arroz ", TestData.Now);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(DonationStatus.Pledged);
        result.Value.DonorName.Should().Be("Carlos");
        result.Value.Description.Should().Be("10kg de arroz");
        result.Value.DonorPhone!.Value.Should().Be("11987654321");
        result.Value.ClosedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Pledge_SemTelefone_DeveAceitar()
    {
        var result = Donation.Pledge(CollectionPointId.New(), "Carlos", null, "Roupas", TestData.Now);

        result.IsError.Should().BeFalse();
        result.Value.DonorPhone.Should().BeNull();
    }

    [Fact]
    public void Pledge_ComTelefoneInvalido_DeveFalhar()
    {
        var result = Donation.Pledge(CollectionPointId.New(), "Carlos", "123", "Roupas", TestData.Now);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PhoneNumber.InvalidFormat");
    }

    [Fact]
    public void Pledge_SemDoador_DeveFalhar()
    {
        var result = Donation.Pledge(CollectionPointId.New(), "  ", null, "Roupas", TestData.Now);

        result.FirstError.Code.Should().Be("Donation.DonorRequired");
    }

    [Fact]
    public void Pledge_SemDescricao_DeveFalhar()
    {
        var result = Donation.Pledge(CollectionPointId.New(), "Carlos", null, " ", TestData.Now);

        result.FirstError.Code.Should().Be("Donation.DescriptionRequired");
    }

    [Fact]
    public void Pledge_ComDescricaoLonga_DeveFalhar()
    {
        var result = Donation.Pledge(
            CollectionPointId.New(), "Carlos", null, new string('x', Donation.MaxDescriptionLength + 1), TestData.Now);

        result.FirstError.Code.Should().Be("Donation.DescriptionTooLong");
    }

    [Fact]
    public void MarkReceived_DaPrometida_FechaComoRecebida()
    {
        var donation = PledgedDonation();

        var result = donation.MarkReceived(TestData.Now.AddHours(2));

        result.IsError.Should().BeFalse();
        donation.Status.Should().Be(DonationStatus.Received);
        donation.ClosedAtUtc.Should().Be(TestData.Now.AddHours(2));
    }

    [Fact]
    public void Cancel_DaPrometida_FechaComoCancelada()
    {
        var donation = PledgedDonation();

        var result = donation.Cancel(TestData.Now.AddHours(1));

        result.IsError.Should().BeFalse();
        donation.Status.Should().Be(DonationStatus.Cancelled);
    }

    [Fact]
    public void MarkReceived_JaFechada_DeveFalhar()
    {
        var donation = PledgedDonation();
        donation.Cancel(TestData.Now);

        var result = donation.MarkReceived(TestData.Now);

        result.FirstError.Code.Should().Be("Donation.AlreadyClosed");
        donation.Status.Should().Be(DonationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_JaRecebida_DeveFalhar()
    {
        var donation = PledgedDonation();
        donation.MarkReceived(TestData.Now);

        var result = donation.Cancel(TestData.Now);

        result.FirstError.Code.Should().Be("Donation.AlreadyClosed");
    }
}
