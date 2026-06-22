using DoacaoApp.Domain.Errors;
using ErrorOr;

namespace DoacaoApp.Domain.ValueObjects;

/// <summary>
/// CPF brasileiro como Value Object imutável. Armazena 11 dígitos sem máscara e
/// valida os dígitos verificadores reais (módulo 11) na criação.
/// </summary>
public sealed record Cpf
{
    public const int Length = 11;

    private Cpf(string value) => Value = value;

    /// <summary>CPF normalizado: somente os 11 dígitos, sem pontuação.</summary>
    public string Value { get; }

    /// <summary>Versão com máscara: 000.000.000-00.</summary>
    public string Formatted =>
        $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";

    public static ErrorOr<Cpf> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DomainErrors.Cpf.Empty;
        }

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.Length != Length)
        {
            return DomainErrors.Cpf.InvalidFormat;
        }

        // CPFs com todos os dígitos iguais passam no cálculo, mas são inválidos.
        if (digits.Distinct().Count() == 1)
        {
            return DomainErrors.Cpf.InvalidCheckDigits;
        }

        if (!HasValidCheckDigits(digits))
        {
            return DomainErrors.Cpf.InvalidCheckDigits;
        }

        return new Cpf(digits);
    }

    /// <summary>Reidratação a partir da persistência (dado já validado). Uso interno do EF.</summary>
    internal static Cpf FromTrusted(string value) => new(value);

    private static bool HasValidCheckDigits(string digits)
    {
        var firstCheck = CalculateCheckDigit(digits, 9);
        var secondCheck = CalculateCheckDigit(digits, 10);

        return digits[9] - '0' == firstCheck && digits[10] - '0' == secondCheck;
    }

    private static int CalculateCheckDigit(string digits, int count)
    {
        var sum = 0;
        var weight = count + 1;

        for (var i = 0; i < count; i++)
        {
            sum += (digits[i] - '0') * weight;
            weight--;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    public override string ToString() => Formatted;
}