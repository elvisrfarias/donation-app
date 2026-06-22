using DoacaoApp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DoacaoApp.Domain.Tests.ValueObjects;

public class ContactInfoTests
{
    [Fact]
    public void Create_ComDadosValidos_DeveCompor()
    {
        var result = ContactInfo.Create("Maria", "maria@x.com", "11987654321");

        result.IsError.Should().BeFalse();
        result.Value.Email.Value.Should().Be("maria@x.com");
        result.Value.Phone.Value.Should().Be("11987654321");
    }

    [Fact]
    public void Create_ComEmailETelefoneInvalidos_DeveAcumularErros()
    {
        var result = ContactInfo.Create("Maria", "invalido", "123");

        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "Email.InvalidFormat");
        result.Errors.Should().Contain(e => e.Code == "PhoneNumber.InvalidFormat");
    }
}