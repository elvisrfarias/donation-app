using System.Runtime.CompilerServices;

// Permite que a Infrastructure use os construtores de reidratação (internal) dos
// Value Objects ao configurar conversores do EF Core, sem expô-los publicamente.
[assembly: InternalsVisibleTo("DoacaoApp.Infrastructure")]

namespace DoacaoApp.Domain.Common;

/// <summary>Tipo-âncora para localizar o assembly do Domain via reflection.</summary>
public sealed class AssemblyMarker;