using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations;

/// <summary>Table <c>Quotes</c>: quotes, proverbs and the owner's sayings for the "Quote of the day" card.</summary>
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

        builder.Ignore(q => q.IsPinned);   // worked out from PinnedMonth / PinnedDay, not stored

        builder.HasIndex(q => new { q.PinnedMonth, q.PinnedDay });   // "is anything pinned to today?"
    }
}
