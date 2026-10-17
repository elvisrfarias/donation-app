using AspNetCoreRateLimit;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Api.RateLimiting;

/// <summary>
/// Configuração do AspNetCoreRateLimit em que uma regra com curinga no meio do caminho
/// (ex.: <c>POST:/api/public/collection-points/*/reviews</c>) conta todas as rotas que casam num
/// único contador por IP. Sem isso, a biblioteca conta cada caminho concreto separadamente e um
/// cliente contornaria o limite trocando o id da rota. Regras de prefixo (<c>/api/auth/*</c>) e de
/// caminho exato continuam contando por caminho, como no padrão da biblioteca.
/// </summary>
public sealed class IdPlaceholderRateLimitConfiguration(
    IOptions<IpRateLimitOptions> ipOptions,
    IOptions<ClientRateLimitOptions> clientOptions)
    : RateLimitConfiguration(ipOptions, clientOptions)
{
    public override ICounterKeyBuilder EndpointCounterKeyBuilder { get; } = new IdPlaceholderCounterKeyBuilder();
}

/// <summary>Agrupa pelo endpoint da regra quando ela tem um curinga de id (<c>/*/</c>); senão, pelo caminho.</summary>
public sealed class IdPlaceholderCounterKeyBuilder : ICounterKeyBuilder
{
    private const string IdPlaceholder = "/*/";

    private readonly PathCounterKeyBuilder _perPath = new();
    private readonly EndpointCounterKeyBuilder _perRule = new();

    public string Build(ClientRequestIdentity requestIdentity, RateLimitRule rule) =>
        rule.Endpoint.Contains(IdPlaceholder, StringComparison.Ordinal)
            ? _perRule.Build(requestIdentity, rule)
            : _perPath.Build(requestIdentity, rule);
}
