using FluentValidation;

namespace DoacaoApp.Application.Authentication;

/// <summary>
/// Regras de senha espelhando a política do Identity (Infrastructure/DependencyInjection.cs), para o
/// usuário receber a mensagem em português antes de a senha chegar ao Identity.
/// </summary>
public static class PasswordPolicyRules
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public static IRuleBuilderOptions<T, string> MustSatisfyPasswordPolicy<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Informe a senha.")
            .MinimumLength(MinimumLength).WithMessage($"A senha deve ter no mínimo {MinimumLength} caracteres.")
            .MaximumLength(MaximumLength).WithMessage($"A senha deve ter no máximo {MaximumLength} caracteres.")
            .Matches("[0-9]").WithMessage("A senha deve conter pelo menos um número.");
}
