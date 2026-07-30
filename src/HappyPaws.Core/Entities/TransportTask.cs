using HappyPaws.Core.Enums;
using NetTopologySuite.Geometries;

namespace HappyPaws.Core.Entities;

public class TransportTask
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid? TransporterId { get; set; }
    public Point PickupLocationCoords { get; set; } = null!;
    public string PickupLocation { get; set; } = null!;
    public Point DropoffLocationCoords { get; set; } = null!;
    public string DropoffLocation { get; set; } = null!;
    public TransportStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public RescueCase Case { get; set; } = null!;
    public User? Transporter { get; set; }
}
