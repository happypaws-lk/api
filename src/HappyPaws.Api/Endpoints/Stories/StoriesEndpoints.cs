using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HappyPaws.Api.Extensions;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Common;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;

namespace HappyPaws.Api.Endpoints.Stories;

public class StoriesEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.WithTags("Community Stories");

        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromServices] HappyPawsDbContext db,
            [FromServices] IStorageService storage) =>
        {
            var p = page ?? 1;
            var ps = pageSize ?? 10;
            p = p < 1 ? 1 : p;
            ps = ps < 1 ? 10 : ps;

            var query = db.CommunityStories
                .Include(s => s.Author)
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((p - 1) * ps).Take(ps).ToListAsync();

            var responses = items.Select(s => new CommunityStoryResponse(
                s.Id,
                s.Title,
                s.Content,
                s.PhotoKey != null ? storage.GetPublicUrl(s.PhotoKey) : null,
                s.VideoKey != null ? storage.GetPublicUrl(s.VideoKey) : null,
                s.Tags,
                s.AuthorId,
                s.Author.Name,
                s.CreatedAt
            )).ToList();

            return Results.Ok(new PagedResult<CommunityStoryResponse>(responses, totalCount, p, ps));
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] HappyPawsDbContext db,
            [FromServices] IStorageService storage) =>
        {
            var story = await db.CommunityStories
                .Include(s => s.Author)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (story == null) return Results.NotFound();

            return Results.Ok(new CommunityStoryResponse(
                story.Id,
                story.Title,
                story.Content,
                story.PhotoKey != null ? storage.GetPublicUrl(story.PhotoKey) : null,
                story.VideoKey != null ? storage.GetPublicUrl(story.VideoKey) : null,
                story.Tags,
                story.AuthorId,
                story.Author.Name,
                story.CreatedAt
            ));
        });

        group.MapPost("/", async (
            [FromForm] CreateCommunityStoryRequest request,
            [FromForm] IFormFile? photo,
            [FromForm] IFormFile? video,
            [FromServices] HappyPawsDbContext db,
            [FromServices] IStorageService storage,
            HttpContext httpContext) =>
        {
            var userId = httpContext.User.GetUserId();
            if (userId == Guid.Empty) return Results.Unauthorized();

            string? photoKey = null;
            if (photo != null)
            {
                using var stream = photo.OpenReadStream();
                photoKey = await storage.UploadAsync($"stories/photos/{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}", stream, photo.ContentType);
            }

            string? videoKey = null;
            if (video != null)
            {
                using var stream = video.OpenReadStream();
                videoKey = await storage.UploadAsync($"stories/videos/{Guid.NewGuid()}{Path.GetExtension(video.FileName)}", stream, video.ContentType);
            }

            var story = new CommunityStory
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                PhotoKey = photoKey,
                VideoKey = videoKey,
                Tags = request.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList() ?? [],
                AuthorId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            db.CommunityStories.Add(story);
            await db.SaveChangesAsync();

            var author = await db.Users.FindAsync(userId);
            
            return Results.Created($"/api/v1/stories/{story.Id}", new CommunityStoryResponse(
                story.Id,
                story.Title,
                story.Content,
                story.PhotoKey != null ? storage.GetPublicUrl(story.PhotoKey) : null,
                story.VideoKey != null ? storage.GetPublicUrl(story.VideoKey) : null,
                story.Tags,
                story.AuthorId,
                author!.Name,
                story.CreatedAt
            ));
        })
        .RequireAuthorization()
        .DisableAntiforgery();

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] HappyPawsDbContext db,
            [FromServices] IStorageService storage,
            HttpContext httpContext) =>
        {
            var userId = httpContext.User.GetUserId();
            var story = await db.CommunityStories.FindAsync(id);
            
            if (story == null) return Results.NotFound();
            
            if (story.AuthorId != userId && !httpContext.User.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            if (story.PhotoKey != null) await storage.DeleteAsync(story.PhotoKey);
            if (story.VideoKey != null) await storage.DeleteAsync(story.VideoKey);

            var upvotes = await db.PostUpvotes.Where(u => u.TargetId == id).ToListAsync();
            db.PostUpvotes.RemoveRange(upvotes);

            db.CommunityStories.Remove(story);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization();
    }
}
