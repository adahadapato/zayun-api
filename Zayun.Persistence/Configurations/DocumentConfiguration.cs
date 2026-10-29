using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;
using Zayun.Domain.ValueObjects;

namespace Zayun.Persistence.Configurations;

/// <summary>
/// Table <c>Documents</c>: the owner's private documents, with the file's details stored as columns
/// (<c>File_Path</c>, <c>File_OriginalFileName</c>…).
/// </summary>
public sealed class DocumentConfiguration : EntityConfiguration<Document>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.Property(d => d.Title).IsRequired().HasMaxLength(Document.TitleMaxLength);
        builder.Property(d => d.Category).IsRequired();
        builder.Property(d => d.Notes).HasMaxLength(Document.NotesMaxLength);

        builder.OwnsOne(d => d.File, file =>
        {
            file.Property(f => f.Path).IsRequired().HasMaxLength(DocumentFile.PathMaxLength);
            file.Property(f => f.OriginalFileName).IsRequired().HasMaxLength(DocumentFile.OriginalFileNameMaxLength);
            file.Property(f => f.ContentType).IsRequired().HasMaxLength(DocumentFile.ContentTypeMaxLength);
            file.Property(f => f.FileSizeBytes).IsRequired();
            file.Property(f => f.Hash).IsRequired().HasMaxLength(DocumentFile.HashMaxLength);
        });
        builder.Navigation(d => d.File).IsRequired();

        builder.HasIndex(d => new { d.Category, d.CreatedAt });
    }
}
