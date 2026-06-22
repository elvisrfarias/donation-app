using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Families;
using DoacaoApp.Domain.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DoacaoApp.Infrastructure.Persistence.Converters;

/// <summary>
/// Conversores que mapeiam os IDs fortemente tipados (record struct) para Guid no banco.
/// Registrados globalmente em <c>ConfigureConventions</c> do DbContext.
/// </summary>
public sealed class CollectionPointIdConverter()
    : ValueConverter<CollectionPointId, Guid>(id => id.Value, value => CollectionPointId.From(value));

public sealed class WithdrawalIdConverter()
    : ValueConverter<WithdrawalId, Guid>(id => id.Value, value => WithdrawalId.From(value));

public sealed class FamilyIdConverter()
    : ValueConverter<FamilyId, Guid>(id => id.Value, value => FamilyId.From(value));

public sealed class BeneficiaryIdConverter()
    : ValueConverter<BeneficiaryId, Guid>(id => id.Value, value => BeneficiaryId.From(value));

public sealed class StoredFileIdConverter()
    : ValueConverter<StoredFileId, Guid>(id => id.Value, value => StoredFileId.From(value));