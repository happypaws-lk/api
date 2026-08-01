using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Pledges;

/// <summary>Data submitted when creating a financial pledge.</summary>
/// <param name="CaseId">ID of the rescue case to pledge to. Mutually exclusive with ListingId. Optional.</param>
/// <param name="ListingId">ID of the listing to pledge to. Mutually exclusive with CaseId. Optional.</param>
/// <param name="Amount">The pledge amount in the platform currency.</param>
/// <param name="Note">An optional personal message attached to the pledge.</param>
public record CreatePledgeRequest(
    Guid? CaseId,
    Guid? ListingId,
    decimal Amount,
    string? Note);

/// <summary>A financial pledge made by a sponsor.</summary>
/// <param name="Id">Unique identifier of the pledge.</param>
/// <param name="SponsorId">ID of the user who made the pledge.</param>
/// <param name="SponsorName">Display name of the sponsor.</param>
/// <param name="CaseId">ID of the rescue case this pledge supports. Null if the pledge is for a listing.</param>
/// <param name="ListingId">ID of the listing this pledge supports. Null if the pledge is for a rescue case.</param>
/// <param name="Amount">The pledged amount.</param>
/// <param name="Status">The current status of the pledge.</param>
/// <param name="Note">The optional personal message attached to the pledge.</param>
/// <param name="CreatedAt">UTC timestamp when the pledge was created.</param>
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
