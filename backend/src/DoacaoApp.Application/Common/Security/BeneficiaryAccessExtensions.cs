using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;

namespace DoacaoApp.Application.Common.Security;

/// <summary>Regra de visibilidade de um beneficiário já carregado, para os guards de leitura escopada.</summary>
public static class BeneficiaryAccessExtensions
{
    public static bool CanAccess(this ICurrentUser currentUser, Beneficiary beneficiary) =>
        currentUser.IsAdmin
        || (currentUser.CollectionPointId is { } pointId && beneficiary.IsLinkedTo(CollectionPointId.From(pointId)));
}
