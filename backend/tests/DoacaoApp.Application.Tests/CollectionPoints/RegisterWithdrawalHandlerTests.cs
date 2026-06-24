using DoacaoApp.Application.CollectionPoints.Commands;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints;

public class RegisterWithdrawalHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IFamilyRepository> _familyRepo = new();
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeClock _clock = new(Now);

    private RegisterWithdrawalHandler CreateSut() =>
        new(_pointRepo.Object, _familyRepo.Object, _unitOfWork.Object, _clock);

    [Fact]
    public async Task Handle_DentroDoLimite_DeveRegistrarESalvar()
    {
        var family = BuildFamilyWithBeneficiary(out var beneficiary, max: 1);
        var point = BuildPoint();
        SetupRepositories(family, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(point.Id.Value, family.Id.Value, beneficiary.Id.Value, null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.FamilyId.Should().Be(family.Id.Value);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoFamiliaAtingiuLimiteNoMunicipio_DeveBloquearENaoSalvar()
    {
        var family = BuildFamilyWithBeneficiary(out var beneficiary, max: 1);
        var point = BuildPoint();
        SetupRepositories(family, point, withdrawalsInCycle: 1);

        var command = new RegisterWithdrawalCommand(point.Id.Value, family.Id.Value, beneficiary.Id.Value, null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FamiliaInexistente_DeveRetornarNotFound()
    {
        _familyRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<FamilyId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Family?)null);

        var command = new RegisterWithdrawalCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Family.NotFound");
    }

    [Fact]
    public async Task Handle_BeneficiarioInexistente_DeveRetornarNotFound()
    {
        var family = BuildFamilyWithBeneficiary(out _, max: 1);
        _familyRepo
            .Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);

        var command = new RegisterWithdrawalCommand(Guid.NewGuid(), family.Id.Value, Guid.NewGuid(), null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    private void SetupRepositories(Family family, CollectionPoint point, int withdrawalsInCycle)
    {
        _familyRepo
            .Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(family);

        _pointRepo
            .Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(point);

        _pointRepo
            .Setup(r => r.CountFamilyWithdrawalsAsync(
                family.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(withdrawalsInCycle);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static Family BuildFamilyWithBeneficiary(out Beneficiary beneficiary, int max)
    {
        var family = Family.Create("Família Silva", WithdrawalCycle.Create(CycleType.Monthly, max).Value, Now).Value;
        beneficiary = family.AddBeneficiary(
            "João Silva", Cpf.Create("111.444.777-35").Value, null, CollectionPointId.New(), Now).Value;
        return family;
    }

    private static CollectionPoint BuildPoint()
    {
        var address = Address.Create("Rua A", "1", null, "Centro", "São Paulo", "SP", "01001000").Value;
        var contact = ContactInfo.Create("Maria", "maria@x.com", "11987654321").Value;
        return CollectionPoint.Create("Ponto", CollectionPointType.Church, address, contact, Now).Value;
    }
}