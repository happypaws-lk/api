using HappyPaws.Core.Enums;
using NetTopologySuite.Geometries;

namespace HappyPaws.Core.Entities;

public class AnimalListing
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? RescueCaseId { get; set; }
    public string Title { get; set; } = null!;
    public List<string> Tags { get; set; } = [];
    public string Name { get; set; } = null!;
    public string Species { get; set; } = null!;
    public string Breed { get; set; } = null!;
    public int AgeMonths { get; set; }
    public string? AgeLabel { get; set; }
    public Gender Gender { get; set; }
    public AnimalSize Size { get; set; }
    public ActivityLevel ActivityLevel { get; set; }
    public string Description { get; set; } = null!;
    public Point LocationCoords { get; set; } = null!;
    public string LocationName { get; set; } = null!;
    public ListingStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User Owner { get; set; } = null!;
    public RescueCase? RescueCase { get; set; }
    public ICollection<ListingPhoto> Photos { get; set; } = [];
    public ICollection<AdoptionApplication> Applications { get; set; } = [];
    public ICollection<Pledge> Pledges { get; set; } = [];
    public ICollection<Conversation> Conversations { get; set; } = [];
}
