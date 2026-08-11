namespace DoacaoApp.Application.Common.Security;

/// <summary>Marca um Command que só pode ser executado no ponto de coleta do usuário (a menos que Admin).</summary>
public interface IScopedToCollectionPoint
{
    Guid CollectionPointId { get; }
}
