using Zayun.Domain.Constants;

namespace Zayun.Presentation.Settings.Cors;

/// <summary>
/// The <c>Cors</c> section: which websites may call the API from a browser. Keys: <see cref="AppSettings.Cors"/>.
/// </summary>
/// <remarks>
/// <para>Production lists the Zayun website (<c>https://zayun.net</c>, with and without www); development
/// lists the local Vite address (<c>http://localhost:5173</c>).</para>
/// <para>Checked when the app starts (see <see cref="CorsSetup"/>): a malformed address stops the app instead of
/// silently blocking the website.</para>
/// </remarks>
public sealed class CorsSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = AppSettings.Cors.Section;

    /// <summary>Full addresses, e.g. "https://zayun.net": scheme + host (+ port), no path, no trailing
    /// slash. Empty = no website may call the API from a browser.</summary>
    public string[] AllowedOrigins { get; set; } = [];
}
