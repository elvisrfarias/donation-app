namespace DoacaoApp.Domain.Notices;

/// <summary>Quem enxerga um aviso: toda a comunidade ou apenas a rede de gestores.</summary>
public enum NoticeAudience
{
    Public = 1,
    Managers = 2,
}
