using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zayun.Domain.Entities;
using Zayun.Domain.Interfaces;
using Zayun.Domain.Shared.Text;
using Zayun.Domain.ValueObjects;

namespace Zayun.Persistence.Seeding;

/// <summary>
/// Fills an empty database with the website's starter content from <c>starter-data.json</c>: projects (with
/// pictures), publications, presentations (with pictures), experience areas, skill groups and core expertise.
/// </summary>
/// <remarks>
/// <para><b>Only when <c>SeedData:StarterDataEnabled</c> is true.</b></para>
/// <para><b>Each block only while its table is empty</b>, counting deleted rows, so anything the admin changes or
/// deletes is never brought back by a restart.</para>
/// <para><b>Pictures</b> go through the same <see cref="IImageProcessor"/> as admin uploads, so they're resized and
/// stored exactly like uploaded ones.</para>
/// <para>Everything is published, in the order of the file.</para>
/// </remarks>
internal static class StarterDataSeeder
{
    /// <summary>Reads enum names ("Featured"), comments and any letter case.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Seeds the blocks whose tables are still empty.</summary>
    /// <param name="context">The database.</param>
    /// <param name="storage">Where pictures are saved.</param>
    /// <param name="images">The picture pipeline.</param>
    /// <param name="settings">The <c>SeedData</c> section.</param>
    /// <param name="logger">Start-up log.</param>
    /// <param name="cancellationToken">Stops seeding.</param>
    /// <exception cref="InvalidOperationException">A value in starter-data.json breaks a rule, or a picture is missing
    /// or unreadable (the message names the entry).</exception>
    public static async Task SeedAsync(ZayunDbContext context, IFileStorage storage, IImageProcessor images,
                                       SeedDataSettings settings, ILogger logger,
                                       CancellationToken cancellationToken = default)
    {
        if (!settings.StarterDataEnabled) return;

        var data = await ReadAsync(settings.StarterDataPath, logger, cancellationToken);
        if (data is null) return;

        var imageFolder = FullPath(settings.StarterImagesPath);
        var pictures = new PictureSource(storage, images, imageFolder);

        await SeedProjectsAsync(context, data, pictures, logger, cancellationToken);
        await SeedPublicationsAsync(context, data, logger, cancellationToken);
        await SeedPresentationsAsync(context, data, pictures, logger, cancellationToken);
        await SeedExperienceAsync(context, data, logger, cancellationToken);
        await SeedSkillGroupsAsync(context, data, logger, cancellationToken);
        await SeedCoreExpertiseAsync(context, data, logger, cancellationToken);
    }

    // ───────────────────────────── Blocks ─────────────────────────────

    private static async Task SeedProjectsAsync(ZayunDbContext context, StarterData data, PictureSource pictures,
                                                ILogger logger, CancellationToken ct)
    {
        if (data.Projects.Count == 0 || await context.Projects.IgnoreQueryFilters().AnyAsync(ct)) return;

        for (var i = 0; i < data.Projects.Count; i++)
        {
            var p = data.Projects[i];
            var what = $"project '{p.Title}'";

            var project = Check(what, () => Project.Create(
                new ProjectDetails(p.Title, p.Subtitle, p.Description, p.Category, p.Label, p.VisualLabel,
                                   p.GithubUrl, p.LiveUrl, p.IsPrivateRepo, p.Tags),
                string.IsNullOrWhiteSpace(p.Slug) ? Slugs.From(p.Title) : p.Slug.Trim(),
                displayOrder: i,
                isPublished: true));

            foreach (var picture in p.Images)
            {
                var imageId = Guid.CreateVersion7();
                var file = await pictures.StoreAsync(picture.File, $"projects/{project.Id:N}", imageId, what, ct);
                var added = Check(what, () => project.AddImage(imageId, file, picture.AltText, picture.Label,
                                                               picture.Kind));
                if (added.IsFailure)
                    throw new InvalidOperationException($"starter-data.json, {what}: {added.Error.Message}");
            }

            context.Projects.Add(project);
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} projects", data.Projects.Count);
    }

    private static async Task SeedPublicationsAsync(ZayunDbContext context, StarterData data, ILogger logger,
                                                    CancellationToken ct)
    {
        if (data.Publications.Count == 0 || await context.Publications.IgnoreQueryFilters().AnyAsync(ct)) return;

        for (var i = 0; i < data.Publications.Count; i++)
        {
            var p = data.Publications[i];
            context.Publications.Add(Check($"publication '{p.Title}'", () => Publication.Create(
                new PublicationDetails(p.Title, p.Authors, p.Venue, p.Year, p.Type, p.Award,
                                       p.Links.Select(l => new PublicationLinkDetails(l.Label, l.Url, l.IsPrimary))
                                              .ToList()),
                displayOrder: i,
                isPublished: true)));
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} publications", data.Publications.Count);
    }

    private static async Task SeedPresentationsAsync(ZayunDbContext context, StarterData data, PictureSource pictures,
                                                     ILogger logger, CancellationToken ct)
    {
        if (data.Presentations.Count == 0 || await context.Presentations.IgnoreQueryFilters().AnyAsync(ct)) return;

        for (var i = 0; i < data.Presentations.Count; i++)
        {
            var p = data.Presentations[i];
            var what = $"presentation '{p.Title}'";

            var presentation = Check(what, () => Presentation.Create(
                new PresentationDetails(p.Title, p.EventName, p.Year, p.Type, ImageAlt: null),
                displayOrder: i,
                isPublished: true));

            if (!string.IsNullOrWhiteSpace(p.Image))
            {
                var file = await pictures.StoreAsync(p.Image, $"presentations/{presentation.Id:N}",
                                                     Guid.CreateVersion7(), what, ct);
                Check(what, () => presentation.SetImage(file, p.ImageAlt ?? p.Title));
            }

            context.Presentations.Add(presentation);
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} presentations", data.Presentations.Count);
    }

    private static async Task SeedExperienceAsync(ZayunDbContext context, StarterData data, ILogger logger,
                                                  CancellationToken ct)
    {
        if (data.ExperienceAreas.Count == 0 || await context.ExperienceAreas.IgnoreQueryFilters().AnyAsync(ct)) return;

        for (var i = 0; i < data.ExperienceAreas.Count; i++)
        {
            var e = data.ExperienceAreas[i];
            context.ExperienceAreas.Add(Check($"experience area '{e.Title}'",
                () => ExperienceArea.Create(e.Title, e.Description, e.Skills, displayOrder: i, isPublished: true)));
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} experience areas", data.ExperienceAreas.Count);
    }

    private static async Task SeedSkillGroupsAsync(ZayunDbContext context, StarterData data, ILogger logger,
                                                   CancellationToken ct)
    {
        if (data.SkillGroups.Count == 0 || await context.SkillGroups.IgnoreQueryFilters().AnyAsync(ct)) return;

        for (var i = 0; i < data.SkillGroups.Count; i++)
        {
            var g = data.SkillGroups[i];
            context.SkillGroups.Add(Check($"skill group '{g.Title}'",
                () => SkillGroup.Create(g.Title, g.Description, g.Skills, displayOrder: i, isPublished: true)));
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} skill groups", data.SkillGroups.Count);
    }

    private static async Task SeedCoreExpertiseAsync(ZayunDbContext context, StarterData data, ILogger logger,
                                                     CancellationToken ct)
    {
        if (data.CoreExpertise.Count == 0 || await context.CoreExpertise.IgnoreQueryFilters().AnyAsync(ct)) return;

        var names = data.CoreExpertise
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Select(n => n.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i];
            context.CoreExpertise.Add(Check($"core expertise '{name}'",
                () => CoreExpertise.Create(name, displayOrder: i, isPublished: true)));
        }

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} core expertise items", names.Count);
    }

    // ───────────────────────────── Helpers ─────────────────────────────

    /// <summary>Runs a Domain call and turns a rule it throws into a message naming the JSON entry.</summary>
    private static T Check<T>(string what, Func<T> create)
    {
        try
        {
            return create();
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"starter-data.json, {what}: {ex.Message}", ex);
        }
    }

    /// <summary>Finds and reads the JSON file; null (with a warning) if it's missing.</summary>
    private static async Task<StarterData?> ReadAsync(string path, ILogger logger, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            logger.LogWarning("Starter data skipped: SeedData:StarterDataPath is empty.");
            return null;
        }

        var fullPath = FullPath(path);
        if (!File.Exists(fullPath))
        {
            logger.LogWarning("Starter data skipped: {Path} not found.", fullPath);
            return null;
        }

        await using var stream = File.OpenRead(fullPath);
        return await JsonSerializer.DeserializeAsync<StarterData>(stream, JsonOptions, cancellationToken)
               ?? throw new InvalidOperationException($"{fullPath} is empty.");
    }

    /// <summary>A path relative to the app's folder, or a full path as it is.</summary>
    private static string FullPath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);

    /// <summary>Reads a starter picture, runs it through the pipeline and stores it.</summary>
    private sealed class PictureSource(IFileStorage storage, IImageProcessor images, string folder)
    {
        /// <summary>Processes and stores one picture.</summary>
        /// <param name="relativeFile">The file, relative to the images folder (forward slashes).</param>
        /// <param name="storageFolder">Where to store it, e.g. "projects/{id}".</param>
        /// <param name="imageId">The picture's Id (its file name).</param>
        /// <param name="what">The JSON entry, for error messages.</param>
        /// <param name="ct">Stops the work.</param>
        /// <returns>The stored file's details.</returns>
        /// <exception cref="InvalidOperationException">Missing or unreadable picture.</exception>
        public async Task<ImageFile> StoreAsync(string relativeFile, string storageFolder, Guid imageId, string what,
                                                CancellationToken ct)
        {
            var source = Path.Combine(folder, relativeFile.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source))
                throw new InvalidOperationException($"starter-data.json, {what}: picture '{relativeFile}' not found at {source}.");

            await using var upload = File.OpenRead(source);
            var processed = await images.ProcessAsync(upload, ct);
            if (processed.IsFailure)
                throw new InvalidOperationException(
                    $"starter-data.json, {what}: picture '{relativeFile}' was refused: {processed.Error.Message}");

            await using var image = processed.Value;
            return await storage.SaveImageAsync(image, storageFolder, imageId, ct);
        }
    }
}
