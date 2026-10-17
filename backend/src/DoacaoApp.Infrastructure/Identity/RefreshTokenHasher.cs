using System.Security.Cryptography;
using System.Text;

namespace DoacaoApp.Infrastructure.Identity;

/// <summary>
/// Hash SHA-256 (hex, 64 caracteres) do refresh token: o banco só guarda o hash, então um vazamento da
/// tabela não entrega sessões utilizáveis. Basta SHA-256 sem salt porque o token já é aleatório de 512 bits.
/// </summary>
public static class RefreshTokenHasher
{
    public static string Hash(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
