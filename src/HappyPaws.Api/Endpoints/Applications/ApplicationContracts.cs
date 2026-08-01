using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Applications;

/// <summary>Data submitted when applying for an adoption listing.</summary>
/// <param name="ListingId">ID of the listing to apply for.</param>
public record CreateApplicationRequest(Guid ListingId);

/// <summary>An adoption application and its current review state.</summary>
/// <param name="Id">Unique identifier of the application.</param>
/// <param name="ListingId">ID of the listing this application is for.</param>
/// <param name="ListingName">Name of the animal being applied for.</param>
/// <param name="ApplicantId">ID of the user who submitted the application.</param>
/// <param name="ApplicantName">Display name of the applicant.</param>
/// <param name="Status">The current status of the application.</param>
/// <param name="ReviewNotes">Notes left by the listing owner during review. Null if not yet reviewed.</param>
/// <param name="AppliedAt">UTC timestamp when the application was submitted.</param>
/// <param name="UpdatedAt">UTC timestamp of the last status change.</param>
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
