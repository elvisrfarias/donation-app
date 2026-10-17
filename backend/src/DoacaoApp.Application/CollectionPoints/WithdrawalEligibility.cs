using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Errors;
using ErrorOr;

namespace DoacaoApp.Application.CollectionPoints;

/// <summary>Só beneficiários ativos e vinculados ao ponto podem ter retiradas registradas ou corrigidas nele.</summary>
public static class WithdrawalEligibility
{
    public static ErrorOr<Success> Check(Beneficiary beneficiary, CollectionPointId pointId)
    {
        if (!beneficiary.IsActive)
        {
            return DomainErrors.Withdrawal.BeneficiaryInactive;
        }

        if (!beneficiary.IsLinkedTo(pointId))
        {
            return DomainErrors.Withdrawal.BeneficiaryNotLinkedToPoint;
        }

        return Result.Success;
    }
}
