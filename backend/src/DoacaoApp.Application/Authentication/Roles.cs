namespace DoacaoApp.Application.Authentication;

/// <summary>Roles de autorização do sistema (role-based authorization).</summary>
public static class Roles
{
    /// <summary>Acesso total: gerencia usuários, pontos, famílias e relatórios.</summary>
    public const string Admin = "Admin";

    /// <summary>Gestor de um ponto de coleta: cadastra beneficiários e registra retiradas.</summary>
    public const string PointManager = "PointManager";

    /// <summary>Somente leitura de dados gerenciais e relatórios.</summary>
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> All = [Admin, PointManager, Viewer];

    public static bool IsValid(string role) => All.Contains(role);
}