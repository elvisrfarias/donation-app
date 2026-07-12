using DoacaoApp.Application.Families.Commands;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Families;

public class UpdateBeneficiaryHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
    private const string ValidCpf = "111.444.777-35";
    private const string OtherValidCpf = "529.982.247-25";

    private readonly Mock<IFamilyRepository> _familyRepo = new();
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private UpdateBeneficiaryHandler CreateSut() =>
        new(_familyRepo.Object, _pointRepo.Object, _unitOfWork.Object);

    [Fact]
    public async Task Handle_DadosValidos_DeveAtualizarBeneficiarioESalvar()
    {
        var family = BuildFamily();
        var point = BuildPoint();
        var beneficiary = family.AddBeneficiary("João", Cpf.Create(ValidCpf).Value, null, point.Id, Now).Value;
        _familyRepo.Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>())).ReturnsAsync(family);
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        _familyRepo
            .Setup(r => r.ExistsBeneficiaryWithCpfAsync(It.IsAny<Cpf>(), beneficiary.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateBeneficiaryCommand(
            family.Id.Value, beneficiary.Id.Value, "João Silva", OtherValidCpf, null, point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("João Silva");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CpfJaCadastradoEmOutroBeneficiario_DeveBloquear()
    {
        var family = BuildFamily();
        var point = BuildPoint();
        var beneficiary = family.AddBeneficiary("João", Cpf.Create(ValidCpf).Value, null, point.Id, Now).Value;
        _familyRepo.Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>())).ReturnsAsync(family);
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        _familyRepo
            .Setup(r => r.ExistsBeneficiaryWithCpfAsync(It.IsAny<Cpf>(), beneficiary.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new UpdateBeneficiaryCommand(
            family.Id.Value, beneficiary.Id.Value, "João Silva", OtherValidCpf, null, point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.CpfAlreadyRegistered");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CpfInvalido_DeveRetornarErroDeValidacao()
    {
        var command = new UpdateBeneficiaryCommand(Guid.NewGuid(), Guid.NewGuid(), "João", "123", null, Guid.NewGuid());

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Cpf.InvalidFormat");
    }

    [Fact]
    public async Task Handle_FamiliaInexistente_DeveRetornarNotFound()
    {
        _familyRepo.Setup(r => r.GetByIdAsync(It.IsAny<FamilyId>(), It.IsAny<CancellationToken>())).ReturnsAsync((Family?)null);

        var command = new UpdateBeneficiaryCommand(Guid.NewGuid(), Guid.NewGuid(), "João", ValidCpf, null, Guid.NewGuid());

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Family.NotFound");
    }

    [Fact]
    public async Task Handle_BeneficiarioInexistente_DeveRetornarNotFound()
    {
        var family = BuildFamily();
        var point = BuildPoint();
        _familyRepo.Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>())).ReturnsAsync(family);
        _pointRepo.Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>())).ReturnsAsync(point);
        _familyRepo
            .Setup(r => r.ExistsBeneficiaryWithCpfAsync(It.IsAny<Cpf>(), It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new UpdateBeneficiaryCommand(
            family.Id.Value, Guid.NewGuid(), "João", ValidCpf, null, point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Family BuildFamily() =>
        Family.Create("Família Silva", WithdrawalCycle.Create(CycleType.Monthly, 1).Value, Now).Value;

    private static CollectionPoint BuildPoint()
    {
        var address = Address.Create("Rua A", "1", null, "Centro", "São Paulo", "SP", "01001000").Value;
        var contact = ContactInfo.Create("Maria", "maria@x.com", "11987654321").Value;
        return CollectionPoint.Create("Ponto", CollectionPointType.Church, address, contact, Now).Value;
    }
}
