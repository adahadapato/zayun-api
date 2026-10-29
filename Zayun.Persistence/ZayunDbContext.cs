using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Persistence.Configurations.Identity;
using Zayun.Persistence.Converters;

namespace Zayun.Persistence;

/// <summary>
/// The one database context for Zayun: Identity's tables (users, roles) AND every Domain table.
/// Also the <see cref="IUnitOfWork"/>: repositories add and change entities, and one
/// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> saves them all in one transaction.
/// </summary>
/// <remarks>
/// <para><b>One context, one scope:</b> registered per request, so Identity's UserManager and our repositories share
/// the same instance.</para>
/// <para><b>Mappings</b> come from the <c>IEntityTypeConfiguration</c> classes in <c>Configurations/</c>, all loaded
/// with <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>; nothing is configured inline here.</para>
/// <para><b>Audit columns</b> (CreatedAt / UpdatedAt) are filled by the audit interceptor, registered in
/// DependencyInjection.</para>
/// </remarks>
public sealed class ZayunDbContext(DbContextOptions<ZayunDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IUnitOfWork
{
    // ---------- Website content (children are reached through their root) ----------

    /// <summary>Projects (with their pictures).</summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Publications (with their links).</summary>
    public DbSet<Publication> Publications => Set<Publication>();

    /// <summary>Presentations (with their picture).</summary>
    public DbSet<Presentation> Presentations => Set<Presentation>();

    /// <summary>Areas of experience.</summary>
    public DbSet<ExperienceArea> ExperienceAreas => Set<ExperienceArea>();

    /// <summary>Skill groups.</summary>
    public DbSet<SkillGroup> SkillGroups => Set<SkillGroup>();

    /// <summary>The core expertise list.</summary>
    public DbSet<CoreExpertise> CoreExpertise => Set<CoreExpertise>();

    /// <summary>Awards and recognition (with their picture).</summary>
    public DbSet<Award> Awards => Set<Award>();

    /// <summary>The owner's personal details (one row): name, headline, email, phones, addresses.</summary>
    public DbSet<PersonalDetails> PersonalDetails => Set<PersonalDetails>();

    /// <summary>Named pictures on the website, e.g. the owner's photo.</summary>
    public DbSet<SiteImage> SiteImages => Set<SiteImage>();

    /// <summary>Online profiles (ORCID, ResearchGate, LinkedIn…).</summary>
    public DbSet<Profile> Profiles => Set<Profile>();

    /// <summary>The shared tag list (project tags, experience and skill-group skills).</summary>
    public DbSet<Tag> Tags => Set<Tag>();

    // ---------- Private ----------

    /// <summary>The owner's private documents (admin only).</summary>
    public DbSet<Document> Documents => Set<Document>();

    // ---------- Inbox and sign-in ----------

    /// <summary>Messages from the Contact form.</summary>
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    /// <summary>Hashed refresh tokens (sign-in), managed by the Identity services.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Identity's tables first, then every configuration in this assembly.</summary>
    /// <param name="builder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);   // Identity's tables (must come first)
        builder.ApplyConfigurationsFromAssembly(typeof(ZayunDbContext).Assembly);
    }

    /// <summary>Defaults for every column of a type, unless a configuration says otherwise.</summary>
    /// <param name="configurationBuilder">The conventions builder.</param>
    /// <remarks><b>Dates:</b> every DateTime is UTC and read back marked as UTC (so JSON writes the "Z").</remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Starts a database transaction on this context. Everything saved through this context (UserManager,
    /// repositories) until <see cref="ITransaction.CommitAsync"/> is committed together, or rolled back together.
    /// </remarks>
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await Database.BeginTransactionAsync(cancellationToken));
}
