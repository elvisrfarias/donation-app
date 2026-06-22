using System.Text.RegularExpressions;
using DoacaoApp.Domain.Errors;
using ErrorOr;

namespace DoacaoApp.Domain.ValueObjects;

/// <summary>E-mail validado e normalizado (trim + lowercase).</summary>
public sealed partial record Email
{
    public const int MaxLength = 320;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static ErrorOr<Email> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return DomainErrors.Email.Empty;
        }

        var normalized = input.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength || !EmailRegex().IsMatch(normalized))
        {
            return DomainErrors.Email.InvalidFormat;
        }

        return new Email(normalized);
    }

    internal static Email FromTrusted(string value) => new(value);

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}