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
using NetTopologySuite.Geometries;

namespace HappyPaws.Api.Endpoints.Rescues;

public class RescuesEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", CreateRescueAsync)
            .RequireAuthorization("Verified")
            .DisableAntiforgery()
            .RequireRateLimiting("UploadLimiter")
            .AddEndpointFilter(new RequestSizeLimitFilter(10_485_760))
            .AddEndpointFilter<HtmlSanitizationFilter<CreateRescueRequest>>()
            .AddEndpointFilter<ValidationFilter<CreateRescueRequest>>()
            .WithName("CreateRescue")
            .WithSummary("Report a new animal rescue case with AI urgency classification")
            .WithDescription("Reports an animal in distress. Requires a photo (up to 10 MB). Gemini AI classifies urgency automatically. Nearby verified responders (Foster, Transporter, Veterinarian) within the configured alert radius receive a push notification.")
            .Produces<RescueCaseResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        group.MapGet("/", ListRescuesAsync)
            .WithName("ListRescues")
            .WithSummary("List active rescue cases with optional status and urgency filters")
            .WithDescription("Returns a paginated list of active rescue cases. Filter by status (Open, InProgress, Resolved) or urgency level.")
            .Produces<PagedResult<RescueCaseSummaryResponse>>();

        group.MapGet("/{id:guid}", GetRescueAsync)
            .WithName("GetRescue")
            .WithSummary("Get full details of a rescue case")
            .WithDescription("Returns the full details of a rescue case, including the reporter, assigned foster, coordinates, and AI urgency data.")
            .Produces<RescueCaseResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/accept", AcceptRescueAsync)
            .RequireAuthorization("Verified")
            .WithName("AcceptRescue")
            .WithSummary("Accept a rescue case as the assigned foster")
            .WithDescription("Assigns the authenticated foster to the case and moves its status to InProgress. A case can only be accepted once.")
            .Produces<RescueCaseResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/updates", PostUpdateAsync)
            .RequireAuthorization("Verified")
            .DisableAntiforgery()
            .AddEndpointFilter<HtmlSanitizationFilter<PostCaseUpdateRequest>>()
            .AddEndpointFilter<ValidationFilter<PostCaseUpdateRequest>>()
            .WithName("PostCaseUpdate")
            .WithSummary("Post a progress update on a rescue case")
            .WithDescription("Posts a progress update on a case. Veterinarians who post a MedicalGuidance update earn 10 reputation points. All updates notify the other involved party.")
            .Produces<CaseUpdateResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}/updates", GetUpdatesAsync)
            .WithName("GetCaseUpdates")
            .WithSummary("Get all updates for a rescue case")
            .WithDescription("Returns all progress updates for a case in chronological order.")
            .Produces<List<CaseUpdateResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/resolve", ResolveRescueAsync)
            .RequireAuthorization("Verified")
            .WithName("ResolveRescue")
            .WithSummary("Mark a rescue case as resolved")
            .WithDescription("Marks the case as Resolved. Only the assigned foster can do this. Awards 20 reputation points and evaluates badge eligibility.")
            .Produces<RescueCaseResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}/urgency", OverrideUrgencyAsync)
            .RequireAuthorization("Verified")
            .AddEndpointFilter<ValidationFilter<OverrideUrgencyRequest>>()
            .WithName("OverrideUrgency")
            .WithSummary("Override the AI-assigned urgency level (Admin/Vet only)")
            .WithDescription("Overrides the AI-assigned urgency level. Only Admins and Veterinarians can call this. Awards 10 reputation points to the original reporter.")
            .Produces<RescueCaseResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();
    }

    private static async Task<Results<Created<RescueCaseResponse>, BadRequest<string>>> CreateRescueAsync(
        IFormFile photo,
        [AsParameters] CreateRescueRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        IUrgencyClassificationService classificationService,
        ISystemConfigService configService,
        IServiceScopeFactory scopeFactory,
        ILogger<RescuesEndpoints> logger,
        CancellationToken ct)
    {
        if (photo.Length > 10 * 1024 * 1024)
            return TypedResults.BadRequest("Photo must not exceed 10MB");

        var userId = principal.GetUserId();
        var caseId = Guid.NewGuid();

        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        var key = $"rescues/{caseId}/{Guid.NewGuid()}{extension}";

        await using var uploadStream = photo.OpenReadStream();
        await storageService.UploadAsync(key, uploadStream, photo.ContentType, cancellationToken: ct);

        await using var classifyStream = photo.OpenReadStream();
        var classification = await classificationService.ClassifyAsync(classifyStream, ct);

        var rescueCase = new RescueCase
        {
            Id = caseId,
            Title = request.Title,
            Tags = request.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList() ?? [],
            ReporterId = userId,
            LocationCoords = new Point(request.Longitude, request.Latitude) { SRID = 4326 },
            LocationName = request.LocationName,
            Description = request.Description,
            PhotoKey = key,
            ConditionNotes = request.ConditionNotes,
            OriginalAiUrgency = classification.OriginalAiUrgency,
            UrgencySource = classification.Source,
            Urgency = classification.Urgency,
            Status = CaseStatus.PendingApproval,
            IsActive = true
        };

        db.RescueCases.Add(rescueCase);
        await db.SaveChangesAsync(ct);

        var caseLocation = rescueCase.LocationCoords;

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);
        var photoUrl = storageService.GetPublicUrl(key);

        var response = MapToResponse(rescueCase, user.Name, null, photoUrl);
        return TypedResults.Created($"/api/v1/rescues/{caseId}", response);
    }

    private static async Task<Ok<PagedResult<RescueCaseSummaryResponse>>> ListRescuesAsync(
        [AsParameters] PaginationQuery pagination,
        CaseStatus? status,
        Urgency? urgency,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var query = db.RescueCases
            .AsNoTracking()
            .Where(rc => rc.IsActive);

        if (status.HasValue)
            query = query.Where(rc => rc.Status == status.Value);

        if (urgency.HasValue)
            query = query.Where(rc => rc.Urgency == urgency.Value);

        var totalCount = await query.CountAsync(ct);

        var cases = await query
            .OrderByDescending(rc => rc.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(rc => new RescueCaseSummaryResponse(
                rc.Id,
                rc.Title,
                rc.LocationName,
                storageService.GetPublicUrl(rc.PhotoKey),
                rc.Urgency,
                rc.Status,
                rc.Tags.ToList(),
                rc.CreatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<RescueCaseSummaryResponse>(cases, totalCount, pagination.Page, pagination.PageSize));
    }

    private static async Task<Results<Ok<RescueCaseResponse>, NotFound>> GetRescueAsync(
        Guid id,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var rescueCase = await db.RescueCases
            .AsNoTracking()
            .Include(rc => rc.Reporter)
            .Include(rc => rc.AssignedFoster)
            .FirstOrDefaultAsync(rc => rc.Id == id, ct);

        if (rescueCase is null)
            return TypedResults.NotFound();

        var photoUrl = storageService.GetPublicUrl(rescueCase.PhotoKey);
        var response = MapToResponse(rescueCase, rescueCase.Reporter.Name, rescueCase.AssignedFoster?.Name, photoUrl);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<RescueCaseResponse>, NotFound, Conflict<string>, ForbidHttpResult>> AcceptRescueAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var roles = principal.GetRoles();

        if (!roles.Contains(Role.Foster.ToString()))
            return TypedResults.Forbid();

        var rescueCase = await db.RescueCases
            .Include(rc => rc.Reporter)
            .FirstOrDefaultAsync(rc => rc.Id == id && rc.IsActive, ct);

        if (rescueCase is null)
            return TypedResults.NotFound();

        if (rescueCase.AssignedFosterId is not null)
            return TypedResults.Conflict("This case has already been accepted by another foster");

        if (rescueCase.Status != CaseStatus.Open)
            return TypedResults.Conflict("This case is no longer open for acceptance");

        rescueCase.AssignedFosterId = userId;
        rescueCase.Status = CaseStatus.InProgress;
        await db.SaveChangesAsync(ct);

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);
        var photoUrl = storageService.GetPublicUrl(rescueCase.PhotoKey);
        var response = MapToResponse(rescueCase, rescueCase.Reporter.Name, user.Name, photoUrl);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created<CaseUpdateResponse>, NotFound, ForbidHttpResult, BadRequest<string>>> PostUpdateAsync(
        Guid id,
        [AsParameters] PostCaseUpdateRequest request,
        IFormFile? photo,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        INotificationService notificationService,
        IReputationService reputationService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var rescueCase = await db.RescueCases
            .AsNoTracking()
            .FirstOrDefaultAsync(rc => rc.Id == id && rc.IsActive, ct);

        if (rescueCase is null)
            return TypedResults.NotFound();

        if (rescueCase.ReporterId != userId && rescueCase.AssignedFosterId != userId)
            return TypedResults.Forbid();

        string? photoKey = null;
        if (photo is not null)
        {
            if (photo.Length > 10 * 1024 * 1024)
                return TypedResults.BadRequest("Photo must not exceed 10MB");

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            photoKey = $"rescues/{id}/updates/{Guid.NewGuid()}{extension}";

            await using var stream = photo.OpenReadStream();
            await storageService.UploadAsync(photoKey, stream, photo.ContentType, cancellationToken: ct);
        }

        var caseUpdate = new CaseUpdate
        {
            Id = Guid.NewGuid(),
            CaseId = id,
            UserId = userId,
            UpdateType = request.UpdateType,
            UpdateText = request.UpdateText,
            PhotoKey = photoKey
        };

        db.CaseUpdates.Add(caseUpdate);
        await db.SaveChangesAsync(ct);

        if (request.UpdateType == UpdateType.MedicalGuidance && principal.GetRoles().Contains(Role.Veterinarian.ToString()))
        {
            await reputationService.AwardPointsAsync(userId, "MedicalGuidance", 10, id, "RescueCase", ct);
        }

        var involvedUserIds = new List<Guid> { rescueCase.ReporterId };
        if (rescueCase.AssignedFosterId.HasValue) involvedUserIds.Add(rescueCase.AssignedFosterId.Value);
        involvedUserIds.Remove(userId);

        if (involvedUserIds.Count > 0)
        {
            await notificationService.SendNotificationsAsync(
                involvedUserIds,
                "case_update",
                "New Case Update",
                "A new update was posted on the case.",
                id,
                "RescueCase",
                new Dictionary<string, string> { ["caseId"] = id.ToString() },
                ct);
        }

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);
        var photoUrl = photoKey is not null ? storageService.GetPublicUrl(photoKey) : null;

        var response = new CaseUpdateResponse(
            caseUpdate.Id,
            userId,
            user.Name,
            caseUpdate.UpdateType,
            caseUpdate.UpdateText,
            photoUrl,
            caseUpdate.CreatedAt);

        return TypedResults.Created($"/api/v1/rescues/{id}/updates", response);
    }

    private static async Task<Results<Ok<List<CaseUpdateResponse>>, NotFound>> GetUpdatesAsync(
        Guid id,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var exists = await db.RescueCases.AsNoTracking().AnyAsync(rc => rc.Id == id, ct);
        if (!exists)
            return TypedResults.NotFound();

        var updates = await db.CaseUpdates
            .AsNoTracking()
            .Where(u => u.CaseId == id)
            .Include(u => u.User)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new CaseUpdateResponse(
                u.Id,
                u.UserId,
                u.User.Name,
                u.UpdateType,
                u.UpdateText,
                u.PhotoKey != null ? storageService.GetPublicUrl(u.PhotoKey) : null,
                u.CreatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(updates);
    }

    private static async Task<Results<Ok<RescueCaseResponse>, NotFound, Conflict<string>, ForbidHttpResult>> ResolveRescueAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        IReputationService reputationService,
        IBadgeEvaluationService badgeEvaluationService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var rescueCase = await db.RescueCases
            .Include(rc => rc.Reporter)
            .Include(rc => rc.AssignedFoster)
            .FirstOrDefaultAsync(rc => rc.Id == id && rc.IsActive, ct);

        if (rescueCase is null)
            return TypedResults.NotFound();

        if (rescueCase.AssignedFosterId != userId)
            return TypedResults.Forbid();

        if (rescueCase.Status != CaseStatus.InProgress)
            return TypedResults.Conflict("Only in-progress cases can be resolved.");

        rescueCase.Status = CaseStatus.Resolved;
        await db.SaveChangesAsync(ct);

        await reputationService.AwardPointsAsync(
            userId, "FosterPlacementCompleted", 20, rescueCase.Id, "RescueCase", ct);

        await badgeEvaluationService.EvaluateAndAwardBadgesAsync(userId, ct);

        var photoUrl = storageService.GetPublicUrl(rescueCase.PhotoKey);
        var response = MapToResponse(rescueCase, rescueCase.Reporter.Name, rescueCase.AssignedFoster?.Name, photoUrl);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<RescueCaseResponse>, NotFound, ForbidHttpResult>> OverrideUrgencyAsync(
        Guid id,
        OverrideUrgencyRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        IReputationService reputationService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var roles = principal.GetRoles();

        if (!roles.Contains(Role.Admin.ToString()) && !roles.Contains(Role.Veterinarian.ToString()))
            return TypedResults.Forbid();

        var rescueCase = await db.RescueCases
            .Include(rc => rc.Reporter)
            .Include(rc => rc.AssignedFoster)
            .FirstOrDefaultAsync(rc => rc.Id == id && rc.IsActive, ct);

        if (rescueCase is null)
            return TypedResults.NotFound();

        rescueCase.Urgency = request.Urgency;
        rescueCase.UrgencySource = UrgencySource.ManualOverride;
        rescueCase.UrgencyOverriddenById = userId;
        await db.SaveChangesAsync(ct);

        await reputationService.AwardPointsAsync(rescueCase.ReporterId, "RescueReportVerified", 10, rescueCase.Id, "RescueCase", ct);

        var photoUrl = storageService.GetPublicUrl(rescueCase.PhotoKey);
        var response = MapToResponse(rescueCase, rescueCase.Reporter.Name, rescueCase.AssignedFoster?.Name, photoUrl);
        return TypedResults.Ok(response);
    }

    private static RescueCaseResponse MapToResponse(RescueCase rc, string reporterName, string? fosterName, string photoUrl)
    {
        return new RescueCaseResponse(
            rc.Id,
            rc.Title,
            rc.ReporterId,
            reporterName,
            rc.AssignedFosterId,
            fosterName,
            rc.LocationCoords.Y,
            rc.LocationCoords.X,
            rc.LocationName,
            rc.Description,
            photoUrl,
            rc.ConditionNotes,
            rc.Urgency,
            rc.OriginalAiUrgency,
            rc.UrgencySource,
            rc.Status,
            rc.Tags.ToList(),
            rc.CreatedAt,
            rc.UpdatedAt);
    }
}
