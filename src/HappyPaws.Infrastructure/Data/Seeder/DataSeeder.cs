using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace HappyPaws.Infrastructure.Data.Seeder;

public static class DataSeeder
{
    public static async Task SeedAsync(HappyPawsDbContext dbContext, IPasswordHasher<User> passwordHasher)
    {
        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        const string defaultPassword = "Password123!";

        var adminId = Guid.NewGuid();
        var adminUser = new User { Id = adminId, Name = "HappyPaws Admin", Email = "admin@happypaws.lk", AvatarKey = "avatars/admin.jpg", LastKnownLocation = new Point(79.8528, 6.9147) { SRID = 4326 }, IsVerified = true, ReputationPoints = 500, IsSuspended = false, CreatedAt = now, UpdatedAt = now };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, defaultPassword);

        var vetId = Guid.NewGuid();
        var vetUser = new User { Id = vetId, Name = "HappyPaws Veterinarian", Email = "veterinarian@happypaws.lk", AvatarKey = "avatars/veterinarian.jpg", LastKnownLocation = new Point(79.8647, 6.8511) { SRID = 4326 }, IsVerified = true, ReputationPoints = 350, IsSuspended = false, CreatedAt = now, UpdatedAt = now };
        vetUser.PasswordHash = passwordHasher.HashPassword(vetUser, defaultPassword);

        var fosterId = Guid.NewGuid();
        var fosterUser = new User { Id = fosterId, Name = "HappyPaws Foster", Email = "foster@happypaws.lk", AvatarKey = "avatars/foster.jpg", LastKnownLocation = new Point(79.9723, 6.9042) { SRID = 4326 }, IsVerified = true, ReputationPoints = 220, IsSuspended = false, CreatedAt = now, UpdatedAt = now };
        fosterUser.PasswordHash = passwordHasher.HashPassword(fosterUser, defaultPassword);

        var transporterId = Guid.NewGuid();
        var transporterUser = new User { Id = transporterId, Name = "HappyPaws Transporter", Email = "transporter@happypaws.lk", AvatarKey = "avatars/transporter.jpg", LastKnownLocation = new Point(79.8633, 6.8301) { SRID = 4326 }, IsVerified = true, ReputationPoints = 180, IsSuspended = false, CreatedAt = now, UpdatedAt = now };
        transporterUser.PasswordHash = passwordHasher.HashPassword(transporterUser, defaultPassword);

        var sponsorId = Guid.NewGuid();
        var sponsorUser = new User { Id = sponsorId, Name = "HappyPaws Sponsor", Email = "sponsor@happypaws.lk", AvatarKey = "avatars/sponsor.jpg", LastKnownLocation = new Point(79.9275, 6.8480) { SRID = 4326 }, IsVerified = true, ReputationPoints = 300, IsSuspended = false, CreatedAt = now, UpdatedAt = now };
        sponsorUser.PasswordHash = passwordHasher.HashPassword(sponsorUser, defaultPassword);

        var adopterId = Guid.NewGuid();
        var adopterUser = new User { Id = adopterId, Name = "HappyPaws Adopter", Email = "adopter@happypaws.lk", AvatarKey = "avatars/adopter.jpg", LastKnownLocation = new Point(79.8816, 6.7730) { SRID = 4326 }, IsVerified = true, ReputationPoints = 100, IsSuspended = false, CreatedAt = now, UpdatedAt = now };
        adopterUser.PasswordHash = passwordHasher.HashPassword(adopterUser, defaultPassword);

        var users = new[] { adminUser, vetUser, fosterUser, transporterUser, sponsorUser, adopterUser };
        dbContext.Users.AddRange(users);

        var userRoles = new List<UserRole>
        {
            new() { Id = Guid.NewGuid(), UserId = adminId, Role = Role.Admin, AssignedAt = now },
            new() { Id = Guid.NewGuid(), UserId = adminId, Role = Role.Adopter, AssignedAt = now },
            new() { Id = Guid.NewGuid(), UserId = vetId, Role = Role.Veterinarian, AssignedAt = now },
            new() { Id = Guid.NewGuid(), UserId = vetId, Role = Role.Foster, AssignedAt = now },
            new() { Id = Guid.NewGuid(), UserId = fosterId, Role = Role.Foster, AssignedAt = now },
            new() { Id = Guid.NewGuid(), UserId = fosterId, Role = Role.Adopter, AssignedAt = now },
            new() { Id = Guid.NewGuid(), UserId = transporterId, Role = Role.Transporter, AssignedAt = now },
            new() { Id = Guid.NewGuid(), UserId = transporterId, Role = Role.Adopter, AssignedAt = now },
            new() { Id = Guid.NewGuid(), UserId = sponsorId, Role = Role.Sponsor, AssignedAt = now },
            new() { Id = Guid.NewGuid(), UserId = sponsorId, Role = Role.Adopter, AssignedAt = now },
            new() { Id = Guid.NewGuid(), UserId = adopterId, Role = Role.Adopter, AssignedAt = now }
        };
        dbContext.UserRoles.AddRange(userRoles);

        foreach (var user in users)
        {
            dbContext.LifestyleProfiles.Add(new LifestyleProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                HomeSize = HomeSize.House,
                ActivityLevel = ActivityLevel.Moderate,
                HasChildren = false,
                HasYard = true,
                UpdatedAt = now
            });
        }
        
        dbContext.IdentityDocuments.Add(new IdentityDocument { Id = Guid.NewGuid(), UserId = fosterId, DocumentType = DocumentType.Nic, Status = DocumentStatus.Approved, DocumentKey = "doc1", UploadedAt = now });
        dbContext.IdentityDocuments.Add(new IdentityDocument { Id = Guid.NewGuid(), UserId = vetId, DocumentType = DocumentType.ClinicReg, Status = DocumentStatus.Approved, DocumentKey = "doc2", UploadedAt = now });
        dbContext.IdentityDocuments.Add(new IdentityDocument { Id = Guid.NewGuid(), UserId = transporterId, DocumentType = DocumentType.License, Status = DocumentStatus.Approved, DocumentKey = "doc3", UploadedAt = now });

        dbContext.UserBadges.Add(new UserBadge { Id = Guid.NewGuid(), UserId = vetId, BadgeType = BadgeType.VerifiedVet, AwardedAt = now });
        dbContext.UserBadges.Add(new UserBadge { Id = Guid.NewGuid(), UserId = fosterId, BadgeType = BadgeType.TopFoster, AwardedAt = now });
        dbContext.UserBadges.Add(new UserBadge { Id = Guid.NewGuid(), UserId = transporterId, BadgeType = BadgeType.TrustedTransporter, AwardedAt = now });

        var rescueCase = new RescueCase
        {
            Id = Guid.NewGuid(),
            ReporterId = fosterId,
            Title = "Injured Dog",
            Description = "A dog needs help",
            LocationCoords = new Point(79.85, 6.90) { SRID = 4326 },
            LocationName = "Colombo",
            Status = CaseStatus.Open,
            Urgency = Urgency.Critical,
            UrgencySource = UrgencySource.RuleBased,
            PhotoKey = "photo1",
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.RescueCases.Add(rescueCase);

        dbContext.CaseUpdates.Add(new CaseUpdate
        {
            Id = Guid.NewGuid(),
            CaseId = rescueCase.Id,
            UserId = fosterId,
            UpdateType = UpdateType.StatusUpdate,
            UpdateText = "Dog is safe",
            CreatedAt = now
        });

        var listing = new AnimalListing
        {
            Id = Guid.NewGuid(),
            OwnerId = fosterId,
            RescueCaseId = rescueCase.Id,
            Title = "Sweet puppy",
            Name = "Buddy",
            Species = "Dog",
            Breed = "Mixed",
            AgeMonths = 6,
            AgeLabel = "Puppy",
            Gender = Gender.Male,
            Size = AnimalSize.Small,
            ActivityLevel = ActivityLevel.Moderate,
            Description = "Very friendly",
            LocationName = "Colombo",
            LocationCoords = new Point(79.85, 6.90) { SRID = 4326 },
            Status = ListingStatus.Available,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.AnimalListings.Add(listing);
        dbContext.ListingPhotos.Add(new ListingPhoto { Id = Guid.NewGuid(), ListingId = listing.Id, StorageKey = "photo2", SortOrder = 1 });

        dbContext.AdoptionApplications.Add(new AdoptionApplication
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            ApplicantId = adopterId,
            Status = ApplicationStatus.Pending,
            ReviewNotes = "I would love to adopt",
            AppliedAt = now,
            UpdatedAt = now
        });

        dbContext.Pledges.Add(new Pledge
        {
            Id = Guid.NewGuid(),
            CaseId = rescueCase.Id,
            SponsorId = sponsorId,
            Amount = 1000,
            Status = PledgeStatus.Pledged,
            CreatedAt = now
        });

        dbContext.TransportTasks.Add(new TransportTask
        {
            Id = Guid.NewGuid(),
            CaseId = rescueCase.Id,
            Title = "Transport to vet",
            PhotoKey = "photo3",
            PickupLocation = "Colombo",
            PickupLocationCoords = new Point(79.85, 6.90) { SRID = 4326 },
            DropoffLocation = "Vet",
            DropoffLocationCoords = new Point(79.86, 6.85) { SRID = 4326 },
            Status = TransportStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        });

        var conversation = new Conversation { Id = Guid.NewGuid(), ListingId = listing.Id, CreatedAt = now };
        dbContext.Conversations.Add(conversation);
        
        dbContext.ConversationParticipants.Add(new ConversationParticipant { ConversationId = conversation.Id, UserId = fosterId, JoinedAt = now });
        dbContext.ConversationParticipants.Add(new ConversationParticipant { ConversationId = conversation.Id, UserId = adopterId, JoinedAt = now });
        
        dbContext.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            SenderId = adopterId,
            Content = "Hi!",
            SentAt = now
        });

        await dbContext.SaveChangesAsync();
    }
}
