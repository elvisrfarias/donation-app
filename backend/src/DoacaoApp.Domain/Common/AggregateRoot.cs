namespace DoacaoApp.Domain.Common;

/// <summary>
/// Raiz de agregado. É o único ponto de entrada para modificar o agregado e a
/// fronteira de consistência transacional. Repositórios operam apenas sobre raízes.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }
}