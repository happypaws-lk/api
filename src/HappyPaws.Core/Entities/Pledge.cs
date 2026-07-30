using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Entities;

public class Pledge
{
    public Guid Id { get; set; }
    public Guid SponsorId { get; set; }
    public Guid? CaseId { get; set; }
    public Guid? ListingId { get; set; }
    public decimal Amount { get; set; }
    public PledgeStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User Sponsor { get; set; } = null!;
    public RescueCase? Case { get; set; }
    public AnimalListing? Listing { get; set; }
}
