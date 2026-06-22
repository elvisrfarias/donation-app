using ErrorOr;

namespace DoacaoApp.Domain.ValueObjects;

/// <summary>
/// Informações de contato públicas de um ponto de coleta:
/// nome do responsável, e-mail e telefone. Composto por outros Value Objects.
/// </summary>
public sealed record ContactInfo
{
    private ContactInfo(string responsibleName, Email email, PhoneNumber phone)
    {
        ResponsibleName = responsibleName;
        Email = email;
        Phone = phone;
    }

    public string ResponsibleName { get; }
    public Email Email { get; }
    public PhoneNumber Phone { get; }

    public static ErrorOr<ContactInfo> Create(string? responsibleName, string? email, string? phone)
    {
        var errors = new List<Error>();

        var emailResult = ValueObjects.Email.Create(email);
        if (emailResult.IsError)
        {
            errors.AddRange(emailResult.Errors);
        }

        var phoneResult = PhoneNumber.Create(phone);
        if (phoneResult.IsError)
        {
            errors.AddRange(phoneResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new ContactInfo(
            (responsibleName ?? string.Empty).Trim(),
            emailResult.Value,
            phoneResult.Value);
    }

    public override string ToString() => $"{ResponsibleName} <{Email}> {Phone}";
}