using DoacaoApp.Application.CollectionPoints.Commands;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Common;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.CollectionPoints;

public class RegisterWithdrawalHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly Mock<ICollectionPointRepository> _pointRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeClock _clock = new(TestData.Now);

    private RegisterWithdrawalHandler CreateSut() =>
        new(_pointRepo.Object, _beneficiaryRepo.Object, _unitOfWork.Object, _clock);

    [Fact]
    public async Task Handle_DentroDoLimite_DeveRegistrarESalvar()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var command = new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [ItemCategory.Food], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.BeneficiaryId.Should().Be(beneficiary.Id.Value);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_QuandoBeneficiarioAtingiuLimiteDoPontoSomandoOMunicipio_DeveBloquearENaoSalvar()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 1);

        var command = new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [ItemCategory.Food], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UsaOLimiteConfiguradoNoPontoDaRetirada()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 3);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 2);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [ItemCategory.Food], null, null), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ContaARetiradasDoBeneficiarioNoCicloDoPonto()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        await CreateSut().Handle(
            new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [ItemCategory.Food], null, null), CancellationToken.None);

        _pointRepo.Verify(
            r => r.CountBeneficiaryWithdrawalsAsync(
                beneficiary.Id,
                ItemCategory.Food,
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ComCestaJuntoDeRoupas_ContaNoLimiteEBloqueiaQuandoEsgotado()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 1);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(
                point.Id.Value, beneficiary.Id.Value, [ItemCategory.Clothing, ItemCategory.Food], null, null),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.CycleLimitReached");
    }

    [Fact]
    public async Task Handle_ItemSemEstoqueNoPonto_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        point.ReplaceStock(new Dictionary<ItemCategory, int> { [ItemCategory.Food] = 5 });
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [ItemCategory.Clothing], null, null),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.ItemUnavailable");
    }

    [Fact]
    public async Task Handle_SemNenhumItem_DeveBloquear()
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point();
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 0);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [], null, null),
            CancellationToken.None);

        result.FirstError.Code.Should().Be("Withdrawal.CategoriesRequired");
    }

    [Theory]
    [InlineData(ItemCategory.Clothing)]
    [InlineData(ItemCategory.Footwear)]
    public async Task Handle_DeCategoriaSemLimite_RegistraMesmoComOLimiteDeCestasEsgotado(ItemCategory category)
    {
        var beneficiary = TestData.Beneficiary();
        var point = TestData.Point(maxPerCycle: 1);
        SetupRepositories(beneficiary, point, withdrawalsInCycle: 5);

        var result = await CreateSut().Handle(
            new RegisterWithdrawalCommand(point.Id.Value, beneficiary.Id.Value, [category], null, null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Categories.Should().Equal(category.ToString());
        _pointRepo.Verify(
            r => r.CountBeneficiaryWithdrawalsAsync(
                It.IsAny<BeneficiaryId>(), It.IsAny<ItemCategory>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_BeneficiarioInexistente_DeveRetornarNotFound()
    {
        _beneficiaryRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<BeneficiaryId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Beneficiary?)null);

        var command = new RegisterWithdrawalCommand(Guid.NewGuid(), Guid.NewGuid(), [ItemCategory.Food], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("Beneficiary.NotFound");
    }

    [Fact]
    public async Task Handle_PontoInexistente_DeveRetornarNotFound()
    {
        var beneficiary = TestData.Beneficiary();
        _beneficiaryRepo.Setup(r => r.GetByIdAsync(beneficiary.Id, It.IsAny<CancellationToken>())).ReturnsAsync(beneficiary);
        _pointRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<CollectionPointId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CollectionPoint?)null);

        var command = new RegisterWithdrawalCommand(Guid.NewGuid(), beneficiary.Id.Value, [ItemCategory.Food], null, null);

        var result = await CreateSut().Handle(command, CancellationToken.None);

        result.FirstError.Code.Should().Be("CollectionPoint.NotFound");
    }

    private void SetupRepositories(Beneficiary beneficiary, CollectionPoint point, int withdrawalsInCycle)
    {
        _beneficiaryRepo
            .Setup(r => r.GetByIdAsync(beneficiary.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(beneficiary);

        _pointRepo
            .Setup(r => r.GetByIdAsync(point.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(point);

        _pointRepo
            .Setup(r => r.CountBeneficiaryWithdrawalsAsync(
                beneficiary.Id, It.IsAny<ItemCategory>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(withdrawalsInCycle);

        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }
}
