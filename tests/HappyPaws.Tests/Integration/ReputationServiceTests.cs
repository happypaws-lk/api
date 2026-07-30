using FluentAssertions;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPaws.Tests.Integration;

public class ReputationServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReputationServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static User CreateUser(string? email = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Reputation Test User",
        Email = email ?? $"{Guid.NewGuid():N}@test.com",
        PasswordHash = "hash",
        IsVerified = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
        ReputationPoints = 0
    };

    [Fact]
    public async Task AwardPointsAsync_RescueReportVerified_Adds_10_Points()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IReputationService>();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var referenceId = Guid.NewGuid();

        await service.AwardPointsAsync(user.Id, "RescueReportVerified", 10, referenceId, "RescueCase");

        var updatedUser = await db.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.ReputationPoints.Should().Be(10);

        var repEvent = await db.ReputationEvents.FirstOrDefaultAsync(e => e.UserId == user.Id);
        repEvent.Should().NotBeNull();
        repEvent!.EventType.Should().Be("RescueReportVerified");
        repEvent.Points.Should().Be(10);
        repEvent.ReferenceId.Should().Be(referenceId);
        repEvent.ReferenceType.Should().Be("RescueCase");
    }

    [Fact]
    public async Task AwardPointsAsync_FosterPlacementCompleted_Adds_20_Points()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IReputationService>();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var referenceId = Guid.NewGuid();

        await service.AwardPointsAsync(user.Id, "FosterPlacementCompleted", 20, referenceId, "RescueCase");

        var updatedUser = await db.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.ReputationPoints.Should().Be(20);

        var repEvent = await db.ReputationEvents.FirstOrDefaultAsync(e => e.UserId == user.Id);
        repEvent.Should().NotBeNull();
        repEvent!.EventType.Should().Be("FosterPlacementCompleted");
        repEvent.Points.Should().Be(20);
    }

    [Fact]
    public async Task AwardPointsAsync_AdoptionCompleted_Adds_15_Points()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IReputationService>();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var referenceId = Guid.NewGuid();

        await service.AwardPointsAsync(user.Id, "AdoptionCompleted", 15, referenceId, "AnimalListing");

        var updatedUser = await db.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.ReputationPoints.Should().Be(15);

        var repEvent = await db.ReputationEvents.FirstOrDefaultAsync(e => e.UserId == user.Id);
        repEvent.Should().NotBeNull();
        repEvent!.EventType.Should().Be("AdoptionCompleted");
        repEvent.Points.Should().Be(15);
        repEvent.ReferenceType.Should().Be("AnimalListing");
    }

    [Fact]
    public async Task AwardPointsAsync_TransportDelivered_Adds_10_Points()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IReputationService>();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var referenceId = Guid.NewGuid();

        await service.AwardPointsAsync(user.Id, "TransportDelivered", 10, referenceId, "TransportTask");

        var updatedUser = await db.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.ReputationPoints.Should().Be(10);

        var repEvent = await db.ReputationEvents.FirstOrDefaultAsync(e => e.UserId == user.Id);
        repEvent.Should().NotBeNull();
        repEvent!.EventType.Should().Be("TransportDelivered");
        repEvent.Points.Should().Be(10);
        repEvent.ReferenceType.Should().Be("TransportTask");
    }

    [Fact]
    public async Task AwardPointsAsync_MedicalGuidance_Adds_10_Points()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IReputationService>();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var referenceId = Guid.NewGuid();

        await service.AwardPointsAsync(user.Id, "MedicalGuidance", 10, referenceId, "RescueCase");

        var updatedUser = await db.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.ReputationPoints.Should().Be(10);

        var repEvent = await db.ReputationEvents.FirstOrDefaultAsync(e => e.UserId == user.Id);
        repEvent.Should().NotBeNull();
        repEvent!.EventType.Should().Be("MedicalGuidance");
        repEvent.Points.Should().Be(10);
    }

    [Fact]
    public async Task AwardPointsAsync_PledgeConfirmed_Adds_5_Points()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IReputationService>();

        var user = CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var referenceId = Guid.NewGuid();

        await service.AwardPointsAsync(user.Id, "PledgeConfirmed", 5, referenceId, "Pledge");

        var updatedUser = await db.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.ReputationPoints.Should().Be(5);

        var repEvent = await db.ReputationEvents.FirstOrDefaultAsync(e => e.UserId == user.Id);
        repEvent.Should().NotBeNull();
        repEvent!.EventType.Should().Be("PledgeConfirmed");
        repEvent.Points.Should().Be(5);
        repEvent.ReferenceType.Should().Be("Pledge");
    }
}
