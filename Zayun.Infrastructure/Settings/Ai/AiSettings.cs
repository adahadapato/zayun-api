using System.ComponentModel.DataAnnotations;
using Zayun.Domain.Constants;

namespace Zayun.Infrastructure.Settings.Ai;

/// <summary>
/// The <c>"Ai"</c> section (keys in <see cref="AppSettings.Ai"/>): the AI tag check with Claude.
/// </summary>
/// <remarks>
/// <para><see cref="ApiKey"/> is a SECRET: User Secrets in development, the CI pipeline (ANTHROPIC_API_KEY) in
/// production.</para>
/// <para>With <see cref="Enabled"/> false, or no key, the API still starts: tags are then checked with the built-in
/// rules only.</para>
/// </remarks>
public sealed class AiSettings
{
    /// <summary>The section's name in appsettings.json.</summary>
    public const string SectionName = AppSettings.Ai.Section;

    /// <summary>Use the AI tag check.</summary>
    public bool Enabled { get; set; }

    /// <summary>The Anthropic API key (SECRET).</summary>
    public string? ApiKey { get; set; }

    /// <summary>The Claude model.</summary>
    [Required]
    public string Model { get; set; } = "claude-haiku-5-5";

    /// <summary>The API's address.</summary>
    [Required, Url]
    public string BaseUrl { get; set; } = "https://api.anthropic.com";

    /// <summary>How long to wait for an answer, in seconds.</summary>
    [Range(2, 60)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>True when the AI check can be used.</summary>
    public bool IsUsable => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
}
