using FluentValidation;

namespace HappyPaws.Api.Endpoints.Conversations;

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

public sealed record ConversationResponse(
    Guid Id,
    Guid ParticipantId,
    string ParticipantName,
    string? ParticipantAvatarKey,
    string? LastMessageSnippet,
    int UnreadCount,
    DateTimeOffset CreatedAt);

public sealed record MessageResponse(
    Guid Id,
    Guid SenderId,
    string Content,
    DateTimeOffset SentAt);
