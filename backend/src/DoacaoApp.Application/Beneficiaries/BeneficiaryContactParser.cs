using DoacaoApp.Domain.ValueObjects;
using ErrorOr;

namespace DoacaoApp.Application.Beneficiaries;

/// <summary>Converte o e-mail e os telefones recebidos na API nos Value Objects validados do beneficiário.</summary>
public static class BeneficiaryContactParser
{
    public static ErrorOr<(Email Email, IReadOnlyList<PhoneNumber> Phones)> Parse(
        string? email,
        IReadOnlyList<string>? phones)
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

        return (parsedEmail.Value, parsedPhones);
    }
}
