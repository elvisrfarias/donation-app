namespace DoacaoApp.Domain.Common;

/// <summary>
/// Contrato não-genérico que expõe os Domain Events de uma entidade, permitindo ao
/// dispatcher coletá-los pelo ChangeTracker sem conhecer o tipo do Id.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}