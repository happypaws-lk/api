using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace HappyPaws.Infrastructure.Data.Seeder;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(HappyPawsDbContext dbContext, IPasswordHasher<User> passwordHasher)
    {
        if (await dbContext.Users.AnyAsync())
        {
            return; // DB has been seeded
        }

        var adminId = Guid.NewGuid();
        var adminUser = new User
        {
            Id = adminId,
            Email = "admin@happypaws.lk",
            Name = "System Admin",
            Roles = [new UserRole { Id = Guid.NewGuid(), Role = Role.Admin, AssignedAt = DateTimeOffset.UtcNow }],
            IsVerified = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin@123");
        dbContext.Users.Add(adminUser);

        var vetId = Guid.NewGuid();
        var vetUser = new User
        {
            Id = vetId,
            Email = "vet@happypaws.lk",
            Name = "Dr. Silva",
            Roles = [new UserRole { Id = Guid.NewGuid(), Role = Role.Veterinarian, AssignedAt = DateTimeOffset.UtcNow }],
            IsVerified = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        vetUser.PasswordHash = passwordHasher.HashPassword(vetUser, "Vet@123");
        dbContext.Users.Add(vetUser);

        var rescuerId = Guid.NewGuid();
        var rescuer = new User
        {
            Id = rescuerId,
            Email = "rescuer@happypaws.lk",
            Name = "Kasun Perera",
            Roles = [new UserRole { Id = Guid.NewGuid(), Role = Role.Adopter, AssignedAt = DateTimeOffset.UtcNow }],
            IsVerified = true,
            ReputationPoints = 50,
            CreatedAt = DateTimeOffset.UtcNow
        };
        rescuer.PasswordHash = passwordHasher.HashPassword(rescuer, "Rescuer@123");
        dbContext.Users.Add(rescuer);

        var rescueCaseId = Guid.NewGuid();
        var rescueCase = new RescueCase
        {
            Id = rescueCaseId,
            ReporterId = rescuerId,
            Status = CaseStatus.Open,
            Urgency = Urgency.Critical,
            Description = "Found an injured dog near Galle Road.",
            LocationCoords = new Point(79.8612, 6.9271) { SRID = 4326 },
            LocationName = "Galle Road, Colombo",
            CreatedAt = DateTimeOffset.UtcNow,
            OriginalAiUrgency = Urgency.Critical,
            UrgencySource = UrgencySource.Gemini,
            PhotoKey = "demo_photo.jpg",
            IsActive = true
        };
        dbContext.RescueCases.Add(rescueCase);

        await dbContext.SaveChangesAsync();
    }
}
