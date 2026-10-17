using DoacaoApp.Application.Beneficiaries.Queries;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Beneficiaries;

public class FindBeneficiaryByCpfHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _repo = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private FindBeneficiaryByCpfHandler CreateSut() =>
        new(_repo.Object, _currentUser.Object, NullLogger<FindBeneficiaryByCpfHandler>.Instance);

    [Fact]
    public async Task Handle_AdminRecebeORegistroCompletoDeQualquerPonto()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
        var beneficiary = GivenBeneficiary("Maria Souza", CollectionPointId.New());

        var result = await CreateSut().Handle(new FindBeneficiaryByCpfQuery(TestData.ValidCpfA), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(beneficiary.Id.Value);
        result.Value.Name.Should().Be("Maria Souza");
        result.Value.Cpf.Should().Be(TestData.ValidCpfA);
        result.Value.Beneficiary.Should().NotBeNull();
        result.Value.Beneficiary!.Phones.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_GestorDoPontoVinculado_RecebeORegistroCompletoEVinculoComOSeuPonto()
    {
        var ownPointId = CollectionPointId.New();
        GivenManagerOf(ownPointId);
        GivenBeneficiary("Maria Souza", ownPointId);

        var result = await CreateSut().Handle(new FindBeneficiaryByCpfQuery(TestData.ValidCpfA), CancellationToken.None);

        result.Value.IsLinkedToMyPoint.Should().BeTrue();
        result.Value.Beneficiary.Should().NotBeNull();
        result.Value.Cpf.Should().Be(TestData.ValidCpfA);
    }

    [Fact]
    public async Task Handle_GestorDeOutroPonto_RecebeSoAProjecaoMascarada()
    {
        GivenManagerOf(CollectionPointId.New());
        var beneficiary = GivenBeneficiary("Maria Aparecida Souza", CollectionPointId.New());

        var result = await CreateSut().Handle(new FindBeneficiaryByCpfQuery(TestData.ValidCpfA), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(beneficiary.Id.Value);
        result.Value.Name.Should().Be("Maria S.");
        result.Value.Cpf.Should().Be("***.444.777-**");
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsLinkedToMyPoint.Should().BeFalse();
        result.Value.Beneficiary.Should().BeNull();
    }

    [Fact]
    public async Task Handle_GestorDeOutroPonto_InformaQueOBeneficiarioEstaInativo()
    {
        GivenManagerOf(CollectionPointId.New());
        var beneficiary = GivenBeneficiary("Maria Souza", CollectionPointId.New());
        beneficiary.Deactivate();

        var result = await CreateSut().Handle(new FindBeneficiaryByCpfQuery(TestData.ValidCpfA), CancellationToken.None);

        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoOCpfNaoExiste_RetornaNotFound()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
        _repo.Setup(r => r.GetByCpfAsync(It.IsAny<Cpf>(), It.IsAny<CancellationToken>())).ReturnsAsync((Beneficiary?)null);

        var result = await CreateSut().Handle(new FindBeneficiaryByCpfQuery(TestData.ValidCpfA), CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    [Fact]
    public async Task Handle_CpfInvalido_RetornaErroDeValidacao()
    {
        var result = await CreateSut().Handle(new FindBeneficiaryByCpfQuery("123"), CancellationToken.None);

        result.FirstError.Code.Should().Be("Cpf.InvalidFormat");
    }

    private void GivenManagerOf(CollectionPointId pointId)
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(pointId.Value);
    }

    private Beneficiary GivenBeneficiary(string name, CollectionPointId registeredByPointId)
    {
        var beneficiary = TestData.Beneficiary(name, TestData.ValidCpfA, registeredByPointId);
        _repo.Setup(r => r.GetByCpfAsync(beneficiary.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(beneficiary);
        return beneficiary;
    }
}
