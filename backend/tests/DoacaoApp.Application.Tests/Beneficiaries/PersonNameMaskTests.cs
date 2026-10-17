using DoacaoApp.Application.Beneficiaries.Contracts;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Application.Tests.Beneficiaries;

public class PersonNameMaskTests
{
    [Theory]
    [InlineData("Maria Souza", "Maria S.")]
    [InlineData("Maria Aparecida da Silva", "Maria S.")]
    [InlineData("  joão   pereira ", "joão P.")]
    [InlineData("Maria", "Maria")]
    [InlineData("", "")]
    public void Abbreviate_MantemOPrimeiroNomeEAInicialDoUltimoSobrenome(string fullName, string expected)
    {
        PersonNameMask.Abbreviate(fullName).Should().Be(expected);
    }
}
