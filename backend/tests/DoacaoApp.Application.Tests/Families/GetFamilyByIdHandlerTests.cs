using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Application.Families.Queries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Families;

public class GetFamilyByIdHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
    private const string ValidCpf = "111.444.777-35";

    private readonly Mock<IFamilyRepository> _familyRepo = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    private GetFamilyByIdHandler CreateSut() => new(_familyRepo.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_QuandoUsuarioEAdmin_RetornaAFamiliaIndependenteDoPonto()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(true);
        var family = BuildFamilyWithBeneficiary(CollectionPointId.New());
        _familyRepo.Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await CreateSut().Handle(new GetFamilyByIdQuery(family.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoUsuarioNaoEAdminEFamiliaTemBeneficiarioDoSeuPonto_RetornaAFamilia()
    {
        var ownPointId = CollectionPointId.New();
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(ownPointId.Value);
        var family = BuildFamilyWithBeneficiary(ownPointId);
        _familyRepo.Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await CreateSut().Handle(new GetFamilyByIdQuery(family.Id.Value), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_QuandoUsuarioNaoEAdminEFamiliaNaoTemBeneficiarioDoSeuPonto_RetornaNotFound()
    {
        _currentUser.Setup(u => u.IsAdmin).Returns(false);
        _currentUser.Setup(u => u.CollectionPointId).Returns(CollectionPointId.New().Value);
        var family = BuildFamilyWithBeneficiary(CollectionPointId.New());
        _familyRepo.Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>())).ReturnsAsync(family);

        var result = await CreateSut().Handle(new GetFamilyByIdQuery(family.Id.Value), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Family.NotFound");
    }

    private static Family BuildFamilyWithBeneficiary(CollectionPointId registeredByPointId)
    {
        var family = Family.Create("Família Silva", WithdrawalCycle.Create(CycleType.Monthly, 1).Value, Now).Value;
        family.AddBeneficiary("João", Cpf.Create(ValidCpf).Value, null, registeredByPointId, Now);
        return family;
    }
}
