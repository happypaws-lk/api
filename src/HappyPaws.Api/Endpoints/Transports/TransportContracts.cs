using HappyPaws.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyPaws.Api.Endpoints.Transports;

/// <summary>Data submitted when creating a transport task for a rescue case.</summary>
/// <param name="CaseId">ID of the active rescue case this transport supports.</param>
/// <param name="PickupLatitude">WGS84 latitude of the pickup location.</param>
/// <param name="PickupLongitude">WGS84 longitude of the pickup location.</param>
/// <param name="PickupLocation">Human-readable name of the pickup location.</param>
/// <param name="DropoffLatitude">WGS84 latitude of the drop-off location.</param>
/// <param name="DropoffLongitude">WGS84 longitude of the drop-off location.</param>
/// <param name="DropoffLocation">Human-readable name of the drop-off location.</param>
public record CreateTransportRequest(
    [FromForm] Guid CaseId,
    [FromForm] string Title,
    [FromForm] double PickupLatitude,
    [FromForm] double PickupLongitude,
    [FromForm] string PickupLocation,
    [FromForm] double DropoffLatitude,
    [FromForm] double DropoffLongitude,
    [FromForm] string DropoffLocation,
    [FromForm] string? SpecialInstructions,
    [FromForm] DateTimeOffset? PickupTimeStart,
    [FromForm] DateTimeOffset? PickupTimeEnd,
    [FromForm] string? PickupContactName,
    [FromForm] string? DropoffContactName,
    [FromForm] string? Tags
);

/// <summary>A transport task for moving a rescued animal between locations.</summary>
/// <param name="Id">Unique identifier of the transport task.</param>
/// <param name="CaseId">ID of the rescue case this task supports.</param>
/// <param name="TransporterId">ID of the user who claimed this task. Null if unclaimed.</param>
/// <param name="TransporterName">Display name of the transporter. Empty string if unclaimed.</param>
/// <param name="PickupLatitude">WGS84 latitude of the pickup location.</param>
/// <param name="PickupLongitude">WGS84 longitude of the pickup location.</param>
/// <param name="PickupLocation">Human-readable name of the pickup location.</param>
/// <param name="DropoffLatitude">WGS84 latitude of the drop-off location.</param>
/// <param name="DropoffLongitude">WGS84 longitude of the drop-off location.</param>
/// <param name="DropoffLocation">Human-readable name of the drop-off location.</param>
/// <param name="Status">The current status of the transport task.</param>
/// <param name="CreatedAt">UTC timestamp when the task was created.</param>
/// <param name="UpdatedAt">UTC timestamp of the last status change.</param>
public record TransportTaskResponse(
    Guid Id,
    Guid CaseId,
    string Title,
    string PhotoUrl,
    Guid? TransporterId,
    string TransporterName,
    double PickupLatitude,
    double PickupLongitude,
    string PickupLocation,
    double DropoffLatitude,
    double DropoffLongitude,
    string DropoffLocation,
    string? SpecialInstructions,
    DateTimeOffset? PickupTimeStart,
    DateTimeOffset? PickupTimeEnd,
    string? PickupContactName,
    string? DropoffContactName,
    List<string> Tags,
    TransportStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

/// <summary>Data submitted when advancing a transport task to its next status.</summary>
/// <param name="Status">The new status. Must be exactly one step forward from the current status.</param>
public record TransportStatusUpdateRequest(
    TransportStatus Status
);
