using DoacaoApp.Domain.Beneficiaries;

namespace DoacaoApp.Application.Beneficiaries.Contracts;

public sealed record BeneficiaryKinshipResponse(
    Guid RelatedBeneficiaryId,
    string RelatedBeneficiaryName,
    string RelatedBeneficiaryCpf,
    string Type);

public sealed record BeneficiaryResponse(
    Guid Id,
    string Name,
    string Cpf,
    DateOnly? BirthDate,
    IReadOnlyList<Guid> LinkedPointIds,
    bool IsActive,
    IReadOnlyList<BeneficiaryKinshipResponse> Kinships);

public static class BeneficiaryMappings
{
    public static BeneficiaryKinshipResponse ToResponse(this BeneficiaryKinship kinship) =>
        new(
            kinship.RelatedBeneficiaryId.Value,
            kinship.RelatedBeneficiaryName,
            kinship.RelatedBeneficiaryCpf.Formatted,
            kinship.Type.ToString());

    public static BeneficiaryResponse ToResponse(this Beneficiary beneficiary) =>
        new(
            beneficiary.Id.Value,
            beneficiary.Name,
            beneficiary.Cpf.Formatted,
            beneficiary.BirthDate,
            beneficiary.LinkedPointIds.Select(id => id.Value).ToList(),
            beneficiary.IsActive,
            beneficiary.Kinships.Select(k => k.ToResponse()).ToList());
}
