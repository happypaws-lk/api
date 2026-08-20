using HappyPaws.Api.Extensions;
using HappyPaws.Core.Common;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HappyPaws.Api.Endpoints.Community;

public class CommunityEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetCommunityPosts)
            .WithName("GetCommunityPosts")
            .WithSummary("Get community feed")
            .Produces<PagedResult<CommunityPostDto>>();
            
        group.MapGet("/me", GetMyCommunityPosts)
            .WithName("GetMyCommunityPosts")
            .RequireAuthorization()
            .WithSummary("Get my community feed")
            .Produces<PagedResult<CommunityPostDto>>();
            
        group.MapPost("/{targetType}/{id}/upvote", ToggleUpvote)
            .WithName("ToggleUpvote")
            .RequireAuthorization()
            .WithSummary("Toggle upvote on a post")
            .Produces<UpvoteResponse>();

        group.MapDelete("/{targetType}/{id}", DeletePost)
            .WithName("DeletePost")
            .RequireAuthorization()
            .WithSummary("Delete a community post");

        group.MapGet("/{targetType}/{id}", GetPostById)
            .WithName("GetPostById")
            .WithSummary("Get a community post by ID")
            .Produces<CommunityPostDto>();
    }

    private static async Task<IResult> GetCommunityPosts(
        ClaimsPrincipal user,
        [Microsoft.AspNetCore.Mvc.FromServices] IStorageService storage,
        string sort = "Recent",
        int pageIndex = 1,
        int pageSize = 10,
        HappyPawsDbContext db = null!)
    {
        var currentUserId = Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        
        var stories = await db.CommunityStories.Include(s => s.Author).Where(s => s.IsActive).OrderByDescending(s => s.CreatedAt).Take(pageSize).ToListAsync();
        var rescues = await db.RescueCases.Include(r => r.Reporter).Where(r => r.IsActive).OrderByDescending(r => r.CreatedAt).Take(pageSize).ToListAsync();
        var listings = await db.AnimalListings.Include(a => a.Owner).Include(a => a.Photos).Where(a => a.IsActive).OrderByDescending(a => a.CreatedAt).Take(pageSize).ToListAsync();
        var transports = await db.TransportTasks.OrderByDescending(t => t.CreatedAt).Take(pageSize).ToListAsync();

        var targetIds = stories.Select(s=>s.Id).Concat(rescues.Select(r=>r.Id)).Concat(listings.Select(l=>l.Id)).Concat(transports.Select(t=>t.Id)).ToList();
        var upvotes = await db.PostUpvotes.Where(u => targetIds.Contains(u.TargetId)).ToListAsync();

        var dtos = new List<CommunityPostDto>();
        
        dtos.AddRange(stories.Select(s => CreateDto(s.Id, "COMMUNITY_STORY", s.Title, s.Content, s.Author?.Name ?? "Unknown", 0, s.CreatedAt, s.PhotoKey != null ? storage.GetPublicUrl(s.PhotoKey) : null, false, currentUserId, upvotes)));
        dtos.AddRange(rescues.Select(r => CreateDto(r.Id, "RESCUE_REPORT", r.Title ?? "Rescue Needed", r.Description, r.Reporter?.Name ?? "Unknown", 0, r.CreatedAt, r.PhotoKey != null ? storage.GetPublicUrl(r.PhotoKey) : null, false, currentUserId, upvotes)));
        dtos.AddRange(listings.Select(l => CreateDto(l.Id, "ADOPTION_LISTING", l.Title ?? "Adoption", l.Description, l.Owner?.Name ?? "Unknown", 0, l.CreatedAt, l.Photos.OrderBy(p => p.SortOrder).FirstOrDefault()?.StorageKey != null ? storage.GetPublicUrl(l.Photos.OrderBy(p => p.SortOrder).First().StorageKey) : null, false, currentUserId, upvotes)));
        dtos.AddRange(transports.Select(t => CreateDto(t.Id, "TRANSPORT_REQUEST", t.Title ?? "Transport", "Needs transport from " + t.PickupLocation, "Unknown", 0, t.CreatedAt, null, false, currentUserId, upvotes)));

        if (sort == "Recent") {
            dtos = dtos.OrderByDescending(d => d.CreatedAt).ToList();
        } else {
            dtos = dtos.OrderByDescending(d => d.Upvotes).ToList();
        }

        var pagedDtos = dtos.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        return TypedResults.Ok(new PagedResult<CommunityPostDto>(pagedDtos, dtos.Count, pageIndex, pageSize));
    }
    
    private static async Task<IResult> GetMyCommunityPosts(
        ClaimsPrincipal user,
        [Microsoft.AspNetCore.Mvc.FromServices] IStorageService storage,
        string sort = "Recent",
        int pageIndex = 1,
        int pageSize = 10,
        HappyPawsDbContext db = null!)
    {
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return TypedResults.Unauthorized();
        
        var stories = await db.CommunityStories.Include(s => s.Author).Where(s => s.AuthorId == userId && s.IsActive).OrderByDescending(s => s.CreatedAt).ToListAsync();
        var rescues = await db.RescueCases.Include(r => r.Reporter).Where(r => r.ReporterId == userId && r.IsActive).OrderByDescending(r => r.CreatedAt).ToListAsync();
        var listings = await db.AnimalListings.Include(a => a.Owner).Include(a => a.Photos).Where(l => l.OwnerId == userId && l.IsActive).OrderByDescending(a => a.CreatedAt).ToListAsync();

        var targetIds = stories.Select(s=>s.Id).Concat(rescues.Select(r=>r.Id)).Concat(listings.Select(l=>l.Id)).ToList();
        var allUpvotes = await db.PostUpvotes.Where(u => targetIds.Contains(u.TargetId)).ToListAsync();

        var dtos = new List<CommunityPostDto>();
        dtos.AddRange(stories.Select(s => CreateDto(s.Id, "COMMUNITY_STORY", s.Title, s.Content, s.Author?.Name ?? "Unknown", 0, s.CreatedAt, s.PhotoKey != null ? storage.GetPublicUrl(s.PhotoKey) : null, !s.IsActive, userId, allUpvotes)));
        dtos.AddRange(rescues.Select(r => CreateDto(r.Id, "RESCUE_REPORT", r.Title ?? "Rescue Needed", r.Description, r.Reporter?.Name ?? "Unknown", 0, r.CreatedAt, r.PhotoKey != null ? storage.GetPublicUrl(r.PhotoKey) : null, r.Status == HappyPaws.Core.Enums.CaseStatus.PendingApproval, userId, allUpvotes)));
        dtos.AddRange(listings.Select(l => CreateDto(l.Id, "ADOPTION_LISTING", l.Title ?? "Adoption", l.Description, l.Owner?.Name ?? "Unknown", 0, l.CreatedAt, l.Photos.OrderBy(p => p.SortOrder).FirstOrDefault()?.StorageKey != null ? storage.GetPublicUrl(l.Photos.OrderBy(p => p.SortOrder).First().StorageKey) : null, l.Status == HappyPaws.Core.Enums.ListingStatus.Pending, userId, allUpvotes)));

        if (sort == "Pending") {
            dtos = dtos.Where(d => d.IsPending).ToList();
        } else if (sort == "Recent") {
            dtos = dtos.OrderByDescending(d => d.CreatedAt).ToList();
        } else {
            dtos = dtos.OrderByDescending(d => d.Upvotes).ToList();
        }

        var pagedDtos = dtos.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        return TypedResults.Ok(new PagedResult<CommunityPostDto>(pagedDtos, dtos.Count, pageIndex, pageSize));
    }

    private static CommunityPostDto CreateDto(Guid id, string type, string title, string content, string authorName, int authorRep, DateTimeOffset createdAt, string? imageUrl, bool isPending, Guid currentUserId, List<PostUpvote> allUpvotes)
    {
        var upvotesForPost = allUpvotes.Where(u => u.TargetId == id).ToList();
        var isUpvoted = upvotesForPost.Any(u => u.UserId == currentUserId);
        return new CommunityPostDto(id, type, title, content, authorName, authorRep, upvotesForPost.Count, isUpvoted, createdAt, imageUrl, isPending);
    }

    private static async Task<IResult> ToggleUpvote(string targetType, Guid id, ClaimsPrincipal user, HappyPawsDbContext db)
    {
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return TypedResults.Unauthorized();

        var existing = await db.PostUpvotes.FirstOrDefaultAsync(u => u.TargetId == id && u.UserId == userId);
        bool isUpvoted;
        if (existing != null)
        {
            db.PostUpvotes.Remove(existing);
            isUpvoted = false;
        }
        else
        {
            db.PostUpvotes.Add(new PostUpvote { TargetId = id, TargetType = targetType, UserId = userId });
            isUpvoted = true;
        }

        await db.SaveChangesAsync();
        var count = await db.PostUpvotes.CountAsync(u => u.TargetId == id);
        
        return TypedResults.Ok(new UpvoteResponse(count, isUpvoted));
    }

    private static async Task<IResult> DeletePost(string targetType, Guid id, ClaimsPrincipal user, HappyPawsDbContext db, [Microsoft.AspNetCore.Mvc.FromServices] IStorageService storage)
    {
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return TypedResults.Unauthorized();
        bool isAdmin = user.IsInRole("Admin");

        if (targetType == "COMMUNITY_STORY")
        {
            var story = await db.CommunityStories.FindAsync(id);
            if (story == null) return TypedResults.NotFound();
            if (story.AuthorId != userId && !isAdmin) return TypedResults.Forbid();
            if (story.PhotoKey != null) await storage.DeleteAsync(story.PhotoKey);
            if (story.VideoKey != null) await storage.DeleteAsync(story.VideoKey);
            db.CommunityStories.Remove(story);
        }
        else if (targetType == "RESCUE_REPORT")
        {
            var rescue = await db.RescueCases
                .Include(r => r.CaseUpdates)
                .Include(r => r.TransportTasks)
                .Include(r => r.AnimalListings)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (rescue == null) return TypedResults.NotFound();
            if (rescue.ReporterId != userId && !isAdmin) return TypedResults.Forbid();
            if (rescue.PhotoKey != null) await storage.DeleteAsync(rescue.PhotoKey);
            
            foreach (var update in rescue.CaseUpdates)
            {
                if (update.PhotoKey != null) await storage.DeleteAsync(update.PhotoKey);
            }
            
            db.TransportTasks.RemoveRange(rescue.TransportTasks);
            db.CaseUpdates.RemoveRange(rescue.CaseUpdates);
            foreach (var listing in rescue.AnimalListings)
            {
                listing.RescueCaseId = null;
            }
            db.RescueCases.Remove(rescue);
        }
        else if (targetType == "ADOPTION_LISTING")
        {
            var listing = await db.AnimalListings
                .Include(l => l.Photos)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (listing == null) return TypedResults.NotFound();
            if (listing.OwnerId != userId && !isAdmin) return TypedResults.Forbid();
            
            foreach (var photo in listing.Photos)
            {
                await storage.DeleteAsync(photo.StorageKey);
            }
            
            var apps = await db.AdoptionApplications.Where(a => a.ListingId == id).ToListAsync();
            db.AdoptionApplications.RemoveRange(apps);
            db.AnimalListings.Remove(listing);
        }
        else if (targetType == "TRANSPORT_REQUEST")
        {
            var transport = await db.TransportTasks.FindAsync(id);
            if (transport == null) return TypedResults.NotFound();
            if (transport.PhotoKey != null) await storage.DeleteAsync(transport.PhotoKey);
            db.TransportTasks.Remove(transport);
        }
        else return TypedResults.BadRequest();

        var upvotes = await db.PostUpvotes.Where(u => u.TargetId == id).ToListAsync();
        db.PostUpvotes.RemoveRange(upvotes);

        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetPostById(string targetType, Guid id, ClaimsPrincipal user, [Microsoft.AspNetCore.Mvc.FromServices] IStorageService storage, HappyPawsDbContext db)
    {
        var currentUserId = Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;
        var upvotes = await db.PostUpvotes.Where(u => u.TargetId == id).ToListAsync();

        if (targetType == "COMMUNITY_STORY")
        {
            var s = await db.CommunityStories.Include(x => x.Author).FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return TypedResults.NotFound();
            return TypedResults.Ok(CreateDto(s.Id, targetType, s.Title, s.Content, s.Author?.Name ?? "Unknown", 0, s.CreatedAt, s.PhotoKey != null ? storage.GetPublicUrl(s.PhotoKey) : null, false, currentUserId, upvotes));
        }
        else if (targetType == "RESCUE_REPORT")
        {
            var r = await db.RescueCases.Include(x => x.Reporter).FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return TypedResults.NotFound();
            return TypedResults.Ok(CreateDto(r.Id, targetType, r.Title ?? "Rescue", r.Description, r.Reporter?.Name ?? "Unknown", 0, r.CreatedAt, r.PhotoKey != null ? storage.GetPublicUrl(r.PhotoKey) : null, false, currentUserId, upvotes));
        }
        else if (targetType == "ADOPTION_LISTING")
        {
            var l = await db.AnimalListings.Include(x => x.Owner).Include(x => x.Photos).FirstOrDefaultAsync(x => x.Id == id);
            if (l == null) return TypedResults.NotFound();
            return TypedResults.Ok(CreateDto(l.Id, targetType, l.Title ?? "Adoption", l.Description, l.Owner?.Name ?? "Unknown", 0, l.CreatedAt, l.Photos.OrderBy(p => p.SortOrder).FirstOrDefault()?.StorageKey != null ? storage.GetPublicUrl(l.Photos.OrderBy(p => p.SortOrder).First().StorageKey) : null, false, currentUserId, upvotes));
        }
        else if (targetType == "TRANSPORT_REQUEST")
        {
            var t = await db.TransportTasks.FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return TypedResults.NotFound();
            return TypedResults.Ok(CreateDto(t.Id, targetType, t.Title ?? "Transport", "Needs transport from " + t.PickupLocation, "Unknown", 0, t.CreatedAt, null, false, currentUserId, upvotes));
        }
        return TypedResults.NotFound();
    }
}

public record CommunityPostDto(
    Guid Id,
    string Type,
    string Title,
    string Content,
    string AuthorName,
    int AuthorReputation,
    int Upvotes,
    bool IsUpvotedByMe,
    DateTimeOffset CreatedAt,
    string? ImageUrl,
    bool IsPending = false
);

public record UpvoteResponse(int Upvotes, bool IsUpvotedByMe);
