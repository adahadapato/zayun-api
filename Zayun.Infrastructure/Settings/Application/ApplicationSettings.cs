using Zayun.Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace Zayun.Infrastructure.Settings.Application;

/// <summary>
/// The <c>"AppSettings"</c> section: Zayun's own public addresses. Keys are listed in
/// <see cref="AppSettings.Application"/>.
/// </summary>
/// <remarks>
/// Checked at start-up: both must be full addresses (https://…) WITHOUT a trailing slash, because code
/// appends paths like <c>"/files/…"</c>. Development overrides <see cref="ApiBaseUrl"/> in
/// appsettings.Development.json.
/// </remarks>
public sealed class ApplicationSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = AppSettings.Application.Section;

    /// <summary>The API's public address, e.g. "https://api.zayun.net". Used for file links and links in emails.</summary>
    [Required, Url]
    public string ApiBaseUrl { get; set; } = string.Empty;

    /// <summary>The website's public address, e.g. "https://zayun.net".</summary>
    [Required, Url]
    public string WebsiteBaseUrl { get; set; } = string.Empty;
}