using DoacaoApp.Application.Beneficiaries.Queries;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Beneficiaries;

public class GetBeneficiaryByIdHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private GetBeneficiaryByIdHandler CreateSut() => new(_beneficiaryRepo.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_QuandoUsuarioEAdmin_RetornaOBeneficiarioIndependenteDoPonto()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
        var beneficiary = GivenBeneficiary(CollectionPointId.New());

        var result = await CreateSut().Handle(new GetBeneficiaryByIdQuery(beneficiary.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoUsuarioNaoEAdminEBeneficiarioEDoSeuPonto_RetornaOBeneficiario()
    {
        var ownPointId = CollectionPointId.New();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId.Value);
        var beneficiary = GivenBeneficiary(ownPointId);

        var result = await CreateSut().Handle(new GetBeneficiaryByIdQuery(beneficiary.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoBeneficiarioFoiCadastradoPorOutroPontoMasEstaVinculadoAoSeu_RetornaOBeneficiario()
    {
        var ownPointId = CollectionPointId.New();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId.Value);
        var beneficiary = GivenBeneficiary(CollectionPointId.New());
        beneficiary.LinkToPoint(ownPointId, TestData.Now);

        var result = await CreateSut().Handle(new GetBeneficiaryByIdQuery(beneficiary.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoUsuarioNaoEAdminEBeneficiarioEDeOutroPonto_RetornaNotFound()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(CollectionPointId.New().Value);
        var beneficiary = GivenBeneficiary(CollectionPointId.New());

        var result = await CreateSut().Handle(new GetBeneficiaryByIdQuery(beneficiary.Id.Value), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    [Fact]
    public async Task Handle_QuandoBeneficiarioNaoExiste_RetornaNotFound()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
        _beneficiaryRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Beneficiary?)null);

        var result = await CreateSut().Handle(new GetBeneficiaryByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    private Beneficiary GivenBeneficiary(CollectionPointId registeredByPointId)
    {
        var beneficiary = TestData.Beneficiary(pointId: registeredByPointId);
        _beneficiaryRepo.Setup(r => r.GetByIdAsync(beneficiary.Id, It.IsAny<CancellationToken>())).ReturnsAsync(beneficiary);
        return beneficiary;
    }
}
