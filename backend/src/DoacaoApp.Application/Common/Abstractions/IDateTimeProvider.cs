namespace DoacaoApp.Application.Common.Abstractions;

/// <summary>
/// Abstrai o relógio para tornar as regras de ciclo testáveis (sem DateTime.UtcNow direto).
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}