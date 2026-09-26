using DoacaoApp.Application.Beneficiaries.Queries;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Beneficiaries;

public class ListBeneficiariesHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private ListBeneficiariesHandler CreateSut() => new(_beneficiaryRepo.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_QuandoUsuarioEAdmin_ListaTodosOsBeneficiarios()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
        List<Beneficiary> beneficiaries = [TestData.Beneficiary()];
        _beneficiaryRepo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(beneficiaries);

        var result = await CreateSut().Handle(new ListBeneficiariesQuery(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        _beneficiaryRepo.Verify(
            r => r.ListByCollectionPointAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_QuandoUsuarioNaoEAdmin_ListaSoOsBeneficiariosDoProprioPonto()
    {
        var ownPointId = CollectionPointId.New();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId.Value);
        List<Beneficiary> beneficiaries = [TestData.Beneficiary(pointId: ownPointId)];
        _beneficiaryRepo
            .Setup(r => r.ListByCollectionPointAsync(ownPointId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(beneficiaries);

        var result = await CreateSut().Handle(new ListBeneficiariesQuery(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        _beneficiaryRepo.Verify(r => r.ListAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
