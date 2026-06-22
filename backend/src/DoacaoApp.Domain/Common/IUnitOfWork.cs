namespace DoacaoApp.Domain.Common;

/// <summary>
/// Abstração de transação/persistência. Implementada pelo DbContext na Infrastructure.
/// Mantém o Domain/Application ignorantes quanto ao EF Core.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}