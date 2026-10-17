using System.Security.Cryptography;
using System.Text;

namespace DoacaoApp.Infrastructure.Identity;

/// <summary>
/// Hash SHA-256 (hex, 64 caracteres) de tokens opacos (refresh token, token de redefinição de senha): o banco só
/// guarda o hash, então um vazamento da tabela não entrega tokens utilizáveis. Basta SHA-256 sem salt porque
/// os tokens já são aleatórios de pelo menos 256 bits.
/// </summary>
public static class OpaqueTokenHasher
{
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
