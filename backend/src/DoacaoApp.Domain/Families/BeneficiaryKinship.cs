using DoacaoApp.Domain.ValueObjects;

namespace DoacaoApp.Domain.Families;

/// <summary>
/// Um vínculo de parentesco declarado por um beneficiário com outro. Nome e CPF do
/// beneficiário relacionado são uma cópia (snapshot) do momento em que o vínculo foi
/// criado — evita precisar carregar o agregado <see cref="Family"/> do outro lado (que
/// pode ser um agregado diferente) só para exibir quem é o parente.
/// </summary>
public sealed record BeneficiaryKinship(
    BeneficiaryId RelatedBeneficiaryId,
    string RelatedBeneficiaryName,
    Cpf RelatedBeneficiaryCpf,
    KinshipType Type,
    DateTime CreatedAtUtc);
