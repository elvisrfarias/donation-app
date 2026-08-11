using DoacaoApp.Application.Common.Abstractions;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families;

namespace DoacaoApp.Application.Common.Security;

/// <summary>Regra de visibilidade de uma família já carregada, para os guards de leitura escopada.</summary>
public static class FamilyAccessExtensions
{
    public static bool CanAccess(this ICurrentUser currentUser, Family family) =>
        currentUser.IsAdmin
        || family.HasBeneficiaryRegisteredBy(CollectionPointId.From(currentUser.CollectionPointId ?? Guid.Empty));
}
