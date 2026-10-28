using MediatR;
using Zayun.Application.Features.Contact.Commands;
using Zayun.Application.Features.Contact.Dtos;
using Zayun.Application.Features.Contact.Queries;
using Zayun.Domain.Constants;
using Zayun.Domain.Enums;
using Zayun.Presentation.Extensions;
using Zayun.Presentation.Settings.RateLimiting;

namespace Zayun.Presentation.Endpoints.Contact;

/// <summary><c>/api/contact</c> (the website's form) and <c>/api/admin/contact-messages</c> (the inbox).</summary>
public static class ContactEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapContactEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapPost("/contact/messages", async (SendContactMessageCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.ContactPolicy)
            .WithTags("Contact")
            .WithName("SendContactMessage")
            .WithSummary("Send a message from the Contact form: saved in the inbox, emailed to the owner, confirmation to the sender")
            .WithDescription($"Topic = Project | Research | Opportunity | Speaking | Other. Leave 'website' empty (it's a " +
                             $"spam trap). At most {RateLimitingSetup.ContactPermitLimit} messages every " +
                             $"{RateLimitingSetup.ContactWindow.TotalMinutes:0} minutes from one connection.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        var inbox = api.MapGroup("/admin/contact-messages")
                       .WithTags("Admin: Inbox")
                       .RequireAuthorization(Policies.RequireAdmin);

        inbox.MapGet("/", async ([AsParameters] GetContactMessagesQuery query, ISender sender, CancellationToken ct) =>
                (await sender.Send(query, ct)).ToHttp())
            .WithName("GetContactMessages")
            .WithSummary("The inbox, newest first; optionally ?status=New|Replied|Closed; with counts per status")
            .Produces<ContactInboxDto>()
            .ProducesValidationProblem();

        inbox.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetContactMessageQuery(id), ct)).ToHttp())
            .WithName("GetContactMessage")
            .WithSummary("One message")
            .Produces<ContactMessageDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        inbox.MapPut("/{id:guid}/status", async (Guid id, ContactStatusRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateContactMessageStatusCommand(id, body.Status, body.Note), ct)).ToHttp())
            .WithName("UpdateContactMessageStatus")
            .WithSummary("Mark a message Replied, Closed or back to New, with an optional private note")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        inbox.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteContactMessageCommand(id), ct)).ToHttp())
            .WithName("DeleteContactMessage")
            .WithSummary("Delete a message")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    /// <summary>Body of the status change.</summary>
    public sealed record ContactStatusRequest(ContactMessageStatus Status, string? Note);
}
