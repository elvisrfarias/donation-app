using DoacaoApp.Domain.CollectionPoints;
using DoacaoApp.Domain.Beneficiaries;
using DoacaoApp.Domain.Reviews;
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

public sealed class BeneficiaryIdConverter()
    : ValueConverter<BeneficiaryId, Guid>(id => id.Value, value => BeneficiaryId.From(value));

public sealed class StoredFileIdConverter()
    : ValueConverter<StoredFileId, Guid>(id => id.Value, value => StoredFileId.From(value));

public sealed class ReviewIdConverter()
    : ValueConverter<ReviewId, Guid>(id => id.Value, value => ReviewId.From(value));

public sealed class StockItemIdConverter()
    : ValueConverter<StockItemId, Guid>(id => id.Value, value => StockItemId.From(value));

public sealed class BasketTemplateIdConverter()
    : ValueConverter<BasketTemplateId, Guid>(id => id.Value, value => BasketTemplateId.From(value));