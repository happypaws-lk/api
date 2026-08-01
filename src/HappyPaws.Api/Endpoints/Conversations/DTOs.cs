using FluentValidation;

namespace HappyPaws.Api.Endpoints.Conversations;

/// <summary>Data submitted when starting a new conversation.</summary>
/// <param name="ParticipantId">ID of the other user to start the conversation with.</param>
/// <param name="CaseId">Rescue case this conversation relates to. Mutually exclusive with ListingId. Optional.</param>
/// <param name="ListingId">Listing this conversation relates to. Mutually exclusive with CaseId. Optional.</param>
public sealed record CreateConversationRequest(Guid ParticipantId, Guid? CaseId = null, Guid? ListingId = null);

public sealed class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x.ParticipantId).NotEmpty();
        RuleFor(x => x).Must(x => (x.CaseId.HasValue && !x.ListingId.HasValue) || (!x.CaseId.HasValue && x.ListingId.HasValue) || (!x.CaseId.HasValue && !x.ListingId.HasValue))
            .WithMessage("Conversation can optionally link to a CaseId or a ListingId, but not both.");
    }
}

/// <summary>A conversation summary shown in the user's inbox.</summary>
/// <param name="Id">Unique identifier of the conversation.</param>
/// <param name="ParticipantId">ID of the other participant in the conversation.</param>
/// <param name="ParticipantName">Display name of the other participant.</param>
/// <param name="ParticipantAvatarKey">Storage key for the other participant's avatar. Null if they have no avatar.</param>
/// <param name="LastMessageSnippet">A truncated preview of the most recent message (up to 50 characters). Null if no messages have been sent.</param>
/// <param name="UnreadCount">Number of messages the authenticated user has not yet read.</param>
/// <param name="CreatedAt">UTC timestamp when the conversation was created.</param>
public sealed record ConversationResponse(
    Guid Id,
    Guid ParticipantId,
    string ParticipantName,
    string? ParticipantAvatarKey,
    string? LastMessageSnippet,
    int UnreadCount,
    DateTimeOffset CreatedAt);

/// <summary>A single message within a conversation.</summary>
/// <param name="Id">Unique identifier of the message.</param>
/// <param name="SenderId">ID of the user who sent the message.</param>
/// <param name="Content">The message text.</param>
/// <param name="SentAt">UTC timestamp when the message was sent.</param>
public sealed record MessageResponse(
    Guid Id,
    Guid SenderId,
    string Content,
    DateTimeOffset SentAt);
