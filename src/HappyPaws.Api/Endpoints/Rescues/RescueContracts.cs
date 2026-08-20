using HappyPaws.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Endpoints.Rescues;

/// <summary>Form data submitted when reporting a new rescue case. Send as multipart/form-data alongside the photo file.</summary>
/// <param name="Latitude">WGS84 latitude of the rescue location.</param>
/// <param name="Longitude">WGS84 longitude of the rescue location.</param>
/// <param name="LocationName">Human-readable name of the rescue location.</param>
/// <param name="Description">A description of the situation and the animal's condition.</param>
/// <param name="ConditionNotes">Optional additional notes about the animal's medical condition.</param>
public record CreateRescueRequest(
    [FromForm] string Title,
    [FromForm] double Latitude,
    [FromForm] double Longitude,
    [FromForm] string LocationName,
    [FromForm] string Description,
    [FromForm] string? ConditionNotes,
    [FromForm] string? Tags);

/// <summary>Form data submitted when posting a progress update on a rescue case. Send as multipart/form-data and optionally include a photo file.</summary>
/// <param name="UpdateType">The category of update being posted.</param>
/// <param name="UpdateText">The update message text.</param>
public record PostCaseUpdateRequest(
    [FromForm] UpdateType UpdateType,
    [FromForm] string UpdateText);

/// <summary>Data submitted when overriding the AI-assigned urgency level.</summary>
/// <param name="Urgency">The new urgency level to assign to the case.</param>
public record OverrideUrgencyRequest(Urgency Urgency);

/// <summary>Full details of a rescue case.</summary>
/// <param name="Id">Unique identifier of the rescue case.</param>
/// <param name="ReporterId">ID of the user who reported the case.</param>
/// <param name="ReporterName">Display name of the reporter.</param>
/// <param name="AssignedFosterId">ID of the foster assigned to this case. Null if unassigned.</param>
/// <param name="AssignedFosterName">Display name of the assigned foster. Null if unassigned.</param>
/// <param name="Latitude">WGS84 latitude of the rescue location.</param>
/// <param name="Longitude">WGS84 longitude of the rescue location.</param>
/// <param name="LocationName">Human-readable name of the rescue location.</param>
/// <param name="Description">Description of the situation.</param>
/// <param name="PhotoUrl">Public URL of the case photo.</param>
/// <param name="ConditionNotes">Additional notes about the animal's condition. Null if not provided.</param>
/// <param name="Urgency">The current urgency level (may have been overridden from the AI classification).</param>
/// <param name="OriginalAiUrgency">The urgency level originally assigned by the AI. Null if urgency was set manually from the start.</param>
/// <param name="UrgencySource">Indicates whether urgency was set by AI or a manual override.</param>
/// <param name="Status">The current case status.</param>
/// <param name="CreatedAt">UTC timestamp when the case was reported.</param>
/// <param name="UpdatedAt">UTC timestamp of the last update.</param>
public record RescueCaseResponse(
    Guid Id,
    string Title,
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
    List<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>A compact rescue case entry used in list views and the map.</summary>
/// <param name="Id">Unique identifier of the rescue case.</param>
/// <param name="LocationName">Human-readable name of the rescue location.</param>
/// <param name="PhotoUrl">Public URL of the case photo.</param>
/// <param name="Urgency">The current urgency level.</param>
/// <param name="Status">The current case status.</param>
/// <param name="CreatedAt">UTC timestamp when the case was reported.</param>
public record RescueCaseSummaryResponse(
    Guid Id,
    string Title,
    string LocationName,
    string PhotoUrl,
    Urgency Urgency,
    CaseStatus Status,
    List<string> Tags,
    DateTimeOffset CreatedAt);

/// <summary>A progress update posted on a rescue case.</summary>
/// <param name="Id">Unique identifier of the update.</param>
/// <param name="UserId">ID of the user who posted the update.</param>
/// <param name="UserName">Display name of the user who posted the update.</param>
/// <param name="UpdateType">The category of update.</param>
/// <param name="UpdateText">The update message text.</param>
/// <param name="PhotoUrl">Public URL of an attached photo. Null if no photo was attached.</param>
/// <param name="CreatedAt">UTC timestamp when the update was posted.</param>
public record CaseUpdateResponse(
    Guid Id,
    Guid UserId,
    string UserName,
    UpdateType UpdateType,
    string UpdateText,
    string? PhotoUrl,
    DateTimeOffset CreatedAt);
