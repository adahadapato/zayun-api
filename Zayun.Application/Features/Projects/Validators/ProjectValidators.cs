using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.Projects.Commands;
using Zayun.Application.Features.Projects.Queries;
using System.Linq.Expressions;
using Zayun.Domain.Entities;
using Zayun.Domain.Enums;
using Zayun.Domain.Shared.Text;

namespace Zayun.Application.Features.Projects.Validators;

/// <summary>The fields Create and Update share, so both check exactly the same rules.</summary>
internal static class ProjectRules
{
    /// <summary>Adds the shared rules to a validator.</summary>
    /// <typeparam name="T">The command.</typeparam>
    public static void Apply<T>(AbstractValidator<T> v,
                                Expression<Func<T, string>> title,
                                Expression<Func<T, string?>> slug,
                                Expression<Func<T, string>> subtitle,
                                Expression<Func<T, string>> description,
                                Expression<Func<T, ProjectCategory>> category,
                                Expression<Func<T, string>> label,
                                Expression<Func<T, string?>> visualLabel,
                                Expression<Func<T, string?>> githubUrl,
                                Expression<Func<T, string?>> liveUrl,
                                Expression<Func<T, IReadOnlyList<string>?>> tags)
    {
        v.RuleFor(title).NotEmpty().WithMessage("Please give the project a title.")
         .MaximumLength(Project.TitleMaxLength).WithMessage($"Keep the title under {Project.TitleMaxLength} characters.");
        v.RuleFor(slug).MaximumLength(Slugs.MaxLength)
         .WithMessage($"Keep the web address under {Slugs.MaxLength} characters.");
        v.RuleFor(subtitle).NotEmpty().WithMessage("Please add a subtitle.")
         .MaximumLength(Project.SubtitleMaxLength).WithMessage($"Keep the subtitle under {Project.SubtitleMaxLength} characters.");
        v.RuleFor(description).NotEmpty().WithMessage("Please describe the project.")
         .MaximumLength(Project.DescriptionMaxLength)
         .WithMessage($"Keep the description under {Project.DescriptionMaxLength:N0} characters.");
        v.RuleFor(category).IsInEnum().WithMessage("Please choose Featured or Engineering.");
        v.RuleFor(label).NotEmpty().WithMessage("Please add the card's label, e.g. \"AI Innovation\".")
         .MaximumLength(Project.LabelMaxLength).WithMessage($"Keep the label under {Project.LabelMaxLength} characters.");
        v.RuleFor(visualLabel).MaximumLength(Project.VisualLabelMaxLength)
         .WithMessage($"Keep the picture label under {Project.VisualLabelMaxLength} characters.");
        v.RuleFor(githubUrl).OptionalWebUrl(Project.UrlMaxLength);
        v.RuleFor(liveUrl).OptionalWebUrl(Project.UrlMaxLength);
        v.RuleFor(tags).Labels(Project.TagMaxLength, Project.MaxTags, "tags");
    }
}

/// <summary>Checks <see cref="CreateProjectCommand"/>.</summary>
public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    /// <summary>The rules.</summary>
    public CreateProjectCommandValidator() =>
        ProjectRules.Apply(this, x => x.Title, x => x.Slug, x => x.Subtitle, x => x.Description, x => x.Category,
                           x => x.Label, x => x.VisualLabel, x => x.GithubUrl, x => x.LiveUrl, x => x.Tags);
}

/// <summary>Checks <see cref="UpdateProjectCommand"/>.</summary>
public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    /// <summary>The rules.</summary>
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        ProjectRules.Apply(this, x => x.Title, x => x.Slug, x => x.Subtitle, x => x.Description, x => x.Category,
                           x => x.Label, x => x.VisualLabel, x => x.GithubUrl, x => x.LiveUrl, x => x.Tags);
    }
}

/// <summary>Checks <see cref="ReorderProjectsCommand"/>.</summary>
public sealed class ReorderProjectsCommandValidator : AbstractValidator<ReorderProjectsCommand>
{
    /// <summary>The rules.</summary>
    public ReorderProjectsCommandValidator() => RuleFor(x => x.Ids).ReorderList();
}

/// <summary>Checks <see cref="ReplaceProjectImageFileCommand"/>.</summary>
public sealed class ReplaceProjectImageFileCommandValidator : AbstractValidator<ReplaceProjectImageFileCommand>
{
    /// <summary>The rules.</summary>
    public ReplaceProjectImageFileCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
        RuleFor(x => x.SizeBytes).UploadSize();
    }
}

/// <summary>Checks <see cref="AddProjectImageCommand"/>.</summary>
public sealed class AddProjectImageCommandValidator : AbstractValidator<AddProjectImageCommand>
{
    /// <summary>The rules.</summary>
    public AddProjectImageCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.SizeBytes).UploadSize();
        RuleFor(x => x.AltText).NotEmpty().WithMessage("Please describe the picture for screen readers.")
                               .MaximumLength(ProjectImage.AltTextMaxLength)
                               .WithMessage($"Keep the description under {ProjectImage.AltTextMaxLength} characters.");
        RuleFor(x => x.Label).MaximumLength(ProjectImage.LabelMaxLength)
                             .WithMessage($"Keep the caption under {ProjectImage.LabelMaxLength} characters.");
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Please choose Primary, Secondary or Gallery.");
    }
}

/// <summary>Checks <see cref="UpdateProjectImageCommand"/>.</summary>
public sealed class UpdateProjectImageCommandValidator : AbstractValidator<UpdateProjectImageCommand>
{
    /// <summary>The rules.</summary>
    public UpdateProjectImageCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
        RuleFor(x => x.AltText).NotEmpty().WithMessage("Please describe the picture for screen readers.")
                               .MaximumLength(ProjectImage.AltTextMaxLength)
                               .WithMessage($"Keep the description under {ProjectImage.AltTextMaxLength} characters.");
        RuleFor(x => x.Label).MaximumLength(ProjectImage.LabelMaxLength)
                             .WithMessage($"Keep the caption under {ProjectImage.LabelMaxLength} characters.");
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Please choose Primary, Secondary or Gallery.");
    }
}

/// <summary>Checks <see cref="ReorderProjectImagesCommand"/>.</summary>
public sealed class ReorderProjectImagesCommandValidator : AbstractValidator<ReorderProjectImagesCommand>
{
    /// <summary>The rules.</summary>
    public ReorderProjectImagesCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ImageIds).ReorderList();
    }
}

/// <summary>Checks <see cref="GetProjectBySlugQuery"/>.</summary>
public sealed class GetProjectBySlugQueryValidator : AbstractValidator<GetProjectBySlugQuery>
{
    /// <summary>The rules.</summary>
    public GetProjectBySlugQueryValidator() =>
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(Slugs.MaxLength);
}
