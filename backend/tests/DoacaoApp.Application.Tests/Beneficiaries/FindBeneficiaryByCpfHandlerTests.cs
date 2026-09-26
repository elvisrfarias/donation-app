using DoacaoApp.Application.Beneficiaries.Queries;
using DoacaoApp.Application.Tests.Common;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace DoacaoApp.Application.Tests.Beneficiaries;

public class FindBeneficiaryByCpfHandlerTests
{
    private readonly Mock<IBeneficiaryRepository> _repo = new();

    private FindBeneficiaryByCpfHandler CreateSut() => new(_repo.Object);

    [Fact]
    public async Task Handle_QuandoOCpfExiste_RetornaOBeneficiarioDeQualquerPonto()
    {
        var beneficiary = TestData.Beneficiary();
        _repo.Setup(r => r.GetByCpfAsync(beneficiary.Cpf, It.IsAny<CancellationToken>())).ReturnsAsync(beneficiary);

        var result = await CreateSut().Handle(new FindBeneficiaryByCpfQuery(TestData.ValidCpfA), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(beneficiary.Id.Value);
    }

    [Fact]
    public async Task Handle_QuandoOCpfNaoExiste_RetornaNotFound()
    {
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
}
