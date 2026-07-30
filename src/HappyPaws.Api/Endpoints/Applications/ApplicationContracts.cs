using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Applications;

public record CreateApplicationRequest(Guid ListingId);

public record ApplicationResponse(
    Guid Id,
    Guid ListingId,
    string ListingName,
    Guid ApplicantId,
    string ApplicantName,
    ApplicationStatus Status,
    string? ReviewNotes,
    DateTimeOffset AppliedAt,
    DateTimeOffset UpdatedAt);
