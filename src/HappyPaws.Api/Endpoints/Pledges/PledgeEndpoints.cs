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

namespace HappyPaws.Api.Endpoints.Pledges;

public class PledgeEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", CreatePledgeAsync)
            .RequireAuthorization("Verified")
            .AddEndpointFilter<ValidationFilter<CreatePledgeRequest>>()
            .WithName("CreatePledge")
            .WithSummary("Create a financial pledge for a rescue case or listing");

        group.MapGet("/me", GetMyPledgesAsync)
            .RequireAuthorization()
            .WithName("GetMyPledges")
            .WithSummary("List all pledges made by the authenticated user");
    }

    private static async Task<Results<Created<PledgeResponse>, NotFound<string>, ForbidHttpResult>> CreatePledgeAsync(
        CreatePledgeRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IReputationService reputationService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var roles = principal.GetRoles();

        if (!roles.Contains(Role.Sponsor.ToString()))
            return TypedResults.Forbid();

        if (request.CaseId.HasValue)
        {
            var rescueCase = await db.RescueCases.FirstOrDefaultAsync(rc => rc.Id == request.CaseId && rc.IsActive, ct);
            if (rescueCase is null)
                return TypedResults.NotFound("Rescue case not found.");
        }
        else if (request.ListingId.HasValue)
        {
            var listing = await db.AnimalListings.FirstOrDefaultAsync(l => l.Id == request.ListingId && l.IsActive, ct);
            if (listing is null)
                return TypedResults.NotFound("Listing not found.");
        }

        var pledge = new Pledge
        {
            Id = Guid.NewGuid(),
            SponsorId = userId,
            CaseId = request.CaseId,
            ListingId = request.ListingId,
            Amount = request.Amount,
            Status = PledgeStatus.Confirmed,
            Note = request.Note,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Pledges.Add(pledge);
        await db.SaveChangesAsync(ct);

        await reputationService.AwardPointsAsync(userId, "PledgeConfirmed", 5, pledge.Id, "Pledge", ct);

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);

        var response = new PledgeResponse(
            pledge.Id, pledge.SponsorId, user.Name, pledge.CaseId, pledge.ListingId,
            pledge.Amount, pledge.Status, pledge.Note, pledge.CreatedAt);

        return TypedResults.Created($"/api/v1/pledges/{pledge.Id}", response);
    }

    private static async Task<Ok<PagedResult<PledgeResponse>>> GetMyPledgesAsync(
        [AsParameters] PaginationQuery pagination,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var query = db.Pledges
            .AsNoTracking()
            .Include(p => p.Sponsor)
            .Where(p => p.SponsorId == userId);

        var totalCount = await query.CountAsync(ct);

        var pledges = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(p => new PledgeResponse(
                p.Id, p.SponsorId, p.Sponsor.Name, p.CaseId, p.ListingId,
                p.Amount, p.Status, p.Note, p.CreatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<PledgeResponse>(pledges, totalCount, pagination.Page, pagination.PageSize));
    }
}
