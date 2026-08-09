using FluentAssertions;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Infrastructure.Data;
using HappyPaws.Infrastructure.Data.Seeder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Tests.Integration;

[Collection("Integration")]
public class DataSeederTests
{
    private readonly CustomWebApplicationFactory _factory;

    public DataSeederTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedAsync_PopulatesAllSeedAccountsAndDomainData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        // Verify users exist (either seeded on app startup or via seeder call)
        if (!await db.Users.AnyAsync(u => u.Email == "nethminagunasekara@outlook.com"))
        {
            await DataSeeder.SeedAsync(db, hasher);
        }

        // 1. Verify User accounts
        var users = await db.Users
            .Include(u => u.Roles)
            .Include(u => u.LifestyleProfile)
            .Include(u => u.IdentityDocuments)
            .Include(u => u.Badges)
            .Include(u => u.Devices)
            .ToListAsync();

        users.Should().Contain(u => u.Email == "nethminagunasekara@outlook.com");
        users.Should().Contain(u => u.Email == "ashinichamodya@gmail.com");
        users.Should().Contain(u => u.Email == "shanukaravishan@gmail.com");
        users.Should().Contain(u => u.Email == "sachinthasandaruwan@gmail.com");
        users.Should().Contain(u => u.Email == "chanukadilhara@gmail.com");
        users.Should().Contain(u => u.Email == "shehananushka@gmail.com");

        // 2. Verify all users have Lifestyle Profiles
        var profiles = await db.LifestyleProfiles.ToListAsync();
        profiles.Count.Should().BeGreaterThanOrEqualTo(6);

        // 3. Verify Identity Documents
        var docs = await db.IdentityDocuments.ToListAsync();
        docs.Should().Contain(d => d.DocumentType == DocumentType.Nic && d.Status == DocumentStatus.Approved);
        docs.Should().Contain(d => d.DocumentType == DocumentType.ClinicReg && d.Status == DocumentStatus.Approved);
        docs.Should().Contain(d => d.DocumentType == DocumentType.License && d.Status == DocumentStatus.Approved);

        // 4. Verify Badges
        var badges = await db.UserBadges.ToListAsync();
        badges.Should().Contain(b => b.BadgeType == BadgeType.VerifiedVet);
        badges.Should().Contain(b => b.BadgeType == BadgeType.TopFoster);
        badges.Should().Contain(b => b.BadgeType == BadgeType.TrustedTransporter);

        // 5. Verify Rescue Cases & Case Updates
        var rescueCases = await db.RescueCases.ToListAsync();
        rescueCases.Should().NotBeEmpty();

        var updates = await db.CaseUpdates.ToListAsync();
        updates.Should().NotBeEmpty();

        // 6. Verify Listings & Photos
        var listings = await db.AnimalListings.Include(l => l.Photos).ToListAsync();
        listings.Should().NotBeEmpty();
        listings.SelectMany(l => l.Photos).Should().NotBeEmpty();

        // 7. Verify Applications & Pledges & Transports & Conversations
        var applications = await db.AdoptionApplications.ToListAsync();
        applications.Should().NotBeEmpty();

        var pledges = await db.Pledges.ToListAsync();
        pledges.Should().NotBeEmpty();

        var transports = await db.TransportTasks.ToListAsync();
        transports.Should().NotBeEmpty();

        var conversations = await db.Conversations.Include(c => c.Messages).ToListAsync();
        conversations.Should().NotBeEmpty();
        conversations.SelectMany(c => c.Messages).Should().NotBeEmpty();
    }

    [Fact]
    public async Task SeedAsync_WhenDatabaseAlreadySeeded_IsIdempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var userCountBefore = await db.Users.CountAsync();

        // Second call to SeedAsync should do nothing and not throw
        await DataSeeder.SeedAsync(db, hasher);

        var userCountAfter = await db.Users.CountAsync();
        userCountAfter.Should().Be(userCountBefore);
    }
}
