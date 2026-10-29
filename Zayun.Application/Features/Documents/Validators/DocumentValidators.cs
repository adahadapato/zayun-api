using FluentValidation;
using Zayun.Application.Features.Documents.Commands;
using Zayun.Application.Features.Documents.Queries;
using Zayun.Domain.Entities;
using Zayun.Domain.ValueObjects;

namespace Zayun.Application.Features.Documents.Validators;

/// <summary>Checks <see cref="UploadDocumentCommand"/>. The file's real type is checked from its contents later.</summary>
public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    /// <summary>The rules.</summary>
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please give the document a title.")
                             .MaximumLength(Document.TitleMaxLength);
        RuleFor(x => x.Category).IsInEnum().WithMessage("Please choose a category.");
        RuleFor(x => x.Notes).MaximumLength(Document.NotesMaxLength);
        RuleFor(x => x.SizeBytes).GreaterThan(0).WithMessage("Please choose a file to upload.")
                                 .LessThanOrEqualTo(DocumentFile.MaxFileSizeBytes)
                                 .WithMessage("This file is too big. The maximum is 20 MB.");
    }
}

/// <summary>Checks <see cref="UpdateDocumentCommand"/>.</summary>
public sealed class UpdateDocumentCommandValidator : AbstractValidator<UpdateDocumentCommand>
{
    /// <summary>The rules.</summary>
    public UpdateDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().WithMessage("Please give the document a title.")
                             .MaximumLength(Document.TitleMaxLength);
        RuleFor(x => x.Category).IsInEnum().WithMessage("Please choose a category.");
        RuleFor(x => x.Notes).MaximumLength(Document.NotesMaxLength);
    }
}

/// <summary>Checks <see cref="ReplaceDocumentFileCommand"/>.</summary>
public sealed class ReplaceDocumentFileCommandValidator : AbstractValidator<ReplaceDocumentFileCommand>
{
    /// <summary>The rules.</summary>
    public ReplaceDocumentFileCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SizeBytes).GreaterThan(0).WithMessage("Please choose a file to upload.")
                                 .LessThanOrEqualTo(DocumentFile.MaxFileSizeBytes)
                                 .WithMessage("This file is too big. The maximum is 20 MB.");
    }
}

/// <summary>Checks <see cref="GetDocumentsQuery"/>.</summary>
public sealed class GetDocumentsQueryValidator : AbstractValidator<GetDocumentsQuery>
{
    /// <summary>The rules.</summary>
    public GetDocumentsQueryValidator() => RuleFor(x => x.Category).IsInEnum().When(x => x.Category is not null);
}
