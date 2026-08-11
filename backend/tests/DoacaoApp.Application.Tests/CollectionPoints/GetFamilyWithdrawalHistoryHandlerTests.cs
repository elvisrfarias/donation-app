using DoacaoApp.Application.CollectionPoints.Queries;
using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints;

public class GetFamilyWithdrawalHistoryHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
    private const string ValidCpf = "111.444.777-35";

    private readonly Mock<IFamilyRepository> _familyRepo = new();
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly FakeClock _clock = new(Now);

    private GetFamilyWithdrawalHistoryHandler CreateSut() =>
        new(_familyRepo.Object, _pointRepo.Object, _clock, _currentUser.Object);

    [Fact]
    public async Task Handle_QuandoUsuarioEAdmin_RetornaOHistoricoIndependenteDoPonto()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
        var family = BuildFamilyWithBeneficiary(CollectionPointId.New());
        GivenFamilyAndHistory(family);

        var result = await CreateSut().Handle(new GetFamilyWithdrawalHistoryQuery(family.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoUsuarioNaoEAdminEFamiliaNaoTemBeneficiarioDoSeuPonto_RetornaNotFound()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(CollectionPointId.New().Value);
        var family = BuildFamilyWithBeneficiary(CollectionPointId.New());
        GivenFamilyAndHistory(family);

        var result = await CreateSut().Handle(new GetFamilyWithdrawalHistoryQuery(family.Id.Value), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Family.NotFound");
    }

    private void GivenFamilyAndHistory(Family family)
    {
        _familyRepo.Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>())).ReturnsAsync(family);
        _pointRepo
            .Setup(r => r.CountFamilyWithdrawalsAsync(
                family.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _pointRepo
            .Setup(r => r.GetFamilyWithdrawalHistoryAsync(family.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static Family BuildFamilyWithBeneficiary(CollectionPointId registeredByPointId)
    {
        var family = Family.Create("Família Silva", WithdrawalCycle.Create(CycleType.Monthly, 1).Value, Now).Value;
        family.AddBeneficiary("João", Cpf.Create(ValidCpf).Value, null, registeredByPointId, Now);
        return family;
    }
}
