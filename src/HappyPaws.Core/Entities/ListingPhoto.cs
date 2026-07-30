namespace HappyPaws.Core.Entities;

public class ListingPhoto
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public string StorageKey { get; set; } = null!;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public AnimalListing Listing { get; set; } = null!;
}
