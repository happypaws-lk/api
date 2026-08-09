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
            return; // DB has already been seeded
        }

        var now = DateTimeOffset.UtcNow;
        const string defaultPassword = "Password123!";

        // -----------------------------------------------------------------------------
        // 1. CREATE USER ACCOUNTS
        // -----------------------------------------------------------------------------

        // Admin Account: Nethmina Gunasekara
        var adminId = Guid.NewGuid();
        var adminUser = new User
        {
            Id = adminId,
            Name = "Nethmina Gunasekara",
            Email = "nethminagunasekara@outlook.com",
            AvatarKey = "avatars/nethmina_admin.jpg",
            LastKnownLocation = new Point(79.8528, 6.9147) { SRID = 4326 },
            IsVerified = true,
            ReputationPoints = 500,
            IsSuspended = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, defaultPassword);

        // Veterinarian Account: Dr. Ashini Chamodya
        var vetId = Guid.NewGuid();
        var vetUser = new User
        {
            Id = vetId,
            Name = "Ashini Chamodya",
            Email = "ashinichamodya@gmail.com",
            AvatarKey = "avatars/ashini_vet.jpg",
            LastKnownLocation = new Point(79.8647, 6.8511) { SRID = 4326 },
            IsVerified = true,
            ReputationPoints = 350,
            IsSuspended = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        vetUser.PasswordHash = passwordHasher.HashPassword(vetUser, defaultPassword);

        // Foster / Rescuer Account: Shanuka Ravishan
        var fosterId = Guid.NewGuid();
        var fosterUser = new User
        {
            Id = fosterId,
            Name = "Shanuka Ravishan",
            Email = "shanukaravishan@gmail.com",
            AvatarKey = "avatars/shanuka_foster.jpg",
            LastKnownLocation = new Point(79.9723, 6.9042) { SRID = 4326 },
            IsVerified = true,
            ReputationPoints = 220,
            IsSuspended = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        fosterUser.PasswordHash = passwordHasher.HashPassword(fosterUser, defaultPassword);

        // Transporter Account: Sachintha Sandaruwan
        var transporterId = Guid.NewGuid();
        var transporterUser = new User
        {
            Id = transporterId,
            Name = "Sachintha Sandaruwan",
            Email = "sachinthasandaruwan@gmail.com",
            AvatarKey = "avatars/sachintha_transporter.jpg",
            LastKnownLocation = new Point(79.8633, 6.8301) { SRID = 4326 },
            IsVerified = true,
            ReputationPoints = 180,
            IsSuspended = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        transporterUser.PasswordHash = passwordHasher.HashPassword(transporterUser, defaultPassword);

        // Sponsor Account: Chanuka Dilhara
        var sponsorId = Guid.NewGuid();
        var sponsorUser = new User
        {
            Id = sponsorId,
            Name = "Chanuka Dilhara",
            Email = "chanukadilhara@gmail.com",
            AvatarKey = "avatars/chanuka_sponsor.jpg",
            LastKnownLocation = new Point(79.9275, 6.8480) { SRID = 4326 },
            IsVerified = true,
            ReputationPoints = 300,
            IsSuspended = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        sponsorUser.PasswordHash = passwordHasher.HashPassword(sponsorUser, defaultPassword);

        // Adopter Account: Shehan Anushka
        var adopterId = Guid.NewGuid();
        var adopterUser = new User
        {
            Id = adopterId,
            Name = "Shehan Anushka",
            Email = "shehananushka@gmail.com",
            AvatarKey = "avatars/shehan_adopter.jpg",
            LastKnownLocation = new Point(79.8816, 6.7730) { SRID = 4326 },
            IsVerified = true,
            ReputationPoints = 100,
            IsSuspended = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        adopterUser.PasswordHash = passwordHasher.HashPassword(adopterUser, defaultPassword);

        var users = new[] { adminUser, vetUser, fosterUser, transporterUser, sponsorUser, adopterUser };
        dbContext.Users.AddRange(users);

        // -----------------------------------------------------------------------------
        // 2. ASSIGN USER ROLES
        // -----------------------------------------------------------------------------

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

        // -----------------------------------------------------------------------------
        // 3. LIFESTYLE PROFILES FOR ALL USERS
        // -----------------------------------------------------------------------------

        var lifestyleProfiles = new List<LifestyleProfile>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = adminId,
                HomeSize = HomeSize.House,
                ActivityLevel = ActivityLevel.Moderate,
                ExistingPetTypes = ["Dog", "Cat"],
                HasChildren = false,
                HasYard = true,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = vetId,
                HomeSize = HomeSize.House,
                ActivityLevel = ActivityLevel.High,
                ExistingPetTypes = ["Dog", "Cat", "Rabbit"],
                HasChildren = true,
                HasYard = true,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = fosterId,
                HomeSize = HomeSize.House,
                ActivityLevel = ActivityLevel.High,
                ExistingPetTypes = ["Dog"],
                HasChildren = false,
                HasYard = true,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = transporterId,
                HomeSize = HomeSize.Apartment,
                ActivityLevel = ActivityLevel.High,
                ExistingPetTypes = [],
                HasChildren = false,
                HasYard = false,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = sponsorId,
                HomeSize = HomeSize.Estate,
                ActivityLevel = ActivityLevel.Low,
                ExistingPetTypes = ["Dog", "Bird"],
                HasChildren = true,
                HasYard = true,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = adopterId,
                HomeSize = HomeSize.Apartment,
                ActivityLevel = ActivityLevel.Moderate,
                ExistingPetTypes = ["Cat"],
                HasChildren = true,
                HasYard = false,
                UpdatedAt = now
            }
        };
        dbContext.LifestyleProfiles.AddRange(lifestyleProfiles);

        // -----------------------------------------------------------------------------
        // 4. IDENTITY DOCUMENTS & BADGES
        // -----------------------------------------------------------------------------

        var identityDocuments = new List<IdentityDocument>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = adminId,
                ReviewedById = adminId,
                DocumentKey = "docs/nic_nethmina.jpg",
                DocumentType = DocumentType.Nic,
                Status = DocumentStatus.Approved,
                UploadedAt = now.AddDays(-30),
                ReviewedAt = now.AddDays(-30)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = vetId,
                ReviewedById = adminId,
                DocumentKey = "docs/clinic_reg_ashini.pdf",
                DocumentType = DocumentType.ClinicReg,
                Status = DocumentStatus.Approved,
                UploadedAt = now.AddDays(-25),
                ReviewedAt = now.AddDays(-24)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = fosterId,
                ReviewedById = adminId,
                DocumentKey = "docs/nic_shanuka.jpg",
                DocumentType = DocumentType.Nic,
                Status = DocumentStatus.Approved,
                UploadedAt = now.AddDays(-20),
                ReviewedAt = now.AddDays(-19)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = transporterId,
                ReviewedById = adminId,
                DocumentKey = "docs/license_sachintha.jpg",
                DocumentType = DocumentType.License,
                Status = DocumentStatus.Approved,
                UploadedAt = now.AddDays(-18),
                ReviewedAt = now.AddDays(-17)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = sponsorId,
                ReviewedById = adminId,
                DocumentKey = "docs/nic_chanuka.jpg",
                DocumentType = DocumentType.Nic,
                Status = DocumentStatus.Approved,
                UploadedAt = now.AddDays(-15),
                ReviewedAt = now.AddDays(-14)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = adopterId,
                ReviewedById = adminId,
                DocumentKey = "docs/nic_shehan.jpg",
                DocumentType = DocumentType.Nic,
                Status = DocumentStatus.Approved,
                UploadedAt = now.AddDays(-10),
                ReviewedAt = now.AddDays(-9)
            }
        };
        dbContext.IdentityDocuments.AddRange(identityDocuments);

        var badges = new List<UserBadge>
        {
            new() { Id = Guid.NewGuid(), UserId = vetId, BadgeType = BadgeType.VerifiedVet, AwardedAt = now.AddDays(-24) },
            new() { Id = Guid.NewGuid(), UserId = fosterId, BadgeType = BadgeType.TopFoster, AwardedAt = now.AddDays(-15) },
            new() { Id = Guid.NewGuid(), UserId = transporterId, BadgeType = BadgeType.TrustedTransporter, AwardedAt = now.AddDays(-12) }
        };
        dbContext.UserBadges.AddRange(badges);

        // -----------------------------------------------------------------------------
        // 5. USER DEVICES (FCM PUSH TOKENS)
        // -----------------------------------------------------------------------------

        var devices = new List<UserDevice>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = vetId,
                FcmToken = "fcm_token_vet_ashini_web",
                DeviceName = "Dr. Ashini's Clinic iPad",
                Platform = Platform.Web,
                LastActiveAt = now,
                CreatedAt = now.AddDays(-20)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = fosterId,
                FcmToken = "fcm_token_foster_shanuka_android",
                DeviceName = "Shanuka's Pixel 8",
                Platform = Platform.Android,
                LastActiveAt = now,
                CreatedAt = now.AddDays(-18)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = transporterId,
                FcmToken = "fcm_token_transporter_sachintha_android",
                DeviceName = "Sachintha's Galaxy S24",
                Platform = Platform.Android,
                LastActiveAt = now,
                CreatedAt = now.AddDays(-16)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = sponsorId,
                FcmToken = "fcm_token_sponsor_chanuka_web",
                DeviceName = "Chanuka's MacBook Pro",
                Platform = Platform.Web,
                LastActiveAt = now,
                CreatedAt = now.AddDays(-12)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = adopterId,
                FcmToken = "fcm_token_adopter_shehan_android",
                DeviceName = "Shehan's OnePlus 11",
                Platform = Platform.Android,
                LastActiveAt = now,
                CreatedAt = now.AddDays(-8)
            }
        };
        dbContext.UserDevices.AddRange(devices);

        // -----------------------------------------------------------------------------
        // 6. RESCUE CASES & CASE UPDATES
        // -----------------------------------------------------------------------------

        var rescueCase1Id = Guid.NewGuid();
        var rescueCase1 = new RescueCase
        {
            Id = rescueCase1Id,
            ReporterId = fosterId,
            AssignedFosterId = fosterId,
            Status = CaseStatus.InProgress,
            Urgency = Urgency.Critical,
            OriginalAiUrgency = Urgency.Critical,
            UrgencySource = UrgencySource.Gemini,
            Description = "Found an injured stray dog hit by a vehicle near Galle Road. Requires immediate veterinary attention and emergency foster care.",
            ConditionNotes = "Left hind leg swollen, minor abrasions.",
            LocationCoords = new Point(79.8612, 6.9271) { SRID = 4326 },
            LocationName = "Galle Road, Bambalapitiya",
            PhotoKey = "rescues/galle_road_dog.jpg",
            IsActive = true,
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddDays(-2)
        };

        var rescueCase2Id = Guid.NewGuid();
        var rescueCase2 = new RescueCase
        {
            Id = rescueCase2Id,
            ReporterId = adopterId,
            Status = CaseStatus.Open,
            Urgency = Urgency.Moderate,
            OriginalAiUrgency = Urgency.Moderate,
            UrgencySource = UrgencySource.Gemini,
            Description = "Found a small box with abandoned kittens near Dehiwala station. They are safe but need medical checks and foster placement.",
            LocationCoords = new Point(79.8640, 6.8520) { SRID = 4326 },
            LocationName = "Station Road, Dehiwala",
            PhotoKey = "rescues/dehiwala_kittens.jpg",
            IsActive = true,
            CreatedAt = now.AddDays(-3),
            UpdatedAt = now.AddDays(-3)
        };

        dbContext.RescueCases.AddRange(rescueCase1, rescueCase2);

        var caseUpdates = new List<CaseUpdate>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CaseId = rescueCase1Id,
                UserId = vetId,
                UpdateType = UpdateType.MedicalGuidance,
                UpdateText = "Examined the dog at Dehiwala Vet Clinic. Administered painkillers, X-ray confirmed no fracture, left hind leg bandaged. Needs 2 weeks of foster rest.",
                PhotoKey = "updates/medical_check_dog.jpg",
                CreatedAt = now.AddDays(-6)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CaseId = rescueCase1Id,
                UserId = fosterId,
                UpdateType = UpdateType.StatusUpdate,
                UpdateText = "Dog is currently resting safely at my foster location in Kaduwela. Eating well and moving slowly.",
                CreatedAt = now.AddDays(-4)
            }
        };
        dbContext.CaseUpdates.AddRange(caseUpdates);

        // -----------------------------------------------------------------------------
        // 7. ANIMAL LISTINGS & PHOTOS
        // -----------------------------------------------------------------------------

        var listing1Id = Guid.NewGuid();
        var listing1 = new AnimalListing
        {
            Id = listing1Id,
            OwnerId = fosterId,
            RescueCaseId = rescueCase1Id,
            Name = "Buddy",
            Species = "Dog",
            Breed = "Golden Retriever Mix",
            AgeMonths = 18,
            AgeLabel = "Young Adult",
            Gender = Gender.Male,
            Size = AnimalSize.Medium,
            ActivityLevel = ActivityLevel.Moderate,
            Description = "Buddy is a super friendly, fully recovered rescue dog looking for a loving forever home. Great with children and other pets.",
            LocationCoords = new Point(79.9723, 6.9042) { SRID = 4326 },
            LocationName = "Malabe, Colombo",
            Status = ListingStatus.Available,
            IsActive = true,
            CreatedAt = now.AddDays(-4),
            UpdatedAt = now.AddDays(-1)
        };

        var listing2Id = Guid.NewGuid();
        var listing2 = new AnimalListing
        {
            Id = listing2Id,
            OwnerId = vetId,
            RescueCaseId = rescueCase2Id,
            Name = "Milo",
            Species = "Cat",
            Breed = "Domestic Short-hair",
            AgeMonths = 4,
            AgeLabel = "Kitten",
            Gender = Gender.Female,
            Size = AnimalSize.Small,
            ActivityLevel = ActivityLevel.High,
            Description = "Milo is a playful and affectionate kitten who has received her first vaccinations. Ready for a warm home.",
            LocationCoords = new Point(79.8647, 6.8511) { SRID = 4326 },
            LocationName = "Dehiwala, Colombo",
            Status = ListingStatus.Available,
            IsActive = true,
            CreatedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-2)
        };

        dbContext.AnimalListings.AddRange(listing1, listing2);

        var listingPhotos = new List<ListingPhoto>
        {
            new() { Id = Guid.NewGuid(), ListingId = listing1Id, StorageKey = "listings/buddy_main.jpg", SortOrder = 0, CreatedAt = now.AddDays(-4) },
            new() { Id = Guid.NewGuid(), ListingId = listing1Id, StorageKey = "listings/buddy_play.jpg", SortOrder = 1, CreatedAt = now.AddDays(-4) },
            new() { Id = Guid.NewGuid(), ListingId = listing2Id, StorageKey = "listings/milo_main.jpg", SortOrder = 0, CreatedAt = now.AddDays(-2) }
        };
        dbContext.ListingPhotos.AddRange(listingPhotos);

        // -----------------------------------------------------------------------------
        // 8. ADOPTION APPLICATIONS
        // -----------------------------------------------------------------------------

        var adoptionApp = new AdoptionApplication
        {
            Id = Guid.NewGuid(),
            ListingId = listing1Id,
            ApplicantId = adopterId,
            Status = ApplicationStatus.Pending,
            ReviewNotes = "Interested in adopting Buddy. Experienced pet owner with a child-friendly home environment.",
            AppliedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        };
        dbContext.AdoptionApplications.Add(adoptionApp);

        // -----------------------------------------------------------------------------
        // 9. PLEDGES (SPONSORSHIPS)
        // -----------------------------------------------------------------------------

        var pledges = new List<Pledge>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SponsorId = sponsorId,
                CaseId = rescueCase1Id,
                Amount = 10000.00m,
                Status = PledgeStatus.Confirmed,
                Note = "Sponsoring veterinary checkups, medications, and recovery food.",
                CreatedAt = now.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                SponsorId = sponsorId,
                ListingId = listing2Id,
                Amount = 5000.00m,
                Status = PledgeStatus.Pledged,
                Note = "Sponsoring kitten care package and vaccination fees.",
                CreatedAt = now.AddDays(-1)
            }
        };
        dbContext.Pledges.AddRange(pledges);

        // -----------------------------------------------------------------------------
        // 10. TRANSPORT TASKS
        // -----------------------------------------------------------------------------

        var transportTask = new TransportTask
        {
            Id = Guid.NewGuid(),
            CaseId = rescueCase1Id,
            TransporterId = transporterId,
            PickupLocationCoords = new Point(79.8612, 6.9271) { SRID = 4326 },
            PickupLocation = "Galle Road, Bambalapitiya",
            DropoffLocationCoords = new Point(79.8647, 6.8511) { SRID = 4326 },
            DropoffLocation = "Dehiwala Vet Clinic",
            Status = TransportStatus.Delivered,
            CreatedAt = now.AddDays(-7),
            UpdatedAt = now.AddDays(-6)
        };
        dbContext.TransportTasks.Add(transportTask);

        // -----------------------------------------------------------------------------
        // 11. CONVERSATIONS & MESSAGES
        // -----------------------------------------------------------------------------

        var conversationId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = conversationId,
            ListingId = listing1Id,
            CreatedAt = now.AddDays(-1)
        };
        dbContext.Conversations.Add(conversation);

        var msg1Id = Guid.NewGuid();
        var msg1 = new Message
        {
            Id = msg1Id,
            ConversationId = conversationId,
            SenderId = adopterId,
            Content = "Hi Shanuka! I submitted an adoption application for Buddy. We would love to meet him!",
            SentAt = now.AddDays(-1)
        };

        var msg2Id = Guid.NewGuid();
        var msg2 = new Message
        {
            Id = msg2Id,
            ConversationId = conversationId,
            SenderId = fosterId,
            Content = "Hi Shehan! Thanks for reaching out. Buddy is doing great! We can arrange a meet and greet this weekend.",
            SentAt = now.AddHours(-12)
        };
        dbContext.Messages.AddRange(msg1, msg2);

        var participants = new List<ConversationParticipant>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = adopterId,
                LastReadMessageId = msg2Id,
                LastReadAt = now.AddHours(-10),
                JoinedAt = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = fosterId,
                LastReadMessageId = msg2Id,
                LastReadAt = now.AddHours(-12),
                JoinedAt = now.AddDays(-1)
            }
        };
        dbContext.ConversationParticipants.AddRange(participants);

        // -----------------------------------------------------------------------------
        // 12. REPUTATION EVENTS
        // -----------------------------------------------------------------------------

        var reputationEvents = new List<ReputationEvent>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = adminId,
                EventType = "SystemAdminContribution",
                Points = 500,
                ReferenceId = adminId,
                ReferenceType = "User",
                CreatedAt = now.AddDays(-30)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = vetId,
                EventType = "MedicalGuidance",
                Points = 350,
                ReferenceId = rescueCase1Id,
                ReferenceType = "RescueCase",
                CreatedAt = now.AddDays(-6)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = fosterId,
                EventType = "RescueReportAndFoster",
                Points = 220,
                ReferenceId = rescueCase1Id,
                ReferenceType = "RescueCase",
                CreatedAt = now.AddDays(-7)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = transporterId,
                EventType = "TransportCompleted",
                Points = 180,
                ReferenceId = transportTask.Id,
                ReferenceType = "TransportTask",
                CreatedAt = now.AddDays(-6)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = sponsorId,
                EventType = "SponsorshipPledge",
                Points = 300,
                ReferenceId = rescueCase1Id,
                ReferenceType = "RescueCase",
                CreatedAt = now.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = adopterId,
                EventType = "AdoptionApplicationSubmitted",
                Points = 100,
                ReferenceId = listing1Id,
                ReferenceType = "AnimalListing",
                CreatedAt = now.AddDays(-1)
            }
        };
        dbContext.ReputationEvents.AddRange(reputationEvents);

        await dbContext.SaveChangesAsync();
    }
}
