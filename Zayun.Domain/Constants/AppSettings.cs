namespace Zayun.Domain.Constants;

/// <summary>
/// Every configuration key the API reads, in one place, so nothing types a key by hand.
/// </summary>
/// <remarks>
/// <para>Secrets (connection string, JWT key, passwords) are empty in <c>appsettings.json</c> and come from User
/// Secrets in development and from the CI pipeline in production.</para>
/// <para>Settings classes are checked at start-up (<c>ValidateOnStart</c>), so a missing value stops the app with a
/// clear message.</para>
/// </remarks>
public static class AppSettings
{
    // ==================== Application ====================

    /// <summary>Addresses of the API and the website (no trailing slash).</summary>
    public static class Application
    {
        public const string Section = "AppSettings";

        /// <summary>Where the API runs, used to build absolute links to uploaded files.</summary>
        public const string ApiBaseUrl = "AppSettings:ApiBaseUrl";

        /// <summary>Where the website runs, used in emails.</summary>
        public const string WebsiteBaseUrl = "AppSettings:WebsiteBaseUrl";
    }

    // ==================== OpenAPI / Swagger ====================

    /// <summary>The OpenAPI document and Swagger UI (switched off in production).</summary>
    public static class OpenApi
    {
        public const string Section = "OpenApi";

        public const string Enabled = "OpenApi:Enabled";

        public const string Title = "OpenApi:Title";

        public const string Version = "OpenApi:Version";

        public const string Description = "OpenApi:Description";

        public const string DocumentPath = "OpenApi:DocumentPath";

        public const string UiPath = "OpenApi:UiPath";

        public static class Contact
        {
            public const string Section = "OpenApi:Contact";

            public const string Name = "OpenApi:Contact:Name";

            public const string Email = "OpenApi:Contact:Email";

            public const string Url = "OpenApi:Contact:Url";
        }
    }

    // ==================== Connection strings ====================

    /// <summary>The SQL Server database.</summary>
    public static class ConnectionStrings
    {
        public const string Section = "ConnectionStrings";

        public const string DefaultName = "DefaultConnection";

        public const string Default = "ConnectionStrings:DefaultConnection";
    }

    // ==================== Seed data ====================

    /// <summary>The first admin account and the starter content.</summary>
    public static class SeedData
    {
        public const string Section = "SeedData";

        public const string AdminEmail = "SeedData:AdminEmail";

        public const string AdminPassword = "SeedData:AdminPassword";

        public const string AdminFirstName = "SeedData:AdminFirstName";

        public const string AdminLastName = "SeedData:AdminLastName";

        /// <summary>Load the starter content (each block only while its table is empty).</summary>
        public const string StarterDataEnabled = "SeedData:StarterDataEnabled";

        /// <summary>The starter JSON file, relative to the app's folder.</summary>
        public const string StarterDataPath = "SeedData:StarterDataPath";

        /// <summary>The folder holding the starter images, relative to the app's folder.</summary>
        public const string StarterImagesPath = "SeedData:StarterImagesPath";
    }

    // ==================== Identity ====================

    /// <summary>Password and lockout rules for admin accounts.</summary>
    public static class Identity
    {
        public const string Section = "Identity";

        public static class Password
        {
            public const string Section = "Identity:Password";

            public const string RequiredLength = "Identity:Password:RequiredLength";

            public const string RequireDigit = "Identity:Password:RequireDigit";

            public const string RequireLowercase = "Identity:Password:RequireLowercase";

            public const string RequireUppercase = "Identity:Password:RequireUppercase";

            public const string RequireNonAlphanumeric = "Identity:Password:RequireNonAlphanumeric";

            public const string RequiredUniqueChars = "Identity:Password:RequiredUniqueChars";
        }

        public static class Lockout
        {
            public const string Section = "Identity:Lockout";

            public const string MaxFailedAccessAttempts = "Identity:Lockout:MaxFailedAccessAttempts";

            public const string LockoutMinutes = "Identity:Lockout:LockoutMinutes";

            public const string AllowedForNewUsers = "Identity:Lockout:AllowedForNewUsers";
        }
    }

    // ==================== JWT ====================

    /// <summary>Access and refresh tokens.</summary>
    public static class Jwt
    {
        public const string Section = "Jwt";

        public const string Key = "Jwt:Key";

        public const string Issuer = "Jwt:Issuer";

        public const string Audience = "Jwt:Audience";

        public const string ExpiryMinutes = "Jwt:ExpiryMinutes";

        public const string RefreshTokenDays = "Jwt:RefreshTokenDays";
    }

    // ==================== CORS (the website) ====================

    /// <summary>Which websites may call the API from a browser.</summary>
    public static class Cors
    {
        public const string Section = "Cors";

        public const string AllowedOrigins = "Cors:AllowedOrigins";
    }

    // ==================== Storage ====================

    /// <summary>Where uploaded images are kept.</summary>
    public static class Storage
    {
        public const string Section = "Storage";

        public static class Local
        {
            public const string Section = "Storage:Local";

            /// <summary>Folder for public files, relative to the web root (served at <c>/uploads</c>).</summary>
            public const string PublicRootPath = "Storage:Local:PublicRootPath";

            /// <summary>Folder for PRIVATE documents, relative to the content root and outside the web root.</summary>
            public const string PrivateRootPath = "Storage:Local:PrivateRootPath";

            /// <summary>The URL path signed download links use, e.g. "/private-files".</summary>
            public const string PrivateDownloadPath = "Storage:Local:PrivateDownloadPath";

            /// <summary>Key that signs download links (SECRET, 32+ characters, not the JWT key).</summary>
            public const string SigningKey = "Storage:Local:SigningKey";

            /// <summary>How long a download link works, in minutes.</summary>
            public const string PrivateLinkMinutes = "Storage:Local:PrivateLinkMinutes";
        }
    }

    // ==================== Images ====================

    /// <summary>Upload limits and the WebP pipeline.</summary>
    public static class Images
    {
        public const string Section = "Images";

        /// <summary>Largest upload accepted, in bytes.</summary>
        public const string MaxUploadBytes = "Images:MaxUploadBytes";

        /// <summary>Longest side of the full-size picture after resizing.</summary>
        public const string MaxSide = "Images:MaxSide";

        /// <summary>Longest side of the thumbnail.</summary>
        public const string ThumbnailMaxSide = "Images:ThumbnailMaxSide";

        /// <summary>WebP quality, 1 to 100.</summary>
        public const string WebpQuality = "Images:WebpQuality";
    }

    // ==================== Email ====================

    /// <summary>The SMTP server and the inbox Contact messages are sent to.</summary>
    public static class Email
    {
        public const string Section = "Email";

        /// <summary>Switch email off entirely (messages are still saved in the inbox).</summary>
        public const string Enabled = "Email:Enabled";

        public const string FromName = "Email:FromName";

        public const string FromAddress = "Email:FromAddress";

        /// <summary>Where new Contact messages are sent.</summary>
        public const string OwnerInbox = "Email:OwnerInbox";

        public const string Host = "Email:Host";

        public const string Port = "Email:Port";

        public const string Security = "Email:Security";

        public const string Username = "Email:Username";

        public const string Password = "Email:Password";
    }

    // ==================== Hosting ====================

    public static class Hosting
    {
        public const string AllowedHosts = "AllowedHosts";
    }

    // ==================== Logging ====================

    public static class Serilog
    {
        public const string Section = "Serilog";

        public const string MinimumLevelDefault = "Serilog:MinimumLevel:Default";
    }
}
