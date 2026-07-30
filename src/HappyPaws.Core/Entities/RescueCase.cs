using HappyPaws.Core.Enums;
using NetTopologySuite.Geometries;

namespace HappyPaws.Core.Entities;

public class RescueCase
{
    public Guid Id { get; set; }
    public Guid ReporterId { get; set; }
    public Guid? AssignedFosterId { get; set; }
    public Guid? UrgencyOverriddenById { get; set; }
    public Point LocationCoords { get; set; } = null!;
    public string LocationName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string PhotoKey { get; set; } = null!;
    public string? ConditionNotes { get; set; }
    public Urgency? OriginalAiUrgency { get; set; }
    public UrgencySource UrgencySource { get; set; }
    public Urgency Urgency { get; set; }
    public CaseStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User Reporter { get; set; } = null!;
    public User? AssignedFoster { get; set; }
    public User? UrgencyOverriddenBy { get; set; }
    public ICollection<CaseUpdate> CaseUpdates { get; set; } = [];
    public ICollection<AnimalListing> AnimalListings { get; set; } = [];
    public ICollection<TransportTask> TransportTasks { get; set; } = [];
    public ICollection<Pledge> Pledges { get; set; } = [];
    public ICollection<Conversation> Conversations { get; set; } = [];
}
