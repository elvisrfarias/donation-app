namespace DoacaoApp.Application.Beneficiaries.Contracts;

/// <summary>Abrevia o nome de uma pessoa para exibição a quem não tem acesso ao cadastro.</summary>
public static class PersonNameMask
{
    /// <summary>Mantém o primeiro nome e a inicial do último sobrenome: "Maria Souza Silva" vira "Maria S.".</summary>
    public static string Abbreviate(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            0 => string.Empty,
            1 => parts[0],
            _ => $"{parts[0]} {char.ToUpperInvariant(parts[^1][0])}.",
        };
    }
}
