using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Zayun.Persistence.Converters;

/// <summary>
/// Marks every <see cref="DateTime"/> read from the database as UTC.
/// </summary>
/// <remarks>
/// <para>Zayun always saves UTC (TimeProvider, the audit interceptor), but SQL Server's <c>datetime2</c> doesn't
/// store the kind, so EF would read it back as "Unspecified". JSON then writes it without a <c>Z</c>, and apps read it as
/// local time.</para>
/// <para>Writing: a UTC value is stored as is; a local value is converted to UTC first; an unspecified one is assumed to
/// be UTC already.</para>
/// <para>Applied to every DateTime and DateTime? column in <c>ZayunDbContext.ConfigureConventions</c>. Nulls never
/// reach a converter, so one class covers both.</para>
/// </remarks>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>Creates the converter.</summary>
    public UtcDateTimeConverter()
        : base(
            toDatabase => toDatabase.Kind == DateTimeKind.Local ? toDatabase.ToUniversalTime() : toDatabase,
            fromDatabase => DateTime.SpecifyKind(fromDatabase, DateTimeKind.Utc))
    {
    }
}