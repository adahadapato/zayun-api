using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;
using Zayun.Domain.ValueObjects;
using Zayun.Persistence.Mappings;

namespace Zayun.Persistence.Configurations;

/// <summary>
/// Table <c>Quotes</c>: quotes, proverbs and the owner's sayings for the "Quote of the day" card, with the optional
/// picture (<c>Picture_Path</c>…) and animation (<c>Animation_Path</c>…) stored as columns, all null when there is none.
/// </summary>
public sealed class QuoteConfiguration : ContentEntityConfiguration<Quote>
{
    /// <inheritdoc />
    protected override void ConfigureContent(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("Quotes");

        builder.Property(q => q.Kind).IsRequired();
        builder.Property(q => q.Text).IsRequired().HasMaxLength(Quote.TextMaxLength);
        builder.Property(q => q.Author).IsRequired().HasMaxLength(Quote.AuthorMaxLength);
        builder.Property(q => q.Source).HasMaxLength(Quote.SourceMaxLength);
        builder.Property(q => q.Language).HasMaxLength(Quote.LanguageMaxLength);
        builder.Property(q => q.Translation).HasMaxLength(Quote.TranslationMaxLength);
        builder.Property(q => q.Series).HasMaxLength(Quote.SeriesMaxLength);
        builder.Property(q => q.Story).HasMaxLength(Quote.StoryMaxLength);
        builder.Property(q => q.IsSourceChecked).IsRequired();
        builder.Property(q => q.PinnedMonth);
        builder.Property(q => q.PinnedDay);

        builder.Property(q => q.PictureAlt).HasMaxLength(Quote.PictureAltMaxLength);
        builder.OwnsOne(q => q.Picture, picture => picture.MapImageFile());
        builder.OwnsOne(q => q.Animation, animation =>
        {
            animation.Property(a => a.Path).IsRequired().HasMaxLength(AnimationFile.PathMaxLength);
            animation.Property(a => a.ContentType).IsRequired().HasMaxLength(AnimationFile.ContentTypeMaxLength);
            animation.Property(a => a.FileSizeBytes).IsRequired();
            animation.Property(a => a.Hash).IsRequired().HasMaxLength(AnimationFile.HashMaxLength);
        });

        builder.Ignore(q => q.IsPinned);   // worked out from PinnedMonth / PinnedDay, not stored

        builder.HasIndex(q => new { q.PinnedMonth, q.PinnedDay });   // "is anything pinned to today?"
    }
}
