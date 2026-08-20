using HappyPaws.Core.Enums;
using NetTopologySuite.Geometries;

namespace HappyPaws.Core.Entities;

public class TransportTask
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid? TransporterId { get; set; }
    public string Title { get; set; } = null!;
    public string PhotoKey { get; set; } = null!;
    public string? SpecialInstructions { get; set; }
    public DateTimeOffset? PickupTimeStart { get; set; }
    public DateTimeOffset? PickupTimeEnd { get; set; }
    public string? PickupContactName { get; set; }
    public string? DropoffContactName { get; set; }
    public List<string> Tags { get; set; } = [];
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
