using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zayun.Domain.Entities;

namespace Zayun.Persistence.Configurations.Identity;

/// <summary>
/// Table <c>RefreshTokens</c>: hashed refresh tokens, linked to their user. Deleting a user deletes their tokens.
/// </summary>
/// <remarks>
/// <para>Not based on EntityConfiguration (no audit / soft-delete columns): tokens are revoked, then cleaned up when
/// expired.</para>
/// <para>Lives with the Identity configurations because of its foreign key to <see cref="ApplicationUser"/>; the class
/// itself is in Domain.</para>
/// </remarks>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>Maps the table.</summary>
    /// <param name="builder">EF Core's builder for this entity.</param>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(RefreshToken.TokenHashLength).IsFixedLength();
        builder.HasIndex(t => t.TokenHash).IsUnique();                 // look-up on every refresh

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.ExpiresAtUtc).IsRequired();
        builder.Property(t => t.RevokedAtUtc);
        builder.Property(t => t.ReplacedByTokenId);

        builder.HasOne<ApplicationUser>()
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(t => new { t.UserId, t.RevokedAtUtc });       // "revoke all of this user's tokens"
    }
}