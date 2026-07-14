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
        string zipCode,
        double? latitude,
        double? longitude)
    {
        Street = street;
        Number = number;
        Complement = complement;
        District = district;
        City = city;
        State = state;
        ZipCode = zipCode;
        Latitude = latitude;
        Longitude = longitude;
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

    /// <summary>Latitude do ponto exato no mapa; nula quando ainda não geolocalizado.</summary>
    public double? Latitude { get; }

    /// <summary>Longitude do ponto exato no mapa; nula quando ainda não geolocalizado.</summary>
    public double? Longitude { get; }

    /// <summary>Indica se o endereço tem coordenadas fixadas no mapa.</summary>
    public bool HasCoordinates => Latitude is not null && Longitude is not null;

    public string FormattedZipCode => $"{ZipCode[..5]}-{ZipCode[5..]}";

    public static ErrorOr<Address> Create(
        string? street,
        string? number,
        string? complement,
        string? district,
        string? city,
        string? state,
        string? zipCode,
        double? latitude = null,
        double? longitude = null)
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

        var (normalizedLatitude, normalizedLongitude) = NormalizeCoordinates(latitude, longitude);
        if (!AreCoordinatesValid(normalizedLatitude, normalizedLongitude))
        {
            return DomainErrors.Address.InvalidCoordinates;
        }

        return new Address(
            street.Trim(),
            (number ?? "S/N").Trim(),
            string.IsNullOrWhiteSpace(complement) ? null : complement.Trim(),
            (district ?? string.Empty).Trim(),
            city.Trim(),
            normalizedState,
            zipDigits,
            normalizedLatitude,
            normalizedLongitude);
    }

    private static (double? Latitude, double? Longitude) NormalizeCoordinates(double? latitude, double? longitude) =>
        latitude is null || longitude is null ? (null, null) : (latitude, longitude);

    private static bool AreCoordinatesValid(double? latitude, double? longitude) =>
        latitude is null || longitude is null
        || (latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180);

    public override string ToString() =>
        $"{Street}, {Number}{(Complement is null ? string.Empty : $" - {Complement}")}, {District}, {City}/{State}, {FormattedZipCode}";
}