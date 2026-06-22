using MediatR;

namespace DoacaoApp.Domain.Common;

/// <summary>
/// Marca um evento de domínio. Herda de <see cref="INotification"/> (MediatR.Contracts)
/// para que possa ser publicado pelo dispatcher sem que o Domain conheça o MediatR completo.
/// </summary>
public interface IDomainEvent : INotification;