using DoacaoApp.Application.Common.Abstractions;

namespace DoacaoApp.Application.Tests.Common;

/// <summary>Relógio determinístico para testes.</summary>
public sealed class FakeClock(DateTime now) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = now;
}