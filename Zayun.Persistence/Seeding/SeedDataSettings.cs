namespace Zayun.Persistence.Seeding;

/// <summary>
/// The <c>SeedData</c> section: everything the seeder needs to create the first Admin and the starter content.
/// Keys: <c>Zayun.Domain.Constants.AppSettings.SeedData</c>.
/// </summary>
public sealed class SeedDataSettings
{
    /// <summary>The first Admin's email (also their sign-in name). Empty = skip admin seeding.</summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>The first Admin's password (SECRET). Empty = skip admin seeding.</summary>
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>The first Admin's first name.</summary>
    public string AdminFirstName { get; set; } = string.Empty;

    /// <summary>The first Admin's last name.</summary>
    public string AdminLastName { get; set; } = string.Empty;

    /// <summary>Seed the starter content on empty tables.</summary>
    public bool StarterDataEnabled { get; set; }

    /// <summary>The starter-data JSON file, relative to the app folder (or a full path).</summary>
    public string StarterDataPath { get; set; } = string.Empty;

    /// <summary>The folder with the starter pictures, relative to the app folder (or a full path).</summary>
    public string StarterImagesPath { get; set; } = string.Empty;

    /// <summary>True when every value needed to create the Admin is present.</summary>
    public bool CanSeedAdmin =>
        !string.IsNullOrWhiteSpace(AdminEmail) &&
        !string.IsNullOrWhiteSpace(AdminPassword) &&
        !string.IsNullOrWhiteSpace(AdminFirstName) &&
        !string.IsNullOrWhiteSpace(AdminLastName);
}
