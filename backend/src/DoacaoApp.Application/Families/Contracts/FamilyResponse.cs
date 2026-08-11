using DoacaoApp.Domain.Families;

namespace DoacaoApp.Application.Families.Contracts;

public sealed record CycleResponse(string Type, int MaxWithdrawalsPerCycle);

public sealed record BeneficiaryKinshipResponse(
    Guid RelatedBeneficiaryId,
    string RelatedBeneficiaryName,
    string RelatedBeneficiaryCpf,
    string Type);

public sealed record BeneficiaryResponse(
    Guid Id,
    Guid FamilyId,
    string Name,
    string Cpf,
    DateOnly? BirthDate,
    Guid RegisteredByPointId,
    bool IsActive,
    IReadOnlyList<BeneficiaryKinshipResponse> Kinships);

public sealed record FamilyResponse(
    Guid Id,
    string Name,
    CycleResponse Cycle,
    IReadOnlyList<BeneficiaryResponse> Beneficiaries);

public static class FamilyMappings
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
            beneficiary.FamilyId.Value,
            beneficiary.Name,
            beneficiary.Cpf.Formatted,
            beneficiary.BirthDate,
            beneficiary.RegisteredByPointId.Value,
            beneficiary.IsActive,
            beneficiary.Kinships.Select(k => k.ToResponse()).ToList());

    public static FamilyResponse ToResponse(this Family family) =>
        new(
            family.Id.Value,
            family.Name,
            new CycleResponse(family.Cycle.Type.ToString(), family.Cycle.MaxWithdrawalsPerCycle),
            family.Beneficiaries.Select(b => b.ToResponse()).ToList());
}