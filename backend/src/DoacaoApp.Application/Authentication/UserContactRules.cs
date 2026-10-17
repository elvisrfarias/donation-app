using DoacaoApp.Domain.ValueObjects;
using FluentValidation;

namespace DoacaoApp.Application.Authentication;

/// <summary>Regras dos dados de contato de um usuário gerencial (o telefone é opcional).</summary>
public static class UserContactRules
{
    public static IRuleBuilderOptions<T, string?> MustBeValidOptionalPhone<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(phone => string.IsNullOrWhiteSpace(phone) || !PhoneNumber.Create(phone).IsError)
            .WithMessage("Telefone inválido. Informe DDD + número (10 ou 11 dígitos).");

    /// <summary>Telefone já validado por <see cref="MustBeValidOptionalPhone{T}"/>; vazio vira null.</summary>
    public static PhoneNumber? ToOptionalPhoneNumber(this string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : PhoneNumber.Create(phone).Value;
}
