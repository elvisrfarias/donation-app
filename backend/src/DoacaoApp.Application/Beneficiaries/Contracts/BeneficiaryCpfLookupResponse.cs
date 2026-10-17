using DoacaoApp.Domain.Beneficiaries;

namespace DoacaoApp.Application.Beneficiaries.Contracts;

/// <summary>
/// Resultado da busca por CPF. Quem já tem acesso ao cadastro (Admin ou ponto vinculado) recebe
/// o registro completo em <see cref="Beneficiary"/>; os demais recebem só o necessário para decidir
/// o vínculo no balcão: nome abreviado, CPF mascarado e se a pessoa está ativa.
/// </summary>
public sealed record BeneficiaryCpfLookupResponse(
    Guid Id,
    string Name,
    string Cpf,
    bool IsActive,
    bool IsLinkedToMyPoint,
    BeneficiaryResponse? Beneficiary);

public static class BeneficiaryCpfLookupMappings
{
    public static BeneficiaryCpfLookupResponse ToFullLookup(this Beneficiary beneficiary, bool isLinkedToMyPoint) =>
        new(
            beneficiary.Id.Value,
            beneficiary.Name,
            beneficiary.Cpf.Formatted,
            beneficiary.IsActive,
            isLinkedToMyPoint,
            beneficiary.ToResponse());

    public static BeneficiaryCpfLookupResponse ToMaskedLookup(this Beneficiary beneficiary) =>
        new(
            beneficiary.Id.Value,
            PersonNameMask.Abbreviate(beneficiary.Name),
            beneficiary.Cpf.Masked,
            beneficiary.IsActive,
            IsLinkedToMyPoint: false,
            Beneficiary: null);
}
