using DoacaoApp.Application.Beneficiaries.Commands;
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

        var command = new RegisterBeneficiaryCommand("João", TestData.ValidCpfA, null, point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        added.Should().NotBeNull();
        added!.RegisteredByPointId.Should().Be(point.Id);
        added.IsLinkedTo(point.Id).Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CpfJaCadastradoNoMunicipio_DeveBloquear()
    {
        var point = GivenPoint();
        _beneficiaryRepo.Setup(r => r.ExistsWithCpfAsync(It.IsAny<Cpf>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var command = new RegisterBeneficiaryCommand("João", TestData.ValidCpfA, null, point.Id.Value);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Beneficiary.CpfAlreadyRegistered");
        _beneficiaryRepo.Verify(r => r.Add(It.IsAny<Beneficiary>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CpfInvalido_DeveRetornarErroDeValidacao()
    {
        var command = new RegisterBeneficiaryCommand("João", "123", null, Guid.NewGuid());

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

        var command = new RegisterBeneficiaryCommand("João", TestData.ValidCpfA, null, Guid.NewGuid());

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
