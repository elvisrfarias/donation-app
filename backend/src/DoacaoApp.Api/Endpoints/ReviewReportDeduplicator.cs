using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace DoacaoApp.Api.Endpoints;

/// <summary>
/// Conta no máximo uma denúncia por IP e avaliação dentro de <see cref="Window"/>, em memória, para
/// que repetir o POST não infle o contador de denúncias. Repetições respondem como sucesso.
/// </summary>
public static class ReviewReportDeduplicator
{
    public static readonly TimeSpan Window = TimeSpan.FromHours(24);

    /// <summary>Registra a denúncia e devolve <c>true</c> só na primeira vez do IP para essa avaliação.</summary>
    public static bool TryRegister(IMemoryCache cache, IPAddress? clientIp, Guid reviewId)
    {
        var key = $"review-report:{clientIp?.ToString() ?? "unknown"}:{reviewId:N}";
        if (cache.TryGetValue(key, out _))
        {
            return false;
        }

        cache.Set(key, true, Window);
        return true;
    }
}
