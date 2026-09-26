using DoacaoApp.Domain.ValueObjects;

namespace DoacaoApp.Application.Beneficiaries.Contracts;

/// <summary>Endereço enviado pelo cliente no cadastro/edição do beneficiário (todos os campos são texto livre).</summary>
public sealed record BeneficiaryAddressInput(
    string? Street,
    string? Number,
    string? Complement,
    string? District,
    string? City,
    string? State,
    string? ZipCode)
{
    public bool IsBlank =>
        string.IsNullOrWhiteSpace(Street)
        && string.IsNullOrWhiteSpace(Number)
        && string.IsNullOrWhiteSpace(Complement)
        && string.IsNullOrWhiteSpace(District)
        && string.IsNullOrWhiteSpace(City)
        && string.IsNullOrWhiteSpace(State)
        && string.IsNullOrWhiteSpace(ZipCode);
}

/// <summary>Endereço do beneficiário devolvido pela API (CEP formatado).</summary>
public sealed record BeneficiaryAddressResponse(
    string Street,
    string Number,
    string? Complement,
    string District,
    string City,
    string State,
    string ZipCode)
{
    public static BeneficiaryAddressResponse From(Address address) =>
        new(
            address.Street,
            address.Number,
            address.Complement,
            address.District,
            address.City,
            address.State,
            address.FormattedZipCode);
}
