using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Entities;

public class LifestyleProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public HomeSize HomeSize { get; set; }
    public ActivityLevel ActivityLevel { get; set; }
    public List<string>? ExistingPetTypes { get; set; }
    public bool HasChildren { get; set; }
    public bool HasYard { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
