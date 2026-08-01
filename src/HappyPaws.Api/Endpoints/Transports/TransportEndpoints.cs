using System.Security.Claims;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Filters;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace HappyPaws.Api.Endpoints.Transports;

public class TransportsEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", CreateTransportAsync)
            .RequireAuthorization("Verified")
            .AddEndpointFilter<ValidationFilter<CreateTransportRequest>>()
            .WithName("CreateTransport")
            .WithSummary("Create a transport task for a rescue case")
            .WithDescription("Creates a transport task for an active rescue case. Only users with the Foster role can create transport tasks.")
            .Produces<TransportTaskResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        group.MapGet("/", ListTransportsAsync)
            .RequireAuthorization()
            .WithName("ListTransports")
            .WithSummary("List all pending transport tasks available to claim")
            .WithDescription("Returns all pending transport tasks available to claim.")
            .Produces<List<TransportTaskResponse>>();

        group.MapPost("/{id:guid}/claim", ClaimTransportAsync)
            .RequireAuthorization("Verified")
            .WithName("ClaimTransport")
            .WithSummary("Claim a pending transport task")
            .WithDescription("Claims a pending transport task. Only users with the Transporter role can claim tasks. Claiming an already-assigned task returns 200 if the caller owns it, or 409 otherwise.")
            .Produces<TransportTaskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}/status", UpdateStatusAsync)
            .RequireAuthorization("Verified")
            .AddEndpointFilter<ValidationFilter<TransportStatusUpdateRequest>>()
            .WithName("UpdateTransportStatus")
            .WithSummary("Update the status of a claimed transport task")
            .WithDescription("Advances a claimed task through its lifecycle (Assigned, PickedUp, InTransit, Delivered). Status must advance exactly one step at a time. Awards 10 reputation points on delivery.")
            .Produces<TransportTaskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();
    }

    private static async Task<Results<Created<TransportTaskResponse>, NotFound<string>, ForbidHttpResult>> CreateTransportAsync(
        CreateTransportRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var roles = principal.GetRoles();

        if (!roles.Contains(Role.Foster.ToString()))
            return TypedResults.Forbid();

        var rescueCase = await db.RescueCases.FirstOrDefaultAsync(rc => rc.Id == request.CaseId && rc.IsActive, ct);
        if (rescueCase is null)
            return TypedResults.NotFound("Rescue case not found.");

        var task = new TransportTask
        {
            Id = Guid.NewGuid(),
            CaseId = request.CaseId,
            PickupLocationCoords = new Point(request.PickupLongitude, request.PickupLatitude) { SRID = 4326 },
            PickupLocation = request.PickupLocation,
            DropoffLocationCoords = new Point(request.DropoffLongitude, request.DropoffLatitude) { SRID = 4326 },
            DropoffLocation = request.DropoffLocation,
            Status = TransportStatus.Pending
        };

        db.TransportTasks.Add(task);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/v1/transports/{task.Id}", MapToResponse(task, null));
    }

    private static async Task<Ok<List<TransportTaskResponse>>> ListTransportsAsync(
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var tasks = await db.TransportTasks
            .AsNoTracking()
            .Where(t => t.Status == TransportStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return TypedResults.Ok(tasks.Select(t => MapToResponse(t, null)).ToList());
    }

    private static async Task<Results<Ok<TransportTaskResponse>, NotFound, Conflict<string>, ForbidHttpResult>> ClaimTransportAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var roles = principal.GetRoles();

        if (!roles.Contains(Role.Transporter.ToString()))
            return TypedResults.Forbid();

        var task = await db.TransportTasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
            return TypedResults.NotFound();

        if (task.Status != TransportStatus.Pending)
        {
            if (task.TransporterId == userId)
                return TypedResults.Ok(MapToResponse(task, principal.Identity?.Name));
            return TypedResults.Conflict("Task is already claimed.");
        }

        task.TransporterId = userId;
        task.Status = TransportStatus.Assigned;
        await db.SaveChangesAsync(ct);

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);
        return TypedResults.Ok(MapToResponse(task, user.Name));
    }

    private static async Task<Results<Ok<TransportTaskResponse>, NotFound, Conflict<string>, ForbidHttpResult>> UpdateStatusAsync(
        Guid id,
        TransportStatusUpdateRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IReputationService reputationService,
        IBadgeEvaluationService badgeEvaluationService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var task = await db.TransportTasks
            .Include(t => t.Transporter)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (task is null)
            return TypedResults.NotFound();

        if (task.TransporterId != userId)
            return TypedResults.Forbid();

        if (request.Status <= task.Status || request.Status > task.Status + 1)
            return TypedResults.Conflict("Invalid status progression.");

        task.Status = request.Status;
        await db.SaveChangesAsync(ct);

        if (task.Status == TransportStatus.Delivered)
        {
            await reputationService.AwardPointsAsync(userId, "TransportDelivered", 10, task.Id, "TransportTask", ct);
            await badgeEvaluationService.EvaluateAndAwardBadgesAsync(userId, ct);
        }

        return TypedResults.Ok(MapToResponse(task, task.Transporter?.Name));
    }

    private static TransportTaskResponse MapToResponse(TransportTask t, string? transporterName)
    {
        return new TransportTaskResponse(
            t.Id,
            t.CaseId,
            t.TransporterId,
            transporterName ?? string.Empty,
            t.PickupLocationCoords.Y,
            t.PickupLocationCoords.X,
            t.PickupLocation,
            t.DropoffLocationCoords.Y,
            t.DropoffLocationCoords.X,
            t.DropoffLocation,
            t.Status,
            t.CreatedAt,
            t.UpdatedAt);
    }
}
