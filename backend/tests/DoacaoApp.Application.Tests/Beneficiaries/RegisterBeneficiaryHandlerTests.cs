using DoacaoApp.Application.Beneficiaries.Commands;
using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Beneficiaries;

public class RegisterBeneficiaryHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeClock _clock = new(TestData.Now);

    private RegisterBeneficiaryHandler CreateSut() =>
        new(_beneficiaryRepo.Object, _pointRepo.Object, _unitOfWork.Object, _clock);

    [Fact]
    public async Task Handle_DadosValidos_DeveCadastrarESalvar()
    {
        var point = GivenPoint();
        Beneficiary? added = null;
        _beneficiaryRepo.Setup(r => r.Add(It.IsAny<Beneficiary>())).Callback<Beneficiary>(b => added = b);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new RegisterBeneficiaryCommand("João", TestData.ValidCpfA, "joao@example.com", ["11987654321"], null, new DateOnly(1990, 5, 20), point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        added.Should().NotBeNull();
        added!.RegisteredByPointId.Should().Be(point.Id);
        added.IsLinkedTo(point.Id).Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ComEmailETelefones_GuardaOsContatosNormalizados()
    {
        var point = GivenPoint();
        Beneficiary? added = null;
        _beneficiaryRepo.Setup(r => r.Add(It.IsAny<Beneficiary>())).Callback<Beneficiary>(b => added = b);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new RegisterBeneficiaryCommand(
            "João", TestData.ValidCpfA, " Joao@Example.com ", ["(11) 98765-4321", "1134567890"], null, new DateOnly(1990, 5, 20), point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        added!.Email!.Value.Should().Be("joao@example.com");
        added.Phones.Select(p => p.Value).Should().Equal("11987654321", "1134567890");
        result.Value.Email.Should().Be("joao@example.com");
        result.Value.Phones.Should().Equal("(11) 98765-4321", "(11) 3456-7890");
    }

    [Theory]
    [InlineData("", "Email.Empty")]
    [InlineData("nao-e-email", "Email.InvalidFormat")]
    public async Task Handle_EmailAusenteOuInvalido_DeveBloquear(string email, string expectedCode)
    {
        var point = GivenPoint();

        var command = new RegisterBeneficiaryCommand("João", TestData.ValidCpfA, email, ["11987654321"], null, new DateOnly(1990, 5, 20), point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be(expectedCode);
        _beneficiaryRepo.Verify(r => r.Add(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TelefoneInvalido_DeveBloquear()
    {
        var point = GivenPoint();

        var command = new RegisterBeneficiaryCommand(
            "João", TestData.ValidCpfA, "joao@example.com", ["123"], null, new DateOnly(1990, 5, 20), point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("PhoneNumber.InvalidFormat");
    }

    [Fact]
    public async Task Handle_ComEnderecoCompleto_GuardaOEnderecoNormalizado()
    {
        var point = GivenPoint();
        Beneficiary? added = null;
        _beneficiaryRepo.Setup(r => r.Add(It.IsAny<Beneficiary>())).Callback<Beneficiary>(b => added = b);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var address = new BeneficiaryAddressInput("Rua das Flores", "100", "Apto 2", "Centro", "Concórdia", "sc", "89700-000");

        var command = new RegisterBeneficiaryCommand(
            "João", TestData.ValidCpfA, "joao@example.com", ["11987654321"], address, new DateOnly(1990, 5, 20), point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        added!.Address!.State.Should().Be("SC");
        added.Address.ZipCode.Should().Be("89700000");
        result.Value.Address!.ZipCode.Should().Be("89700-000");
        result.Value.Address.Street.Should().Be("Rua das Flores");
    }

    [Fact]
    public async Task Handle_EnderecoTotalmenteEmBranco_TratadoComoSemEndereco()
    {
        var point = GivenPoint();
        Beneficiary? added = null;
        _beneficiaryRepo.Setup(r => r.Add(It.IsAny<Beneficiary>())).Callback<Beneficiary>(b => added = b);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var blank = new BeneficiaryAddressInput("", "", "", " ", "", "", "");

        var command = new RegisterBeneficiaryCommand(
            "João", TestData.ValidCpfA, "joao@example.com", ["11987654321"], blank, new DateOnly(1990, 5, 20), point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        added!.Address.Should().BeNull();
        result.Value.Address.Should().BeNull();
    }

    [Theory]
    [InlineData("Rua A", "1", "Centro", "", "SC", "89700000", "Address.Incomplete")]
    [InlineData("Rua A", "1", "Centro", "Concórdia", "SC", "123", "Address.InvalidZipCode")]
    [InlineData("Rua A", "1", "Centro", "Concórdia", "Santa Catarina", "89700000", "Address.InvalidState")]
    public async Task Handle_EnderecoPreenchidoPelaMetade_DeveBloquear(
        string street, string number, string district, string city, string state, string zipCode, string expectedCode)
    {
        var point = GivenPoint();
        var address = new BeneficiaryAddressInput(street, number, null, district, city, state, zipCode);

        var command = new RegisterBeneficiaryCommand(
            "João", TestData.ValidCpfA, "joao@example.com", ["11987654321"], address, new DateOnly(1990, 5, 20), point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be(expectedCode);
        _beneficiaryRepo.Verify(r => r.Add(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SemDataDeNascimento_DeveBloquear()
    {
        var point = GivenPoint();

        var command = new RegisterBeneficiaryCommand(
            "João", TestData.ValidCpfA, "joao@example.com", ["11987654321"], null, null, point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.BirthDateRequired");
        _beneficiaryRepo.Verify(r => r.Add(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SemTelefone_DeveBloquearPoisOContatoPrincipalEObrigatorio()
    {
        var point = GivenPoint();

        var command = new RegisterBeneficiaryCommand(
            "João", TestData.ValidCpfA, "joao@example.com", [], null, new DateOnly(1990, 5, 20), point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.PhoneRequired");
        _beneficiaryRepo.Verify(r => r.Add(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MaisDeDoisTelefones_DeveBloquear()
    {
        var point = GivenPoint();

        var command = new RegisterBeneficiaryCommand(
            "João",
            TestData.ValidCpfA,
            "joao@example.com",
            ["11987654321", "1134567890", "21987654321"],
            null,
            new DateOnly(1990, 5, 20),
            point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.TooManyPhones");
    }

    [Fact]
    public async Task Handle_CpfJaCadastradoNoMunicipio_DeveBloquear()
    {
        var point = GivenPoint();
        _beneficiaryRepo.Setup(r => r.ExistsWithCpfAsync(It.IsAny<Cpf>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new RegisterBeneficiaryCommand("João", TestData.ValidCpfA, "joao@example.com", ["11987654321"], null, new DateOnly(1990, 5, 20), point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.CpfAlreadyRegistered");
        _beneficiaryRepo.Verify(r => r.Add(It.IsAny<Beneficiary>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CpfInvalido_DeveRetornarErroDeValidacao()
    {
        var command = new RegisterBeneficiaryCommand("João", "123", "joao@example.com", ["11987654321"], null, new DateOnly(1990, 5, 20), Guid.NewGuid());

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Cpf.InvalidFormat");
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        _pointRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var command = new RegisterBeneficiaryCommand("João", TestData.ValidCpfA, "joao@example.com", ["11987654321"], null, new DateOnly(1990, 5, 20), Guid.NewGuid());

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    private CollectionPoint GivenPoint()
    {
        var point = TestData.Point();
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        return point;
    }
}
