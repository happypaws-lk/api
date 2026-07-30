using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Entities;

public class AdoptionApplication
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid ApplicantId { get; set; }
    public ApplicationStatus Status { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public AnimalListing Listing { get; set; } = null!;
    public User Applicant { get; set; } = null!;
}
