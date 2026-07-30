using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Transports;

public record CreateTransportRequest(
    Guid CaseId,
    double PickupLatitude,
    double PickupLongitude,
    string PickupLocation,
    double DropoffLatitude,
    double DropoffLongitude,
    string DropoffLocation
);

public record TransportTaskResponse(
    Guid Id,
    Guid CaseId,
    Guid? TransporterId,
    string TransporterName,
    double PickupLatitude,
    double PickupLongitude,
    string PickupLocation,
    double DropoffLatitude,
    double DropoffLongitude,
    string DropoffLocation,
    TransportStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record TransportStatusUpdateRequest(
    TransportStatus Status
);
