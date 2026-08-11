using ErrorOr;

namespace DoacaoApp.Application.Common.Security;

/// <summary>Erros de escopo de acesso por ponto de coleta.</summary>
public static class ScopeErrors
{
    public static Error CollectionPointForbidden =>
        Error.Forbidden("Scope.CollectionPointForbidden", "Você só pode operar no seu próprio ponto de coleta.");
}
