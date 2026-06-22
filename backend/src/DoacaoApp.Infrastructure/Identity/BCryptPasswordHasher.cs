using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using BCryptNet = BCrypt.Net.BCrypt;

namespace DoacaoApp.Infrastructure.Identity;

/// <summary>
/// Implementação explícita de <see cref="IPasswordHasher{TUser}"/> usando BCrypt,
/// substituindo o hasher padrão do ASP.NET Core Identity (PBKDF2).
///
/// POR QUE BCrypt em vez do padrão (PBKDF2-HMAC-SHA256)?
///  1. Função adaptativa: o "work factor" é um parâmetro explícito (custo exponencial).
///     Conforme o hardware evolui, basta aumentar o fator — sem mudar o algoritmo.
///  2. Salt individual por usuário gerado automaticamente e embutido no próprio hash
///     (formato $2a$&lt;cost&gt;$&lt;salt+hash&gt;), eliminando a necessidade de coluna de salt separada.
///  3. Projetado para ser lento e resistente a ataques de GPU/ASIC (acesso intensivo a memória
///     no agendamento de chaves do Blowfish), dificultando brute force offline.
///  4. Detecção de rehash: se aumentarmos o work factor, senhas antigas são re-hasheadas
///     de forma transparente no próximo login (PasswordVerificationResult.SuccessRehashNeeded).
/// </summary>
public sealed class BCryptPasswordHasher<TUser>(IOptions<SecurityOptions> options)
    : IPasswordHasher<TUser>
    where TUser : class
{
    private readonly int _workFactor = options.Value.BCryptWorkFactor;

    public string HashPassword(TUser user, string password) =>
        // O salt é gerado internamente pelo BCrypt e fica embutido no hash retornado.
        BCryptNet.HashPassword(password, workFactor: _workFactor);

    public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
        {
            return PasswordVerificationResult.Failed;
        }

        bool isValid;
        try
        {
            isValid = BCryptNet.Verify(providedPassword, hashedPassword);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash em formato inesperado (ex.: migrado de outro sistema) -> tratado como inválido.
            return PasswordVerificationResult.Failed;
        }

        if (!isValid)
        {
            return PasswordVerificationResult.Failed;
        }

        // Se o custo configurado mudou, sinaliza ao Identity para re-hashear no login.
        return BCryptNet.PasswordNeedsRehash(hashedPassword, _workFactor)
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }
}