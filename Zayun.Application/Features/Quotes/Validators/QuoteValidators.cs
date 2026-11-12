using FluentValidation;
using Zayun.Application.Common.Validation;
using Zayun.Application.Features.Quotes.Commands;
using Zayun.Application.Features.Quotes.Queries;
using Zayun.Domain.Entities;
using Zayun.Domain.ValueObjects;

namespace Zayun.Application.Features.Quotes.Validators;

/// <summary>
/// The rules for the fields Admin → Quotes types in, written once for both <see cref="CreateQuoteCommand"/> and
/// <see cref="UpdateQuoteCommand"/>. Abstract, so it's never registered on its own.
/// </summary>
/// <typeparam name="T">The command.</typeparam>
public abstract class QuoteFieldsValidator<T> : AbstractValidator<T> where T : IQuoteFields
{
    /// <summary>The rules.</summary>
    protected QuoteFieldsValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Please choose quote, proverb or own saying.");

        RuleFor(x => x.Text).NotEmpty().WithMessage("Please enter the words.")
                            .MaximumLength(Quote.TextMaxLength)
                            .WithMessage($"Keep the words under {Quote.TextMaxLength} characters.");

        RuleFor(x => x.Author).NotEmpty().WithMessage("Please enter who said it, e.g. Ada Lovelace or Annang proverb.")
                              .MaximumLength(Quote.AuthorMaxLength)
                              .WithMessage($"Keep who said it under {Quote.AuthorMaxLength} characters.");

        RuleFor(x => x.Source).MaximumLength(Quote.SourceMaxLength)
                              .WithMessage($"Keep the source under {Quote.SourceMaxLength} characters.");
        RuleFor(x => x.Language).MaximumLength(Quote.LanguageMaxLength)
                                .WithMessage($"Keep the language under {Quote.LanguageMaxLength} characters.");
        RuleFor(x => x.Translation).MaximumLength(Quote.TranslationMaxLength)
                                   .WithMessage($"Keep the translation under {Quote.TranslationMaxLength} characters.");
        RuleFor(x => x.Series).MaximumLength(Quote.SeriesMaxLength)
                              .WithMessage($"Keep the series name under {Quote.SeriesMaxLength} characters.");
        RuleFor(x => x.Story).MaximumLength(Quote.StoryMaxLength)
                             .WithMessage($"Keep the story under {Quote.StoryMaxLength} characters.");

        RuleFor(x => x.Language)
            .NotEmpty().When(x => !string.IsNullOrWhiteSpace(x.Translation))
            .WithMessage("A translation needs the original language, e.g. Annang.");

        RuleFor(x => x.PinnedDay)
            .Must((x, _) => (x.PinnedMonth is null) == (x.PinnedDay is null))
            .WithMessage("To pin a quote, choose both the month and the day (or neither).");

        RuleFor(x => x.PinnedMonth)
            .InclusiveBetween(1, 12).When(x => x.PinnedMonth is not null)
            .WithMessage("Please choose a month from 1 to 12.");

        RuleFor(x => x.PinnedDay)
            .Must((x, day) => day >= 1 && day <= DateTime.DaysInMonth(2024, x.PinnedMonth!.Value))
            .When(x => x.PinnedMonth is >= 1 and <= 12 && x.PinnedDay is not null)
            .WithMessage("That day doesn't exist in that month.");
    }
}

/// <summary>Checks <see cref="CreateQuoteCommand"/>.</summary>
public sealed class CreateQuoteCommandValidator : QuoteFieldsValidator<CreateQuoteCommand>;

/// <summary>Checks <see cref="UpdateQuoteCommand"/>.</summary>
public sealed class UpdateQuoteCommandValidator : QuoteFieldsValidator<UpdateQuoteCommand>
{
    /// <summary>The rules (the shared ones, plus the Id).</summary>
    public UpdateQuoteCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

/// <summary>Checks <see cref="ReorderQuotesCommand"/>.</summary>
public sealed class ReorderQuotesCommandValidator : AbstractValidator<ReorderQuotesCommand>
{
    /// <summary>The rules.</summary>
    public ReorderQuotesCommandValidator() => RuleFor(x => x.Ids).ReorderList();
}

/// <summary>Checks <see cref="PreviewQuotesQuery"/>.</summary>
public sealed class PreviewQuotesQueryValidator : AbstractValidator<PreviewQuotesQuery>
{
    /// <summary>The rules.</summary>
    public PreviewQuotesQueryValidator() =>
        RuleFor(x => x.Days).InclusiveBetween(1, PreviewQuotesQuery.MaxDays)
                            .WithMessage($"Please choose from 1 to {PreviewQuotesQuery.MaxDays} days.");
}

/// <summary>Checks <see cref="SetQuotePictureCommand"/>.</summary>
public sealed class SetQuotePictureCommandValidator : AbstractValidator<SetQuotePictureCommand>
{
    /// <summary>The rules.</summary>
    public SetQuotePictureCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SizeBytes).UploadSize();
        RuleFor(x => x.AltText).NotEmpty().WithMessage("Please describe the picture for screen readers, e.g. the story it tells.")
                               .MaximumLength(Quote.PictureAltMaxLength)
                               .WithMessage($"Keep the description under {Quote.PictureAltMaxLength} characters.");
    }
}

/// <summary>Checks <see cref="SetQuoteAnimationCommand"/> (the file itself is checked when it's read).</summary>
public sealed class SetQuoteAnimationCommandValidator : AbstractValidator<SetQuoteAnimationCommand>
{
    /// <summary>The rules.</summary>
    public SetQuoteAnimationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SizeBytes).GreaterThan(0).WithMessage("Please choose an MP4 animation to upload.")
                                 .LessThanOrEqualTo(AnimationFile.MaxFileSizeBytes)
                                 .WithMessage($"Please keep the animation under {AnimationFile.MaxFileSizeBytes / (1024 * 1024)} MB.");
    }
}
