namespace DoacaoApp.Application.Activity.Contracts;

/// <summary>Um acontecimento recente no ponto (doação, aviso, avaliação ou alerta de estoque).</summary>
public sealed record ActivityItemResponse(
    string Kind,
    string Title,
    string Description,
    DateTime OccurredAtUtc);

/// <summary>Feed de atividade do ponto, do mais recente para o mais antigo.</summary>
public sealed record ActivityFeedResponse(IReadOnlyList<ActivityItemResponse> Items);

public static class ActivityKinds
{
    public const string Donation = "Donation";
    public const string Notice = "Notice";
    public const string Review = "Review";
    public const string LowStock = "LowStock";
}
