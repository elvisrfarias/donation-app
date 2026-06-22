namespace DoacaoApp.Domain.Common;

/// <summary>
/// Base de toda entidade com identidade fortemente tipada.
/// Igualdade é por identidade (Id + tipo), não por valor.
/// Mantém a lista de Domain Events pendentes até serem despachados.
/// </summary>
/// <typeparam name="TId">Tipo do identificador (ex.: <c>FamilyId</c>).</typeparam>
public abstract class Entity<TId> : IHasDomainEvents, IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(TId id) => Id = id;

    // Construtor sem parâmetros exigido pela materialização do EF Core.
    protected Entity()
    {
        Id = default!;
    }

    public TId Id { get; protected init; }

    /// <summary>Eventos de domínio acumulados, despachados após o commit.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool Equals(Entity<TId>? other) =>
        other is not null
        && GetType() == other.GetType()
        && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}