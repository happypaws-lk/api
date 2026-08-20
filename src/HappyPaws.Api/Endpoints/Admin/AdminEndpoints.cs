using System.Security.Claims;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Filters;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using HappyPaws.Core.Common;

namespace HappyPaws.Api.Endpoints.Admin;

public class AdminEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization("Admin");

        group.MapGet("/dashboard", GetDashboardAsync)
            .WithName("GetAdminDashboard")
            .WithSummary("Get admin dashboard statistics")
            .WithDescription("Returns summary counts, recent moderation activity, and historical time-series data for user growth and adoption activity. Use startDate and endDate (YYYY-MM-DD) to bound the time-series arrays. Defaults to the last 30 days when omitted.")
            .Produces<DashboardResponse>()
            .ProducesValidationProblem();

        group.MapGet("/cases", GetCasesAsync)
            .WithName("GetAdminCases")
            .WithSummary("Get all active rescue cases for the live map")
            .WithDescription("Returns all active rescue cases with coordinates for the admin live map view.")
            .Produces<List<AdminCaseResponse>>();

        group.MapPost("/cases/{id:guid}/approve", ApproveCaseAsync)
            .WithName("ApproveRescueCase")
            .WithSummary("Approve a pending rescue case")
            .WithDescription("Approves a RescueCase and notifies nearby volunteers.")
            .Produces<Ok>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/users", GetUsersAsync)
            .WithName("GetAdminUsers")
            .WithSummary("Get a paginated list of users")
            .WithDescription("Returns a paginated list of users. Supports filtering by name, email, role, and suspension status.")
            .Produces<PagedResult<AdminUserResponse>>();

        group.MapPut("/users/{id:guid}/suspend", SuspendUserAsync)
            .AddEndpointFilter<ValidationFilter<SuspendRequest>>()
            .WithName("SuspendUser")
            .WithSummary("Suspend a user")
            .WithDescription("Sets the suspension flag and records a moderation action. Safe to call on an already-suspended user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        group.MapPut("/users/{id:guid}/unsuspend", UnsuspendUserAsync)
            .WithName("UnsuspendUser")
            .WithSummary("Unsuspend a user")
            .WithDescription("Clears the suspension flag. Safe to call on an active user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/users/{id:guid}", DeleteUserAsync)
            .WithName("DeleteUser")
            .WithSummary("Delete a user")
            .WithDescription("Completely deletes a user from the platform.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/moderation", CreateModerationActionAsync)
            .AddEndpointFilter<ValidationFilter<ModerationRequest>>()
            .WithName("CreateModerationAction")
            .WithSummary("Perform a moderation action")
            .WithDescription("Creates a moderation action. Supported combinations: remove a Listing, remove a Message, suspend a User, or warn a User.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        group.MapGet("/moderation", GetModerationLogAsync)
            .WithName("GetModerationLog")
            .WithSummary("Get paginated moderation log")
            .WithDescription("Returns a full paginated history of moderation actions, most recent first.")
            .Produces<PagedResult<ModerationLogResponse>>();

        group.MapPut("/reputation/{userId:guid}", AdjustReputationAsync)
            .AddEndpointFilter<ValidationFilter<ReputationAdjustRequest>>()
            .WithName("AdjustReputation")
            .WithSummary("Adjust a user's reputation points directly")
            .WithDescription("Directly adjusts a user's reputation points. Pass a positive value to add points or a negative value to deduct them. Re-evaluates badge eligibility after the adjustment.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapGet("/listings", GetAdminListingsAsync)
            .WithName("GetAdminListings")
            .WithSummary("Get a paginated list of all listings for admin management")
            .WithDescription("Returns a paginated list of all animal listings, including inactive (soft-deleted) ones. Supports filtering by status, species, and owner name.")
            .Produces<PagedResult<AdminListingResponse>>();

        group.MapGet("/users/{id:guid}", GetUserDetailAsync)
            .WithName("GetAdminUserDetail")
            .WithSummary("Get full details of a specific user")
            .WithDescription("Returns all details for a specific user including roles, verification status, suspension info, and reputation.")
            .Produces<AdminUserDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/kyc/pending", GetPendingKycAsync)
            .WithName("GetPendingKyc")
            .WithSummary("List all pending KYC documents for review")
            .WithDescription("Returns all KYC documents awaiting review. Each entry includes a 15-minute presigned URL to view the document securely.")
            .Produces<List<KycPendingResponse>>();

        group.MapPost("/kyc/{id:guid}/approve", ApproveKycAsync)
            .WithName("ApproveKyc")
            .WithSummary("Approve a KYC document and verify the user")
            .WithDescription("Approves the document, marks the user as verified, sends a confirmation email, and evaluates badge eligibility.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/kyc/{id:guid}/reject", RejectKycAsync)
            .AddEndpointFilter<ValidationFilter<KycRejectRequest>>()
            .WithName("RejectKyc")
            .WithSummary("Reject a KYC document with a reason")
            .WithDescription("Rejects the document with a reason and notifies the user by email and push notification.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapGet("/role-requests/pending", GetPendingRoleRequestsAsync)
            .WithName("GetPendingRoleRequests")
            .WithSummary("List all pending role requests for review")
            .WithDescription("Returns all role requests awaiting review, ordered oldest first. Each entry includes a 15-minute presigned URL to view the supporting document.")
            .Produces<List<RoleRequestPendingResponse>>();

        group.MapPost("/role-requests/{id:guid}/approve", ApproveRoleRequestAsync)
            .WithName("ApproveRoleRequest")
            .WithSummary("Approve a role request and grant the role")
            .WithDescription("Approves the request, assigns the role to the user, and sends a push notification.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/role-requests/{id:guid}/reject", RejectRoleRequestAsync)
            .AddEndpointFilter<ValidationFilter<RoleRequestRejectRequest>>()
            .WithName("RejectRoleRequest")
            .WithSummary("Reject a role request with a reason")
            .WithDescription("Rejects the request with a reason and notifies the user via push notification.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapGet("/community/posts", GetCommunityPostsAsync)
            .WithName("GetAdminCommunityPosts")
            .WithSummary("Get community posts")
            .WithDescription("Returns a paginated list of community posts across all content types (Adoption Listings, Rescue Reports, Transport Requests, Community Stories), with optional filtering by type and pending status.")
            .Produces<PagedResult<CommunityPostResponse>>();

        group.MapGet("/community/pending", GetPendingCommunityPostsAsync)
            .WithName("GetPendingCommunityPosts")
            .WithSummary("Get all pending community posts awaiting approval")
            .WithDescription("Returns all community posts across all content types that are awaiting admin approval.")
            .Produces<List<CommunityPostResponse>>();

        group.MapPost("/community/{type}/{id:guid}/approve", ApproveCommunityPostAsync)
            .WithName("ApproveCommunityPost")
            .WithSummary("Approve a pending community post")
            .WithDescription("Approves a community post, making it visible in the community feed.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/community/{type}/{id:guid}/reject", RejectCommunityPostAsync)
            .WithName("RejectCommunityPost")
            .WithSummary("Reject a pending community post")
            .WithDescription("Rejects a community post, removing it from the approval queue.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/community/{type}/{id:guid}", DeleteCommunityPostAsync)
            .WithName("DeleteCommunityPost")
            .WithSummary("Permanently delete a community post")
            .WithDescription("Hard deletes a community post across all content types (Rescue Reports, Adoption Listings, Transport Requests, Community Stories).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/community/{type}/{id:guid}", GetCommunityPostDetailAsync)
            .WithName("GetCommunityPostDetail")
            .WithSummary("Get full details of a community post")
            .WithDescription("Returns the full details of a community post by content type and ID.")
            .Produces<CommunityPostDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<Results<Ok<DashboardResponse>, ValidationProblem>> GetDashboardAsync(
        DateOnly? startDate,
        DateOnly? endDate,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["startDate"] = ["startDate must not be after endDate"]
            });

        var effectiveEnd = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveStart = startDate ?? effectiveEnd.AddDays(-29);

        var rangeStart = new DateTimeOffset(effectiveStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rangeEnd = new DateTimeOffset(effectiveEnd.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(1);

        // Summary stats
        var pendingKycCount = await db.IdentityDocuments.CountAsync(d => d.Status == DocumentStatus.Pending, ct);
        var openCasesCount = await db.RescueCases.CountAsync(c => c.Status == CaseStatus.Open && c.IsActive, ct);
        var totalUsersCount = await db.Users.CountAsync(ct);

        var recentActivity = await db.ModerationActions
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .Select(m => new ModerationLogResponse(
                m.Id,
                m.AdminId,
                m.TargetType.ToString(),
                m.TargetId,
                m.ActionType.ToString(),
                m.Reason,
                m.CreatedAt))
            .ToListAsync(ct);

        // Baselines (cumulative counts before the range window)
        var userBaseline = await db.Users
            .CountAsync(u => u.CreatedAt < rangeStart, ct);

        var verifiedBaseline = await db.IdentityDocuments
            .Where(d => d.Status == DocumentStatus.Approved
                && d.ReviewedAt.HasValue
                && d.ReviewedAt < rangeStart)
            .Select(d => d.UserId)
            .Distinct()
            .CountAsync(ct);

        // Daily aggregates within the range
        var dailyNewUsers = await db.Users
            .Where(u => u.CreatedAt >= rangeStart && u.CreatedAt < rangeEnd)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var dailyNewVerified = await db.IdentityDocuments
            .Where(d => d.Status == DocumentStatus.Approved
                && d.ReviewedAt.HasValue
                && d.ReviewedAt >= rangeStart
                && d.ReviewedAt < rangeEnd)
            .GroupBy(d => d.ReviewedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var dailyApplications = await db.AdoptionApplications
            .Where(a => a.AppliedAt >= rangeStart && a.AppliedAt < rangeEnd)
            .GroupBy(a => a.AppliedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var dailyAdoptions = await db.AnimalListings
            .Where(l => l.Status == ListingStatus.Adopted
                && l.UpdatedAt >= rangeStart
                && l.UpdatedAt < rangeEnd)
            .GroupBy(l => l.UpdatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Build full date series with zeros for days with no activity
        var newUsersMap = dailyNewUsers.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var newVerifiedMap = dailyNewVerified.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var applicationsMap = dailyApplications.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var adoptionsMap = dailyAdoptions.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);

        var userGrowth = new List<UserGrowthDataPoint>();
        var adoptionActivity = new List<AdoptionActivityDataPoint>();
        var runningTotal = userBaseline;
        var runningVerified = verifiedBaseline;

        for (var day = effectiveStart; day <= effectiveEnd; day = day.AddDays(1))
        {
            var newUsers = newUsersMap.GetValueOrDefault(day, 0);
            var newVerified = newVerifiedMap.GetValueOrDefault(day, 0);
            runningTotal += newUsers;
            runningVerified += newVerified;

            userGrowth.Add(new UserGrowthDataPoint(day, runningTotal, newUsers, runningVerified));
            adoptionActivity.Add(new AdoptionActivityDataPoint(
                day,
                applicationsMap.GetValueOrDefault(day, 0),
                adoptionsMap.GetValueOrDefault(day, 0)));
        }

        return TypedResults.Ok(new DashboardResponse(
            pendingKycCount,
            openCasesCount,
            totalUsersCount,
            recentActivity,
            userGrowth,
            adoptionActivity));
    }

    private static async Task<Ok<List<AdminCaseResponse>>> GetCasesAsync(
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var cases = await db.RescueCases
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new AdminCaseResponse(
                c.Id,
                c.LocationCoords.X,
                c.LocationCoords.Y,
                c.LocationName,
                c.Urgency.ToString(),
                c.Status.ToString()))
            .ToListAsync(ct);

        return TypedResults.Ok(cases);
    }

    private static async Task<Results<Ok, NotFound>> ApproveCaseAsync(
        Guid id,
        HappyPawsDbContext db,
        IServiceProvider serviceProvider,
        INotificationService notificationService,
        ISystemConfigService configService,
        ILogger<AdminEndpoints> logger,
        CancellationToken ct)
    {
        var rescueCase = await db.RescueCases.FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);

        if (rescueCase == null)
            return TypedResults.NotFound();

        if (rescueCase.Status != CaseStatus.PendingApproval)
            return TypedResults.Ok(); // Already approved or in another state

        rescueCase.Status = CaseStatus.Open;
        await db.SaveChangesAsync(ct);

        // Send proximity alerts to volunteers
        _ = Task.Run(async () =>
        {
            try
            {
                // We use a new scope since this runs in the background
                using var scope = serviceProvider.CreateScope();
                var scopedDb = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
                var scopedNotification = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var scopedConfig = scope.ServiceProvider.GetRequiredService<ISystemConfigService>();

                var radiusMeters = (await scopedConfig.GetAlertRadiusKmAsync(CancellationToken.None)) * 1000.0;
                var caseLocation = rescueCase.LocationCoords;

                var responderIds = await scopedDb.Users
                    .AsNoTracking()
                    .Where(u => u.IsVerified && !u.IsSuspended && u.Id != rescueCase.ReporterId)
                    .Where(u => u.LastKnownLocation != null &&
                                u.LastKnownLocation.IsWithinDistance(caseLocation, radiusMeters))
                    .Where(u => u.Roles.Any(r => r.Role == Role.Foster || r.Role == Role.Transporter || r.Role == Role.Veterinarian))
                    .Select(u => u.Id)
                    .ToListAsync();

                if (responderIds.Count > 0)
                {
                    await scopedNotification.SendNotificationsAsync(
                        responderIds,
                        "rescue_nearby",
                        "New Rescue Case Nearby",
                        $"{rescueCase.Urgency} urgency rescue reported at {rescueCase.LocationName}",
                        rescueCase.Id,
                        "RescueCase",
                        new Dictionary<string, string>
                        {
                            ["caseId"] = rescueCase.Id.ToString(),
                            ["urgency"] = rescueCase.Urgency.ToString()
                        },
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send proximity alerts for approved case {CaseId}", rescueCase.Id);
            }
        });

        return TypedResults.Ok();
    }

    private static async Task<Ok<PagedResult<AdminUserResponse>>> GetUsersAsync(
        [AsParameters] PaginationQuery query,
        string? name,
        string? email,
        string? role,
        bool? isSuspended,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var dbQuery = db.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            dbQuery = dbQuery.Where(u => EF.Functions.ILike(u.Name, $"%{name}%"));

        if (!string.IsNullOrWhiteSpace(email))
            dbQuery = dbQuery.Where(u => EF.Functions.ILike(u.Email, $"%{email}%"));
            
        if (isSuspended.HasValue)
            dbQuery = dbQuery.Where(u => u.IsSuspended == isSuspended.Value);

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<Role>(role, true, out var roleEnum))
        {
            dbQuery = dbQuery.Where(u => u.Roles.Any(r => r.Role == roleEnum));
        }

        var totalCount = await dbQuery.CountAsync(ct);
        
        var users = await dbQuery
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var items = users.Select(u => new AdminUserResponse(
            u.Id,
            u.Name,
            u.Email,
            u.IsVerified,
            u.IsSuspended,
            u.ReputationPoints,
            u.Roles.Select(r => r.Role.ToString()).ToList(),
            u.CreatedAt));

        return TypedResults.Ok(new PagedResult<AdminUserResponse>(items, totalCount, query.Page, query.PageSize));
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> SuspendUserAsync(
        Guid id,
        SuspendRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var adminId = principal.GetUserId();
        if (adminId == id)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid Operation",
                Detail = "Admins cannot suspend their own account."
            });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return TypedResults.NotFound();

        if (!user.IsSuspended)
        {
            user.IsSuspended = true;
            user.SuspendedAt = DateTimeOffset.UtcNow;
            user.SuspendedReason = request.Reason;

            db.ModerationActions.Add(new HappyPaws.Core.Entities.ModerationAction
            {
                Id = Guid.NewGuid(),
                AdminId = adminId,
                TargetType = ModerationTargetType.User,
                TargetId = user.Id,
                ActionType = ModerationActionType.Suspended,
                Reason = request.Reason,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(ct);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> UnsuspendUserAsync(
        Guid id,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return TypedResults.NotFound();

        if (user.IsSuspended)
        {
            user.IsSuspended = false;
            user.SuspendedAt = null;
            user.SuspendedReason = null;

            await db.SaveChangesAsync(ct);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> DeleteUserAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var adminId = principal.GetUserId();
        if (adminId == id)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid Operation",
                Detail = "Admins cannot delete their own account."
            });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return TypedResults.NotFound();

        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> CreateModerationActionAsync(
        ModerationRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        INotificationService notificationService,
        CancellationToken ct)
    {
        var adminId = principal.GetUserId();

        if (request.TargetType == ModerationTargetType.Listing && request.ActionType == ModerationActionType.Removed)
        {
            var listing = await db.AnimalListings.FirstOrDefaultAsync(l => l.Id == request.TargetId, ct);
            if (listing is null) return TypedResults.NotFound();
            
            listing.IsActive = false;
            listing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else if (request.TargetType == ModerationTargetType.Message && request.ActionType == ModerationActionType.Removed)
        {
            var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == request.TargetId, ct);
            if (message is null) return TypedResults.NotFound();
            
            message.Content = "[Removed by moderator]";
        }
        else if (request.TargetType == ModerationTargetType.User && request.ActionType == ModerationActionType.Suspended)
        {
            if (request.TargetId == adminId)
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Operation",
                    Detail = "Admins cannot suspend their own account."
                });
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.TargetId, ct);
            if (user is null) return TypedResults.NotFound();

            user.IsSuspended = true;
            user.SuspendedAt = DateTimeOffset.UtcNow;
            user.SuspendedReason = request.Reason;
        }
        else if (request.ActionType != ModerationActionType.Warned)
        {
            return TypedResults.BadRequest(new ProblemDetails 
            { 
                Title = "Invalid moderation action combination",
                Detail = "The specified TargetType and ActionType combination is not supported."
            });
        }

        db.ModerationActions.Add(new HappyPaws.Core.Entities.ModerationAction
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            ActionType = request.ActionType,
            Reason = request.Reason,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        
        // Find owner ID for notification if Warned
        if (request.ActionType == ModerationActionType.Warned && request.TargetType == ModerationTargetType.User)
        {
            await notificationService.SendNotificationAsync(
                request.TargetId,
                "moderation_warning",
                "Account Warning",
                $"Your account has received a warning from a moderator. Reason: {request.Reason}",
                null,
                null,
                null,
                ct);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResult<ModerationLogResponse>>> GetModerationLogAsync(
        [AsParameters] PaginationQuery query,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var dbQuery = db.ModerationActions.AsNoTracking();
        
        var totalCount = await dbQuery.CountAsync(ct);
        
        var actions = await dbQuery
            .OrderByDescending(m => m.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var items = actions.Select(m => new ModerationLogResponse(
            m.Id,
            m.AdminId,
            m.TargetType.ToString(),
            m.TargetId,
            m.ActionType.ToString(),
            m.Reason,
            m.CreatedAt));

        return TypedResults.Ok(new PagedResult<ModerationLogResponse>(items, totalCount, query.Page, query.PageSize));
    }

    private static async Task<Results<NoContent, NotFound>> AdjustReputationAsync(
        Guid userId,
        ReputationAdjustRequest request,
        HappyPawsDbContext db,
        IBadgeEvaluationService badgeEvaluationService,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return TypedResults.NotFound();

        db.ReputationEvents.Add(new HappyPaws.Core.Entities.ReputationEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = "AdminAdjustment",
            Points = request.PointsToAdjust,
            CreatedAt = DateTimeOffset.UtcNow
        });

        user.ReputationPoints += request.PointsToAdjust;

        await db.SaveChangesAsync(ct);

        await badgeEvaluationService.EvaluateAndAwardBadgesAsync(userId, ct);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResult<AdminListingResponse>>> GetAdminListingsAsync(
        [AsParameters] PaginationQuery query,
        string? species,
        string? status,
        string? ownerName,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var dbQuery = db.AnimalListings
            .AsNoTracking()
            .Include(l => l.Owner)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(species))
            dbQuery = dbQuery.Where(l => EF.Functions.ILike(l.Species, $"%{species}%"));

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ListingStatus>(status, true, out var statusEnum))
            dbQuery = dbQuery.Where(l => l.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(ownerName))
            dbQuery = dbQuery.Where(l => EF.Functions.ILike(l.Owner.Name, $"%{ownerName}%"));

        var totalCount = await dbQuery.CountAsync(ct);

        var listings = await dbQuery
            .OrderByDescending(l => l.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new AdminListingResponse(
                l.Id,
                l.Name,
                l.Species,
                l.Breed,
                l.OwnerId,
                l.Owner.Name,
                l.Status,
                l.IsActive,
                l.LocationName,
                l.CreatedAt,
                l.UpdatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<AdminListingResponse>(listings, totalCount, query.Page, query.PageSize));
    }

    private static async Task<Results<Ok<AdminUserDetailResponse>, NotFound>> GetUserDetailAsync(
        Guid id,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.Badges)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(new AdminUserDetailResponse(
            user.Id,
            user.Name,
            user.Email,
            user.IsVerified,
            user.IsSuspended,
            user.SuspendedAt,
            user.SuspendedReason,
            user.ReputationPoints,
            user.CreatedAt,
            user.UpdatedAt,
            user.Roles.Select(r => r.Role.ToString()).ToList(),
            user.Badges.Select(b => b.BadgeType.ToString()).ToList()));
    }

    private static async Task<Ok<List<KycPendingResponse>>> GetPendingKycAsync(
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var documents = await db.IdentityDocuments
            .AsNoTracking()
            .Include(d => d.User)
            .Where(d => d.Status == DocumentStatus.Pending)
            .OrderBy(d => d.UploadedAt)
            .ToListAsync(ct);

        var responses = new List<KycPendingResponse>();
        foreach (var doc in documents)
        {
            var url = await storageService.GetPresignedUrlAsync(doc.DocumentKey, TimeSpan.FromMinutes(15), ct);
            responses.Add(new KycPendingResponse(
                doc.Id,
                doc.UserId,
                doc.User.Name,
                doc.User.Email,
                doc.DocumentType,
                url,
                doc.UploadedAt));
        }

        return TypedResults.Ok(responses);
    }

    private static async Task<Results<NoContent, NotFound>> ApproveKycAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IEmailSender emailSender,
        INotificationService notificationService,
        IBadgeEvaluationService badgeEvaluationService,
        CancellationToken ct)
    {
        var adminId = principal.GetUserId();

        var document = await db.IdentityDocuments
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (document is null)
            return TypedResults.NotFound();

        document.Status = DocumentStatus.Approved;
        document.ReviewedById = adminId;
        document.ReviewedAt = DateTimeOffset.UtcNow;
        document.User.IsVerified = true;

        await db.SaveChangesAsync(ct);

        await emailSender.SendVerificationDecisionAsync(document.User.Email, true, null, ct);

        await notificationService.SendNotificationAsync(
            document.UserId,
            "kyc_approved",
            "Verification Approved",
            "Your identity document has been verified. You now have full access to the platform.",
            document.Id,
            "IdentityDocument",
            null,
            ct);

        await badgeEvaluationService.EvaluateAndAwardBadgesAsync(document.UserId, ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> RejectKycAsync(
        Guid id,
        KycRejectRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IEmailSender emailSender,
        INotificationService notificationService,
        CancellationToken ct)
    {
        var adminId = principal.GetUserId();

        var document = await db.IdentityDocuments
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (document is null)
            return TypedResults.NotFound();

        document.Status = DocumentStatus.Rejected;
        document.RejectionReason = request.Reason;
        document.ReviewedById = adminId;
        document.ReviewedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await emailSender.SendVerificationDecisionAsync(document.User.Email, false, request.Reason, ct);

        await notificationService.SendNotificationAsync(
            document.UserId,
            "kyc_rejected",
            "Verification Rejected",
            "Your identity document was rejected. Please review the reason and try again.",
            document.Id,
            "IdentityDocument",
            null,
            ct);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<List<RoleRequestPendingResponse>>> GetPendingRoleRequestsAsync(
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var requests = await db.RoleRequests
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.Status == RoleRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

        var responses = new List<RoleRequestPendingResponse>();
        foreach (var r in requests)
        {
            var url = await storageService.GetPresignedUrlAsync(r.DocumentKey, TimeSpan.FromMinutes(15), ct);
            responses.Add(new RoleRequestPendingResponse(
                r.Id,
                r.UserId,
                r.User.Name,
                r.User.Email,
                r.Role.ToString(),
                r.DocumentType,
                url,
                r.Justification,
                r.CreatedAt));
        }

        return TypedResults.Ok(responses);
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> ApproveRoleRequestAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        INotificationService notificationService,
        CancellationToken ct)
    {
        var adminId = principal.GetUserId();

        var roleRequest = await db.RoleRequests
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (roleRequest is null)
            return TypedResults.NotFound();

        if (roleRequest.Status != RoleRequestStatus.Pending)
            return TypedResults.Conflict("This request is not pending.");

        var alreadyAssigned = await db.UserRoles
            .AnyAsync(r => r.UserId == roleRequest.UserId && r.Role == roleRequest.Role, ct);

        if (!alreadyAssigned)
        {
            db.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = roleRequest.UserId,
                Role = roleRequest.Role,
                AssignedAt = DateTimeOffset.UtcNow
            });
        }

        roleRequest.Status = RoleRequestStatus.Approved;
        roleRequest.ReviewedById = adminId;
        roleRequest.ReviewedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await notificationService.SendNotificationAsync(
            roleRequest.UserId,
            "role_request_approved",
            "Role Request Approved",
            $"Your {roleRequest.Role} role request has been approved. You now have access to {roleRequest.Role} features.",
            roleRequest.Id,
            "RoleRequest",
            null,
            ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> RejectRoleRequestAsync(
        Guid id,
        RoleRequestRejectRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        INotificationService notificationService,
        CancellationToken ct)
    {
        var adminId = principal.GetUserId();

        var roleRequest = await db.RoleRequests
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (roleRequest is null)
            return TypedResults.NotFound();

        if (roleRequest.Status != RoleRequestStatus.Pending)
            return TypedResults.Conflict("This request is not pending.");

        roleRequest.Status = RoleRequestStatus.Rejected;
        roleRequest.RejectionReason = request.Reason;
        roleRequest.ReviewedById = adminId;
        roleRequest.ReviewedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await notificationService.SendNotificationAsync(
            roleRequest.UserId,
            "role_request_rejected",
            "Role Request Not Approved",
            $"Your {roleRequest.Role} role request was not approved. Reason: {request.Reason}",
            roleRequest.Id,
            "RoleRequest",
            null,
            ct);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResult<CommunityPostResponse>>> GetCommunityPostsAsync(
        [AsParameters] PaginationQuery query,
        string? type,
        bool? onlyPending,
        string? status,
        HappyPawsDbContext db,
        IStorageService storage,
        CancellationToken ct)
    {
        var posts = new List<CommunityPostResponse>();
        var filterPending = onlyPending == true || string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase);
        var filterApproved = onlyPending == false || string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase);

        if (type == null || type == "RESCUE_REPORT")
        {
            var rescueQuery = db.RescueCases
                .AsNoTracking()
                .Include(c => c.Reporter)
                .Where(c => c.IsActive);

            if (filterPending)
            {
                rescueQuery = rescueQuery.Where(c => c.Status == CaseStatus.PendingApproval);
            }
            else if (filterApproved)
            {
                rescueQuery = rescueQuery.Where(c => c.Status != CaseStatus.PendingApproval);
            }

            var rescues = await rescueQuery
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommunityPostResponse(
                    c.Id, "RESCUE_REPORT", c.Title, c.Description,
                    storage.GetPublicUrl(c.PhotoKey),
                    c.Tags, c.Reporter.Name, c.ReporterId, c.CreatedAt,
                    c.Status == CaseStatus.PendingApproval ? "Pending" : "Approved"))
                .ToListAsync(ct);
            posts.AddRange(rescues);
        }

        if (type == null || type == "ADOPTION_LISTING")
        {
            var listingQuery = db.AnimalListings
                .AsNoTracking()
                .Include(l => l.Owner)
                .Where(l => l.IsActive);

            if (filterPending)
            {
                listingQuery = listingQuery.Where(l => l.Status == ListingStatus.Pending);
            }
            else if (filterApproved)
            {
                listingQuery = listingQuery.Where(l => l.Status != ListingStatus.Pending);
            }

            var listings = await listingQuery
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new CommunityPostResponse(
                    l.Id, "ADOPTION_LISTING", l.Title, l.Description,
                    l.Photos.Any() ? storage.GetPublicUrl(l.Photos.OrderBy(p => p.SortOrder).First().StorageKey) : null,
                    l.Tags, l.Owner.Name, l.OwnerId, l.CreatedAt,
                    l.Status == ListingStatus.Pending ? "Pending" : "Approved"))
                .ToListAsync(ct);
            posts.AddRange(listings);
        }

        if (type == null || type == "TRANSPORT_REQUEST")
        {
            var transportQuery = db.TransportTasks
                .AsNoTracking()
                .Include(t => t.Case).ThenInclude(c => c.Reporter)
                .AsQueryable();

            if (filterPending)
            {
                transportQuery = transportQuery.Where(t => t.Status == TransportStatus.Pending);
            }
            else if (filterApproved)
            {
                transportQuery = transportQuery.Where(t => t.Status != TransportStatus.Pending);
            }

            var transports = await transportQuery
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new CommunityPostResponse(
                    t.Id, "TRANSPORT_REQUEST", t.Title, t.SpecialInstructions ?? "",
                    storage.GetPublicUrl(t.PhotoKey),
                    t.Tags, t.Case.Reporter.Name, t.Case.ReporterId, t.CreatedAt,
                    t.Status == TransportStatus.Pending ? "Pending" : "Approved"))
                .ToListAsync(ct);
            posts.AddRange(transports);
        }

        if ((type == null || type == "COMMUNITY_STORY") && !filterPending)
        {
            var stories = await db.CommunityStories
                .AsNoTracking()
                .Include(s => s.Author)
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new CommunityPostResponse(
                    s.Id, "COMMUNITY_STORY", s.Title, s.Content,
                    s.PhotoKey != null ? storage.GetPublicUrl(s.PhotoKey) : null,
                    s.Tags, s.Author.Name, s.AuthorId, s.CreatedAt, "Approved"))
                .ToListAsync(ct);
            posts.AddRange(stories);
        }

        var sorted = posts.OrderByDescending(p => p.CreatedAt).ToList();
        var totalCount = sorted.Count;
        var paged = sorted.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        return TypedResults.Ok(new PagedResult<CommunityPostResponse>(paged, totalCount, query.Page, query.PageSize));
    }

    private static async Task<Ok<List<CommunityPostResponse>>> GetPendingCommunityPostsAsync(
        HappyPawsDbContext db,
        IStorageService storage,
        CancellationToken ct)
    {
        var posts = new List<CommunityPostResponse>();

        var rescues = await db.RescueCases
            .AsNoTracking()
            .Include(c => c.Reporter)
            .Where(c => c.IsActive && c.Status == CaseStatus.PendingApproval)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommunityPostResponse(
                c.Id, "RESCUE_REPORT", c.Title, c.Description,
                storage.GetPublicUrl(c.PhotoKey),
                c.Tags, c.Reporter.Name, c.ReporterId, c.CreatedAt, "Pending"))
            .ToListAsync(ct);
        posts.AddRange(rescues);

        var listings = await db.AnimalListings
            .AsNoTracking()
            .Include(l => l.Owner)
            .Where(l => l.IsActive && l.Status == ListingStatus.Pending)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new CommunityPostResponse(
                l.Id, "ADOPTION_LISTING", l.Title, l.Description,
                l.Photos.Any() ? storage.GetPublicUrl(l.Photos.OrderBy(p => p.SortOrder).First().StorageKey) : null,
                l.Tags, l.Owner.Name, l.OwnerId, l.CreatedAt, "Pending"))
            .ToListAsync(ct);
        posts.AddRange(listings);

        var transports = await db.TransportTasks
            .AsNoTracking()
            .Include(t => t.Case).ThenInclude(c => c.Reporter)
            .Where(t => t.Status == TransportStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new CommunityPostResponse(
                t.Id, "TRANSPORT_REQUEST", t.Title, t.SpecialInstructions ?? "",
                storage.GetPublicUrl(t.PhotoKey),
                t.Tags, t.Case.Reporter.Name, t.Case.ReporterId, t.CreatedAt, "Pending"))
            .ToListAsync(ct);
        posts.AddRange(transports);

        return TypedResults.Ok(posts.OrderByDescending(p => p.CreatedAt).ToList());
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> ApproveCommunityPostAsync(
        string type,
        Guid id,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        switch (type)
        {
            case "RESCUE_REPORT":
                var rescue = await db.RescueCases.FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);
                if (rescue == null) return TypedResults.NotFound();
                if (rescue.Status != CaseStatus.PendingApproval)
                    return TypedResults.BadRequest("Post is not pending approval.");
                rescue.Status = CaseStatus.Open;
                break;

            case "ADOPTION_LISTING":
                var listing = await db.AnimalListings.FirstOrDefaultAsync(l => l.Id == id && l.IsActive, ct);
                if (listing == null) return TypedResults.NotFound();
                if (listing.Status != ListingStatus.Pending)
                    return TypedResults.BadRequest("Post is not pending approval.");
                listing.Status = ListingStatus.Available;
                break;

            case "TRANSPORT_REQUEST":
                var transport = await db.TransportTasks.FirstOrDefaultAsync(t => t.Id == id, ct);
                if (transport == null) return TypedResults.NotFound();
                if (transport.Status != TransportStatus.Pending)
                    return TypedResults.BadRequest("Post is not pending approval.");
                transport.Status = TransportStatus.Assigned;
                break;

            default:
                return TypedResults.BadRequest($"Unknown content type: {type}");
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> RejectCommunityPostAsync(
        string type,
        Guid id,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        switch (type)
        {
            case "RESCUE_REPORT":
                var rescue = await db.RescueCases.FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);
                if (rescue == null) return TypedResults.NotFound();
                rescue.IsActive = false;
                rescue.UpdatedAt = DateTimeOffset.UtcNow;
                break;

            case "ADOPTION_LISTING":
                var listing = await db.AnimalListings.FirstOrDefaultAsync(l => l.Id == id && l.IsActive, ct);
                if (listing == null) return TypedResults.NotFound();
                listing.IsActive = false;
                listing.UpdatedAt = DateTimeOffset.UtcNow;
                break;

            case "TRANSPORT_REQUEST":
                var transport = await db.TransportTasks.FirstOrDefaultAsync(t => t.Id == id, ct);
                if (transport == null) return TypedResults.NotFound();
                return TypedResults.BadRequest("Transport requests cannot be rejected, only reassigned.");

            default:
                return TypedResults.BadRequest($"Unknown content type: {type}");
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> DeleteCommunityPostAsync(
        string type,
        Guid id,
        HappyPawsDbContext db,
        IStorageService storage,
        CancellationToken ct)
    {
        switch (type)
        {
            case "RESCUE_REPORT":
                var rescue = await db.RescueCases
                    .Include(c => c.TransportTasks)
                    .Include(c => c.CaseUpdates)
                    .Include(c => c.AnimalListings)
                    .FirstOrDefaultAsync(c => c.Id == id, ct);
                if (rescue == null) return TypedResults.NotFound();

                if (rescue.PhotoKey != null) await storage.DeleteAsync(rescue.PhotoKey, ct);
                
                foreach (var update in rescue.CaseUpdates)
                {
                    if (update.PhotoKey != null) await storage.DeleteAsync(update.PhotoKey, ct);
                }

                if (rescue.TransportTasks.Count > 0)
                {
                    foreach (var task in rescue.TransportTasks)
                    {
                        if (task.PhotoKey != null) await storage.DeleteAsync(task.PhotoKey, ct);
                    }
                    db.TransportTasks.RemoveRange(rescue.TransportTasks);
                }
                
                if (rescue.CaseUpdates.Count > 0)
                    db.CaseUpdates.RemoveRange(rescue.CaseUpdates);
                foreach (var listing in rescue.AnimalListings)
                {
                    listing.RescueCaseId = null;
                }
                db.RescueCases.Remove(rescue);
                break;

            case "ADOPTION_LISTING":
                var listingItem = await db.AnimalListings
                    .Include(l => l.Photos)
                    .FirstOrDefaultAsync(l => l.Id == id, ct);
                if (listingItem == null) return TypedResults.NotFound();
                
                foreach (var photo in listingItem.Photos)
                {
                    await storage.DeleteAsync(photo.StorageKey, ct);
                }
                
                var apps = await db.AdoptionApplications.Where(a => a.ListingId == id).ToListAsync(ct);
                db.AdoptionApplications.RemoveRange(apps);
                
                db.AnimalListings.Remove(listingItem);
                break;

            case "TRANSPORT_REQUEST":
                var transport = await db.TransportTasks.FirstOrDefaultAsync(t => t.Id == id, ct);
                if (transport == null) return TypedResults.NotFound();
                if (transport.PhotoKey != null) await storage.DeleteAsync(transport.PhotoKey, ct);
                db.TransportTasks.Remove(transport);
                break;

            case "COMMUNITY_STORY":
                var story = await db.CommunityStories.FirstOrDefaultAsync(s => s.Id == id, ct);
                if (story == null) return TypedResults.NotFound();
                if (story.PhotoKey != null) await storage.DeleteAsync(story.PhotoKey, ct);
                if (story.VideoKey != null) await storage.DeleteAsync(story.VideoKey, ct);
                db.CommunityStories.Remove(story);
                break;

            default:
                return TypedResults.BadRequest($"Unknown content type: {type}");
        }

        var upvotes = await db.PostUpvotes.Where(u => u.TargetId == id).ToListAsync(ct);
        db.PostUpvotes.RemoveRange(upvotes);

        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<CommunityPostDetailResponse>, NotFound, BadRequest<string>>> GetCommunityPostDetailAsync(
        string type,
        Guid id,
        HappyPawsDbContext db,
        IStorageService storage,
        CancellationToken ct)
    {
        switch (type)
        {
            case "RESCUE_REPORT":
            {
                var c = await db.RescueCases
                    .AsNoTracking()
                    .Include(r => r.Reporter)
                    .FirstOrDefaultAsync(r => r.Id == id, ct);
                if (c == null) return TypedResults.NotFound();

                return TypedResults.Ok(new CommunityPostDetailResponse(
                    c.Id, "RESCUE_REPORT", c.Title, c.Description,
                    storage.GetPublicUrl(c.PhotoKey), null, null,
                    c.Tags, c.Reporter.Name, c.ReporterId,
                    c.LocationName, c.LocationCoords.Y, c.LocationCoords.X,
                    c.Urgency.ToString(), c.UrgencySource.ToString(), c.OriginalAiUrgency?.ToString(),
                    c.Status.ToString(), c.ConditionNotes,
                    null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null,
                    c.CreatedAt, c.UpdatedAt));
            }

            case "ADOPTION_LISTING":
            {
                var l = await db.AnimalListings
                    .AsNoTracking()
                    .Include(x => x.Owner)
                    .Include(x => x.Photos.OrderBy(p => p.SortOrder))
                    .FirstOrDefaultAsync(x => x.Id == id, ct);
                if (l == null) return TypedResults.NotFound();

                var photos = l.Photos.Select(p => storage.GetPublicUrl(p.StorageKey)).ToList();

                return TypedResults.Ok(new CommunityPostDetailResponse(
                    l.Id, "ADOPTION_LISTING", l.Title, l.Description,
                    photos.Count > 0 ? photos[0] : null, null, photos,
                    l.Tags, l.Owner.Name, l.OwnerId,
                    l.LocationName, l.LocationCoords.Y, l.LocationCoords.X,
                    null, null, null,
                    l.Status.ToString(), null,
                    l.Name, l.Species, l.Breed, l.AgeMonths, l.AgeLabel, l.Gender.ToString(), l.Size.ToString(),
                    null, null, null, null, null, null, null, null,
                    l.CreatedAt, l.UpdatedAt));
            }

            case "TRANSPORT_REQUEST":
            {
                var t = await db.TransportTasks
                    .AsNoTracking()
                    .Include(x => x.Case).ThenInclude(c => c.Reporter)
                    .FirstOrDefaultAsync(x => x.Id == id, ct);
                if (t == null) return TypedResults.NotFound();

                return TypedResults.Ok(new CommunityPostDetailResponse(
                    t.Id, "TRANSPORT_REQUEST", t.Title, t.SpecialInstructions ?? "",
                    storage.GetPublicUrl(t.PhotoKey), null, null,
                    t.Tags, t.Case.Reporter.Name, t.Case.ReporterId,
                    t.PickupLocation, t.PickupLocationCoords.Y, t.PickupLocationCoords.X,
                    null, null, null,
                    t.Status.ToString(), null,
                    null, null, null, null, null, null, null,
                    t.DropoffLocation, t.DropoffLocationCoords.Y, t.DropoffLocationCoords.X,
                    t.PickupContactName, t.DropoffContactName, t.PickupTimeStart, t.PickupTimeEnd, null,
                    t.CreatedAt, t.UpdatedAt));
            }

            case "COMMUNITY_STORY":
            {
                var s = await db.CommunityStories
                    .AsNoTracking()
                    .Include(x => x.Author)
                    .FirstOrDefaultAsync(x => x.Id == id, ct);
                if (s == null) return TypedResults.NotFound();

                return TypedResults.Ok(new CommunityPostDetailResponse(
                    s.Id, "COMMUNITY_STORY", s.Title, s.Content,
                    s.PhotoKey != null ? storage.GetPublicUrl(s.PhotoKey) : null,
                    s.VideoKey != null ? storage.GetPublicUrl(s.VideoKey) : null,
                    null,
                    s.Tags, s.Author.Name, s.AuthorId,
                    null, null, null,
                    null, null, null,
                    "Active", null,
                    null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null,
                    s.CreatedAt, s.UpdatedAt));
            }

            default:
                return TypedResults.BadRequest($"Unknown content type: {type}");
        }
    }
}
