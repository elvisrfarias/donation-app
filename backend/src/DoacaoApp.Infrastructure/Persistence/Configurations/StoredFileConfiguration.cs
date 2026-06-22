using System.Text.Json;
using DoacaoApp.Domain.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DoacaoApp.Infrastructure.Persistence.Configurations;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("stored_files");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(f => f.Provider)
            .HasColumnName("storage_provider")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.Bucket).HasColumnName("bucket").HasMaxLength(100).IsRequired();
        builder.Property(f => f.ObjectKey).HasColumnName("object_key").HasMaxLength(512).IsRequired();
        builder.Property(f => f.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(260).IsRequired();
        builder.Property(f => f.Extension).HasColumnName("extension").HasMaxLength(20);
        builder.Property(f => f.ContentType).HasColumnName("content_type").HasMaxLength(150).IsRequired();
        builder.Property(f => f.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(f => f.ChecksumSha256).HasColumnName("checksum_sha256").HasMaxLength(64);
        builder.Property(f => f.ETag).HasColumnName("etag").HasMaxLength(100);
        builder.Property(f => f.Width).HasColumnName("width");
        builder.Property(f => f.Height).HasColumnName("height");

        builder.Property(f => f.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.Visibility)
            .HasColumnName("visibility")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.OwnerType).HasColumnName("owner_type").HasMaxLength(80);
        builder.Property(f => f.OwnerId).HasColumnName("owner_id");
        builder.Property(f => f.UploadedByUserId).HasColumnName("uploaded_by_user_id");

        builder.Property(f => f.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(f => f.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(f => f.DeletedAtUtc).HasColumnName("deleted_at_utc");

        // Metadados extensíveis: Dictionary<string,string> <-> jsonb. A propriedade pública é
        // somente leitura, então mapeamos o campo de apoio diretamente.
        var metadataConverter = new ValueConverter<Dictionary<string, string>, string>(
            value => JsonSerializer.Serialize(value, JsonSerializerOptions.Default),
            json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonSerializerOptions.Default)
                    ?? new Dictionary<string, string>());

        var metadataComparer = new ValueComparer<Dictionary<string, string>>(
            (a, b) => JsonSerializer.Serialize(a, JsonSerializerOptions.Default)
                      == JsonSerializer.Serialize(b, JsonSerializerOptions.Default),
            v => v == null ? 0 : JsonSerializer.Serialize(v, JsonSerializerOptions.Default).GetHashCode(),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(
                     JsonSerializer.Serialize(v, JsonSerializerOptions.Default), JsonSerializerOptions.Default)
                 ?? new Dictionary<string, string>());

        builder.Ignore(f => f.Metadata);
        builder.Property<Dictionary<string, string>>("_metadata")
            .HasField("_metadata")
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(metadataConverter, metadataComparer);

        // Chave natural: cada (bucket, chave) é único.
        builder.HasIndex(f => new { f.Bucket, f.ObjectKey }).IsUnique();
        // Buscas por dono (ex.: fotos de um ponto de coleta).
        builder.HasIndex(f => new { f.OwnerType, f.OwnerId });
        // Filtragem por ciclo de vida.
        builder.HasIndex(f => f.Status);
        // Deduplicação por conteúdo.
        builder.HasIndex(f => f.ChecksumSha256);
    }
}
