using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Pledges;

public record CreatePledgeRequest(
    Guid? CaseId,
    Guid? ListingId,
    decimal Amount,
    string? Note);

public record PledgeResponse(
    Guid Id,
    Guid SponsorId,
    string SponsorName,
    Guid? CaseId,
    Guid? ListingId,
    decimal Amount,
    PledgeStatus Status,
    string? Note,
    DateTimeOffset CreatedAt);
