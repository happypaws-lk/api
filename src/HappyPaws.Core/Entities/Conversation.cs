namespace HappyPaws.Core.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? CaseId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public AnimalListing? Listing { get; set; }
    public RescueCase? Case { get; set; }
    public ICollection<ConversationParticipant> Participants { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
}
