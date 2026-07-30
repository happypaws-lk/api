using System.Security.Claims;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Filters;
using HappyPaws.Core.Common;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Api.Endpoints.Applications;

public class ApplicationsEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", CreateApplicationAsync)
            .RequireAuthorization("Verified")
            .AddEndpointFilter<ValidationFilter<CreateApplicationRequest>>()
            .WithName("CreateApplication")
            .WithSummary("Submit an adoption application for a listing");

        group.MapGet("/me", GetMyApplicationsAsync)
            .RequireAuthorization()
            .WithName("GetMyApplications")
            .WithSummary("List all adoption applications submitted by the authenticated user");

        group.MapPut("/{id:guid}/accept", AcceptApplicationAsync)
            .RequireAuthorization("Verified")
            .WithName("AcceptApplication")
            .WithSummary("Accept an adoption application");

        group.MapPut("/{id:guid}/decline", DeclineApplicationAsync)
            .RequireAuthorization("Verified")
            .WithName("DeclineApplication")
            .WithSummary("Decline an adoption application");
    }

    private static async Task<Results<Created<ApplicationResponse>, Conflict<string>, NotFound<string>>> CreateApplicationAsync(
        CreateApplicationRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var listing = await db.AnimalListings.FirstOrDefaultAsync(l => l.Id == request.ListingId && l.IsActive, ct);
        if (listing is null)
            return TypedResults.NotFound("Listing not found.");

        if (listing.OwnerId == userId)
            return TypedResults.Conflict("You cannot apply for your own listing.");

        var existingApp = await db.AdoptionApplications
            .FirstOrDefaultAsync(a => a.ListingId == request.ListingId && a.ApplicantId == userId, ct);

        if (existingApp is not null)
            return TypedResults.Conflict("You have already applied for this listing.");

        var application = new AdoptionApplication
        {
            Id = Guid.NewGuid(),
            ListingId = request.ListingId,
            ApplicantId = userId,
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTimeOffset.UtcNow
        };

        db.AdoptionApplications.Add(application);
        await db.SaveChangesAsync(ct);

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);

        var response = new ApplicationResponse(
            application.Id, listing.Id, listing.Name, application.ApplicantId, user.Name,
            application.Status, application.ReviewNotes, application.AppliedAt, application.UpdatedAt);

        return TypedResults.Created($"/api/v1/applications/{application.Id}", response);
    }

    private static async Task<Ok<PagedResult<ApplicationResponse>>> GetMyApplicationsAsync(
        [AsParameters] PaginationQuery pagination,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var query = db.AdoptionApplications
            .AsNoTracking()
            .Include(a => a.Listing)
            .Include(a => a.Applicant)
            .Where(a => a.ApplicantId == userId);

        var totalCount = await query.CountAsync(ct);

        var applications = await query
            .OrderByDescending(a => a.AppliedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(a => new ApplicationResponse(
                a.Id, a.ListingId, a.Listing.Name, a.ApplicantId, a.Applicant.Name,
                a.Status, a.ReviewNotes, a.AppliedAt, a.UpdatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<ApplicationResponse>(applications, totalCount, pagination.Page, pagination.PageSize));
    }

    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ForbidHttpResult>> AcceptApplicationAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        INotificationService notificationService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var application = await db.AdoptionApplications
            .Include(a => a.Listing)
            .Include(a => a.Applicant)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (application is null)
            return TypedResults.NotFound();

        if (application.Listing.OwnerId != userId)
            return TypedResults.Forbid();

        application.Status = ApplicationStatus.Accepted;
        application.Listing.Status = ListingStatus.Pending;

        await db.SaveChangesAsync(ct);

        await notificationService.SendNotificationAsync(
            application.ApplicantId,
            "application_accepted",
            "Adoption Application Accepted!",
            $"Your application for {application.Listing.Name} has been accepted.",
            application.Id,
            "AdoptionApplication",
            new Dictionary<string, string>
            {
                ["applicationId"] = application.Id.ToString(),
                ["listingId"] = application.ListingId.ToString()
            },
            ct);

        var response = new ApplicationResponse(
            application.Id, application.ListingId, application.Listing.Name, application.ApplicantId, application.Applicant.Name,
            application.Status, application.ReviewNotes, application.AppliedAt, application.UpdatedAt);

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<ApplicationResponse>, NotFound, ForbidHttpResult>> DeclineApplicationAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        INotificationService notificationService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var application = await db.AdoptionApplications
            .Include(a => a.Listing)
            .Include(a => a.Applicant)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (application is null)
            return TypedResults.NotFound();

        if (application.Listing.OwnerId != userId)
            return TypedResults.Forbid();

        application.Status = ApplicationStatus.Declined;
        await db.SaveChangesAsync(ct);

        await notificationService.SendNotificationAsync(
            application.ApplicantId,
            "application_declined",
            "Adoption Application Update",
            $"Your application for {application.Listing.Name} has been declined.",
            application.Id,
            "AdoptionApplication",
            new Dictionary<string, string>
            {
                ["applicationId"] = application.Id.ToString(),
                ["listingId"] = application.ListingId.ToString()
            },
            ct);

        var response = new ApplicationResponse(
            application.Id, application.ListingId, application.Listing.Name, application.ApplicantId, application.Applicant.Name,
            application.Status, application.ReviewNotes, application.AppliedAt, application.UpdatedAt);

        return TypedResults.Ok(response);
    }
}
