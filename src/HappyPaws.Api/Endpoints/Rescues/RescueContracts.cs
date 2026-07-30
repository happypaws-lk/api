using HappyPaws.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Endpoints.Rescues;

public record CreateRescueRequest(
    [FromForm] double Latitude,
    [FromForm] double Longitude,
    [FromForm] string LocationName,
    [FromForm] string Description,
    [FromForm] string? ConditionNotes);

public record PostCaseUpdateRequest(
    [FromForm] UpdateType UpdateType,
    [FromForm] string UpdateText);

public record OverrideUrgencyRequest(Urgency Urgency);

public record RescueCaseResponse(
    Guid Id,
    Guid ReporterId,
    string ReporterName,
    Guid? AssignedFosterId,
    string? AssignedFosterName,
    double Latitude,
    double Longitude,
    string LocationName,
    string Description,
    string PhotoUrl,
    string? ConditionNotes,
    Urgency Urgency,
    Urgency? OriginalAiUrgency,
    UrgencySource UrgencySource,
    CaseStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record RescueCaseSummaryResponse(
    Guid Id,
    string LocationName,
    string PhotoUrl,
    Urgency Urgency,
    CaseStatus Status,
    DateTimeOffset CreatedAt);

public record CaseUpdateResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    UpdateType UpdateType,
    string UpdateText,
    string? PhotoUrl,
    DateTimeOffset CreatedAt);
