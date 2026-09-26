using DoacaoApp.Application.Beneficiaries.Contracts;
using DoacaoApp.Domain.ValueObjects;
using ErrorOr;

namespace DoacaoApp.Application.Beneficiaries;

/// <summary>Contatos e endereço do beneficiário já convertidos nos Value Objects validados.</summary>
public sealed record BeneficiaryContact(Email Email, IReadOnlyList<PhoneNumber> Phones, Address? Address);

/// <summary>Converte o e-mail, os telefones e o endereço recebidos na API nos Value Objects do beneficiário.</summary>
public static class BeneficiaryContactParser
{
    public static ErrorOr<BeneficiaryContact> Parse(
        string? email,
        IReadOnlyList<string>? phones,
        BeneficiaryAddressInput? address)
    {
        var parsedEmail = Email.Create(email);
        if (parsedEmail.IsError)
        {
            return parsedEmail.Errors;
        }

        var parsedPhones = new List<PhoneNumber>();
        foreach (var phone in phones ?? [])
        {
            var parsedPhone = PhoneNumber.Create(phone);
            if (parsedPhone.IsError)
            {
                return parsedPhone.Errors;
            }

            parsedPhones.Add(parsedPhone.Value);
        }

        var parsedAddress = ParseAddress(address);
        if (parsedAddress.IsError)
        {
            return parsedAddress.Errors;
        }

        return new BeneficiaryContact(parsedEmail.Value, parsedPhones, parsedAddress.Value);
    }

    private static ErrorOr<Address?> ParseAddress(BeneficiaryAddressInput? input)
    {
        if (input is null || input.IsBlank)
        {
            return ErrorOrFactory.From<Address?>(null);
        }

        var address = Address.Create(
            input.Street, input.Number, input.Complement, input.District, input.City, input.State, input.ZipCode);

        return address.IsError ? address.Errors : ErrorOrFactory.From<Address?>(address.Value);
    }
}
