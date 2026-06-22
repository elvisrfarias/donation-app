using DoacaoApp.Domain.Errors;
using ErrorOr;

namespace DoacaoApp.Domain.ValueObjects;

/// <summary>
/// Telefone brasileiro (somente dígitos: DDD + número). Aceita 10 dígitos (fixo)
/// ou 11 dígitos (celular com 9 na frente).
/// </summary>
public sealed record PhoneNumber
{
    private PhoneNumber(string value) => Value = value;

    /// <summary>Somente dígitos, ex.: "11987654321".</summary>
    public string Value { get; }

    public string Formatted => Value.Length == 11
        ? $"({Value[..2]}) {Value[2..7]}-{Value[7..]}"
        : $"({Value[..2]}) {Value[2..6]}-{Value[6..]}";

    public static ErrorOr<PhoneNumber> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DomainErrors.PhoneNumber.Empty;
        }

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.Length is < 10 or > 11)
        {
            return DomainErrors.PhoneNumber.InvalidFormat;
        }

        return new PhoneNumber(digits);
    }

    internal static PhoneNumber FromTrusted(string value) => new(value);

    public override string ToString() => Formatted;
}