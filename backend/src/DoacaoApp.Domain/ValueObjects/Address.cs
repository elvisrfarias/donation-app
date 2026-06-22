using DoacaoApp.Domain.Errors;
using ErrorOr;

namespace DoacaoApp.Domain.ValueObjects;

/// <summary>
/// Endereço de um ponto de coleta. Value Object imutável mapeado como "owned type"
/// no EF Core (colunas inline na própria tabela do ponto).
/// </summary>
public sealed record Address
{
    private Address(
        string street,
        string number,
        string? complement,
        string district,
        string city,
        string state,
        string zipCode)
    {
        Street = street;
        Number = number;
        Complement = complement;
        District = district;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public string Street { get; }
    public string Number { get; }
    public string? Complement { get; }
    public string District { get; }
    public string City { get; }

    /// <summary>UF com 2 letras maiúsculas.</summary>
    public string State { get; }

    /// <summary>CEP com 8 dígitos, sem máscara.</summary>
    public string ZipCode { get; }

    public string FormattedZipCode => $"{ZipCode[..5]}-{ZipCode[5..]}";

    public static ErrorOr<Address> Create(
        string? street,
        string? number,
        string? complement,
        string? district,
        string? city,
        string? state,
        string? zipCode)
    {
        if (string.IsNullOrWhiteSpace(street)
            || string.IsNullOrWhiteSpace(city)
            || string.IsNullOrWhiteSpace(state)
            || string.IsNullOrWhiteSpace(zipCode))
        {
            return DomainErrors.Address.Incomplete;
        }

        var normalizedState = state.Trim().ToUpperInvariant();
        if (normalizedState.Length != 2 || !normalizedState.All(char.IsLetter))
        {
            return DomainErrors.Address.InvalidState;
        }

        var zipDigits = new string(zipCode.Where(char.IsDigit).ToArray());
        if (zipDigits.Length != 8)
        {
            return DomainErrors.Address.InvalidZipCode;
        }

        return new Address(
            street.Trim(),
            (number ?? "S/N").Trim(),
            string.IsNullOrWhiteSpace(complement) ? null : complement.Trim(),
            (district ?? string.Empty).Trim(),
            city.Trim(),
            normalizedState,
            zipDigits);
    }

    public override string ToString() =>
        $"{Street}, {Number}{(Complement is null ? string.Empty : $" - {Complement}")}, {District}, {City}/{State}, {FormattedZipCode}";
}