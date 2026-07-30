using HappyPaws.Core;
using HappyPaws.Core.Common;
using HappyPaws.Core.Entities;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HappyPaws.Api.Endpoints.Notifications;

public class NotificationEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", GetNotifications)
            .WithName("GetNotifications")
            .WithSummary("Get current user's notifications");

        group.MapPut("/{id:guid}/read", MarkAsRead)
            .WithName("MarkNotificationAsRead")
            .WithSummary("Mark a specific notification as read");

        group.MapPut("/read-all", MarkAllAsRead)
            .WithName("MarkAllNotificationsAsRead")
            .WithSummary("Mark all notifications as read");

        group.MapGet("/unread-count", GetUnreadCount)
            .WithName("GetUnreadNotificationCount")
            .WithSummary("Get count of unread notifications");
    }

    private static async Task<Ok<PagedResult<NotificationResponse>>> GetNotifications(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ClaimsPrincipal user,
        HappyPawsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var query = new PaginationQuery(page > 0 ? page : 1, pageSize > 0 ? pageSize : 20);

        var dbQuery = dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(n => new NotificationResponse(
                n.Id,
                n.Type,
                n.Title,
                n.Body,
                n.ReferenceId,
                n.ReferenceType,
                n.IsRead,
                n.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var result = new PagedResult<NotificationResponse>(items, totalCount, query.Page, query.PageSize);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> MarkAsRead(
        Guid id,
        ClaimsPrincipal user,
        HappyPawsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var notification = await dbContext.Set<Notification>()
            .FindAsync([id], cancellationToken);

        if (notification is null) return TypedResults.NotFound();
        if (notification.UserId != userId) return TypedResults.Forbid();

        notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> MarkAllAsRead(
        ClaimsPrincipal user,
        HappyPawsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await dbContext.Set<Notification>()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(n => n.IsRead, true),
                cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<UnreadCountResponse>> GetUnreadCount(
        ClaimsPrincipal user,
        HappyPawsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var count = await dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync(cancellationToken);

        return TypedResults.Ok(new UnreadCountResponse(count));
    }
}
