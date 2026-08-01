using System.Security.Claims;
using FluentValidation;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Filters;
using HappyPaws.Api.Hubs;
using HappyPaws.Core.Common;
using HappyPaws.Core.Entities;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Api.Endpoints.Conversations;

public class ConversationsEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization()
            .WithTags("Conversations");

        group.MapGet("/", GetConversationsAsync)
            .WithName("GetConversations")
            .WithSummary("List conversations for the authenticated user")
            .WithDescription("Returns a paginated list of the authenticated user's conversations, including the other participant's name, unread count, and a 50-character snippet of the last message.")
            .Produces<PagedResult<ConversationResponse>>();

        group.MapPost("/", CreateConversationAsync)
            .AddEndpointFilter<ValidationFilter<CreateConversationRequest>>()
            .RequireAuthorization("Verified")
            .WithName("CreateConversation")
            .WithSummary("Start a new conversation")
            .WithDescription("Starts a new conversation with another user. Optionally links to a rescue case or a listing (not both). Returns a conflict if a conversation between the same two users for the same context already exists.")
            .Produces<ConversationResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}/messages", GetMessagesAsync)
            .WithName("GetMessages")
            .WithSummary("Get messages in a conversation")
            .WithDescription("Returns messages in a conversation in reverse chronological order. Only participants can fetch messages.")
            .Produces<PagedResult<MessageResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/read", MarkAsReadAsync)
            .RequireAuthorization("Verified")
            .WithName("MarkConversationAsRead")
            .WithSummary("Mark all messages in a conversation as read")
            .WithDescription("Marks all messages as read and broadcasts a read receipt to the other participant via SignalR.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<PagedResult<ConversationResponse>>> GetConversationsAsync(
        HappyPawsDbContext dbContext,
        ClaimsPrincipal user,
        [AsParameters] PaginationQuery pagination,
        CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var totalCount = await dbContext.ConversationParticipants
            .AsNoTracking()
            .CountAsync(cp => cp.UserId == userId, ct);

        var items = await dbContext.ConversationParticipants
            .AsNoTracking()
            .Where(cp => cp.UserId == userId)
            .OrderByDescending(cp => cp.Conversation.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(cp => new
            {
                cp.ConversationId,
                cp.Conversation.CreatedAt,
                cp.LastReadAt,
                OtherUserId = cp.Conversation.Participants
                    .Where(p => p.UserId != userId)
                    .Select(p => p.UserId)
                    .FirstOrDefault(),
                OtherUserName = cp.Conversation.Participants
                    .Where(p => p.UserId != userId)
                    .Select(p => p.User.Name)
                    .FirstOrDefault(),
                OtherUserAvatarKey = cp.Conversation.Participants
                    .Where(p => p.UserId != userId)
                    .Select(p => p.User.AvatarKey)
                    .FirstOrDefault(),
                LastMessageContent = dbContext.Messages
                    .Where(m => m.ConversationId == cp.ConversationId)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.Content)
                    .FirstOrDefault(),
                UnreadCount = dbContext.Messages
                    .Count(m => m.ConversationId == cp.ConversationId &&
                                (cp.LastReadAt == null || m.SentAt > cp.LastReadAt))
            })
            .ToListAsync(ct);

        var responses = items.Select(item => new ConversationResponse(
            item.ConversationId,
            item.OtherUserId,
            item.OtherUserName ?? "Unknown",
            item.OtherUserAvatarKey,
            item.LastMessageContent?.Length > 50 ? item.LastMessageContent[..47] + "..." : item.LastMessageContent,
            item.UnreadCount,
            item.CreatedAt
        )).ToList();

        return TypedResults.Ok(new PagedResult<ConversationResponse>(responses, totalCount, pagination.Page, pagination.PageSize));
    }

    private static async Task<Results<Created<ConversationResponse>, Conflict<ProblemDetails>>> CreateConversationAsync(
        CreateConversationRequest request,
        HappyPawsDbContext dbContext,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var existingConversation = await dbContext.Conversations
            .AsNoTracking()
            .Where(c => c.ListingId == request.ListingId && c.CaseId == request.CaseId)
            .Where(c => c.Participants.Any(p => p.UserId == userId) && c.Participants.Any(p => p.UserId == request.ParticipantId))
            .FirstOrDefaultAsync(ct);

        if (existingConversation is not null)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Conversation already exists.",
                Detail = "A conversation between these users for this context already exists."
            });
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            CaseId = request.CaseId,
            ListingId = request.ListingId,
            CreatedAt = DateTimeOffset.UtcNow,
            Participants =
            [
                new ConversationParticipant { Id = Guid.NewGuid(), UserId = userId, JoinedAt = DateTimeOffset.UtcNow },
                new ConversationParticipant { Id = Guid.NewGuid(), UserId = request.ParticipantId, JoinedAt = DateTimeOffset.UtcNow }
            ]
        };

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(ct);

        var otherUser = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ParticipantId, ct);

        var response = new ConversationResponse(
            conversation.Id,
            request.ParticipantId,
            otherUser?.Name ?? "Unknown",
            otherUser?.AvatarKey,
            null,
            0,
            conversation.CreatedAt
        );

        return TypedResults.Created($"/api/v1/conversations/{conversation.Id}", response);
    }

    private static async Task<Results<Ok<PagedResult<MessageResponse>>, ForbidHttpResult, NotFound>> GetMessagesAsync(
        Guid id,
        HappyPawsDbContext dbContext,
        ClaimsPrincipal user,
        [AsParameters] PaginationQuery pagination,
        CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var isParticipant = await dbContext.ConversationParticipants
            .AsNoTracking()
            .AnyAsync(cp => cp.ConversationId == id && cp.UserId == userId, ct);

        if (!isParticipant)
            return TypedResults.Forbid();

        var query = dbContext.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == id)
            .OrderByDescending(m => m.SentAt);

        var totalCount = await query.CountAsync(ct);

        var messages = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(m => new MessageResponse(m.Id, m.SenderId, m.Content, m.SentAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<MessageResponse>(messages, totalCount, pagination.Page, pagination.PageSize));
    }

    private static async Task<Results<NoContent, ForbidHttpResult, NotFound>> MarkAsReadAsync(
        Guid id,
        HappyPawsDbContext dbContext,
        ClaimsPrincipal user,
        IHubContext<ChatHub, IChatClient> hubContext,
        CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var participant = await dbContext.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == id && cp.UserId == userId, ct);

        if (participant is null)
            return TypedResults.NotFound();

        var lastMessage = await dbContext.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == id)
            .OrderByDescending(m => m.SentAt)
            .FirstOrDefaultAsync(ct);

        if (lastMessage is not null)
        {
            participant.LastReadMessageId = lastMessage.Id;
            participant.LastReadAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(ct);

            await hubContext.Clients.Group(id.ToString()).MessageRead(id, userId, lastMessage.Id);
        }

        return TypedResults.NoContent();
    }
}
