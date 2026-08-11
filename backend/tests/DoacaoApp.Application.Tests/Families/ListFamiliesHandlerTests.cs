using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Families.Queries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Families;

public class ListFamiliesHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
    private const string ValidCpf = "111.444.777-35";

    private readonly Mock<IFamilyRepository> _familyRepo = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private ListFamiliesHandler CreateSut() => new(_familyRepo.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_QuandoUsuarioEAdmin_ListaTodasAsFamilias()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
        var families = new List<Family> { BuildFamilyWithBeneficiary(CollectionPointId.New()) };
        _familyRepo.Setup(r => r.ListAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(families);

        var result = await CreateSut().Handle(new ListFamiliesQuery(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        _familyRepo.Verify(r => r.ListAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _familyRepo.Verify(
            r => r.ListByCollectionPointAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_QuandoUsuarioNaoEAdmin_ListaSoAsFamiliasDoProprioPonto()
    {
        var ownPointId = CollectionPointId.New();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId.Value);
        var families = new List<Family> { BuildFamilyWithBeneficiary(ownPointId) };
        _familyRepo
            .Setup(r => r.ListByCollectionPointAsync(ownPointId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(families);

        var result = await CreateSut().Handle(new ListFamiliesQuery(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        _familyRepo.Verify(r => r.ListAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Family BuildFamilyWithBeneficiary(CollectionPointId registeredByPointId)
    {
        var family = Family.Create("Família Silva", WithdrawalCycle.Create(CycleType.Monthly, 1).Value, Now).Value;
        family.AddBeneficiary("João", Cpf.Create(ValidCpf).Value, null, registeredByPointId, Now);
        return family;
    }
}
