using System.Security.Claims;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Filters;
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
            .WithSummary("Get admin dashboard statistics");

        group.MapGet("/cases", GetCasesAsync)
            .WithName("GetAdminCases")
            .WithSummary("Get all active rescue cases for the live map");

        group.MapGet("/users", GetUsersAsync)
            .WithName("GetAdminUsers")
            .WithSummary("Get a paginated list of users");

        group.MapPut("/users/{id:guid}/suspend", SuspendUserAsync)
            .AddEndpointFilter<ValidationFilter<SuspendRequest>>()
            .WithName("SuspendUser")
            .WithSummary("Suspend a user");

        group.MapPut("/users/{id:guid}/unsuspend", UnsuspendUserAsync)
            .WithName("UnsuspendUser")
            .WithSummary("Unsuspend a user");

        group.MapPost("/moderation", CreateModerationActionAsync)
            .AddEndpointFilter<ValidationFilter<ModerationRequest>>()
            .WithName("CreateModerationAction")
            .WithSummary("Perform a moderation action");

        group.MapGet("/moderation", GetModerationLogAsync)
            .WithName("GetModerationLog")
            .WithSummary("Get paginated moderation log");

        group.MapPut("/reputation/{userId:guid}", AdjustReputationAsync)
            .AddEndpointFilter<ValidationFilter<ReputationAdjustRequest>>()
            .WithName("AdjustReputation")
            .WithSummary("Adjust a user's reputation points directly");

        group.MapGet("/kyc/pending", GetPendingKycAsync)
            .WithName("GetPendingKyc")
            .WithSummary("List all pending KYC documents for review");

        group.MapPost("/kyc/{id:guid}/approve", ApproveKycAsync)
            .WithName("ApproveKyc")
            .WithSummary("Approve a KYC document and verify the user");

        group.MapPost("/kyc/{id:guid}/reject", RejectKycAsync)
            .AddEndpointFilter<ValidationFilter<KycRejectRequest>>()
            .WithName("RejectKyc")
            .WithSummary("Reject a KYC document with a reason");
    }

    private static async Task<Ok<DashboardResponse>> GetDashboardAsync(
        HappyPawsDbContext db,
        CancellationToken ct)
    {
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

        return TypedResults.Ok(new DashboardResponse(
            pendingKycCount,
            openCasesCount,
            totalUsersCount,
            recentActivity));
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
            u.Roles.Select(r => r.Role.ToString()).ToList()));

        return TypedResults.Ok(new PagedResult<AdminUserResponse>(items, totalCount, query.Page, query.PageSize));
    }

    private static async Task<Results<NoContent, NotFound>> SuspendUserAsync(
        Guid id,
        SuspendRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var adminId = principal.GetUserId();

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
}
