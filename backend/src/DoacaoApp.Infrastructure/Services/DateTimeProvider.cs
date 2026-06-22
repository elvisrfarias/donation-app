using DoacaoApp.Application.Common.Abstractions;

namespace DoacaoApp.Infrastructure.Services;

/// <summary>Relógio real (UTC). Substituível por um fake nos testes.</summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}