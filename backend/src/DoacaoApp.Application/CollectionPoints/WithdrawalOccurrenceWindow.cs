using DoacaoApp.Domain.Errors;
using ErrorOr;

namespace DoacaoApp.Application.CollectionPoints;

/// <summary>
/// Janela aceita para o instante informado de uma retirada. Sem ela, um lançamento retroativo
/// poderia cair num ciclo já encerrado e escapar do limite municipal do ciclo vigente.
/// </summary>
public static class WithdrawalOccurrenceWindow
{
    /// <summary>Tolerância para relógios de cliente levemente adiantados.</summary>
    public static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(5);

    /// <summary>Quantos dias um gestor de ponto pode lançar retroativamente (Admins não têm limite).</summary>
    public const int ManagerMaxDaysInThePast = 7;

    /// <summary>
    /// Resolve o instante da retirada: sem data informada vale o agora; datas no futuro são recusadas
    /// e, para quem não é Admin, datas além de <see cref="ManagerMaxDaysInThePast"/> dias também.
    /// </summary>
    public static ErrorOr<DateTime> Resolve(DateTime? requestedUtc, DateTime nowUtc, bool isAdmin)
    {
        if (requestedUtc is null)
        {
            return nowUtc;
        }

        var occurredAtUtc = requestedUtc.Value.ToUniversalTime();

        if (occurredAtUtc > nowUtc + AllowedClockSkew)
        {
            return DomainErrors.Withdrawal.OccurredInTheFuture;
        }

        if (!isAdmin && occurredAtUtc < nowUtc.AddDays(-ManagerMaxDaysInThePast))
        {
            return DomainErrors.Withdrawal.OccurredTooLongAgo(ManagerMaxDaysInThePast);
        }

        return occurredAtUtc;
    }
}
