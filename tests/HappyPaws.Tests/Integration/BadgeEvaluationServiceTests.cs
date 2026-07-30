using FluentAssertions;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;

namespace HappyPaws.Tests.Integration;

public class BadgeEvaluationServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BadgeEvaluationServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static User CreateUser(string? suffix = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Badge Test User",
        Email = $"{Guid.NewGuid():N}{suffix}@test.com",
        PasswordHash = "hash",
        IsVerified = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static User CreateReporter() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Reporter",
        Email = $"{Guid.NewGuid():N}@test.com",
        PasswordHash = "hash",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task EvaluateAndAwardBadgesAsync_Should_Award_VerifiedVet()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IBadgeEvaluationService>();

        var user = CreateUser();
        db.Users.Add(user);

        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = user.Id, Role = Role.Veterinarian, AssignedAt = DateTimeOffset.UtcNow });
        db.IdentityDocuments.Add(new IdentityDocument
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DocumentKey = "doc-key",
            DocumentType = DocumentType.ClinicReg,
            Status = DocumentStatus.Approved,
            UploadedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        await service.EvaluateAndAwardBadgesAsync(user.Id);

        var badges = await db.UserBadges.Where(b => b.UserId == user.Id).ToListAsync();
        badges.Should().ContainSingle(b => b.BadgeType == BadgeType.VerifiedVet);
    }

    [Fact]
    public async Task EvaluateAndAwardBadgesAsync_Should_Award_TopFoster()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IBadgeEvaluationService>();

        var user = CreateUser();
        var reporter = CreateReporter();
        db.Users.AddRange(user, reporter);

        for (var i = 0; i < 5; i++)
        {
            db.RescueCases.Add(new RescueCase
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
                AssignedFosterId = user.Id,
                Status = CaseStatus.Resolved,
                LocationCoords = new Point(0, 0) { SRID = 4326 },
                LocationName = "Loc",
                Description = "Desc",
                PhotoKey = "key",
                UrgencySource = UrgencySource.RuleBased,
                Urgency = Urgency.Low,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();

        await service.EvaluateAndAwardBadgesAsync(user.Id);

        var badges = await db.UserBadges.Where(b => b.UserId == user.Id).ToListAsync();
        badges.Should().ContainSingle(b => b.BadgeType == BadgeType.TopFoster);
    }

    [Fact]
    public async Task EvaluateAndAwardBadgesAsync_Should_Award_TrustedTransporter()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IBadgeEvaluationService>();

        var user = CreateUser();
        var reporter = CreateReporter();
        db.Users.AddRange(user, reporter);

        var rescueCase = new RescueCase
        {
            Id = Guid.NewGuid(),
            ReporterId = reporter.Id,
            Status = CaseStatus.Resolved,
            LocationCoords = new Point(0, 0) { SRID = 4326 },
            LocationName = "Loc",
            Description = "Desc",
            PhotoKey = "key",
            UrgencySource = UrgencySource.RuleBased,
            Urgency = Urgency.Low,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };
        db.RescueCases.Add(rescueCase);

        for (var i = 0; i < 10; i++)
        {
            db.TransportTasks.Add(new TransportTask
            {
                Id = Guid.NewGuid(),
                CaseId = rescueCase.Id,
                TransporterId = user.Id,
                Status = TransportStatus.Delivered,
                PickupLocation = "A",
                PickupLocationCoords = new Point(0, 0) { SRID = 4326 },
                DropoffLocation = "B",
                DropoffLocationCoords = new Point(1, 1) { SRID = 4326 },
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync();

        await service.EvaluateAndAwardBadgesAsync(user.Id);

        var badges = await db.UserBadges.Where(b => b.UserId == user.Id).ToListAsync();
        badges.Should().ContainSingle(b => b.BadgeType == BadgeType.TrustedTransporter);
    }

    [Fact]
    public async Task EvaluateAndAwardBadgesAsync_Should_Not_Create_Duplicate_Badge()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPawsDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IBadgeEvaluationService>();

        var user = CreateUser();
        var reporter = CreateReporter();
        db.Users.AddRange(user, reporter);

        for (var i = 0; i < 5; i++)
        {
            db.RescueCases.Add(new RescueCase
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
                AssignedFosterId = user.Id,
                Status = CaseStatus.Resolved,
                LocationCoords = new Point(0, 0) { SRID = 4326 },
                LocationName = "Loc",
                Description = "Desc",
                PhotoKey = "key",
                UrgencySource = UrgencySource.RuleBased,
                Urgency = Urgency.Low,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();

        // Call twice to verify idempotency
        await service.EvaluateAndAwardBadgesAsync(user.Id);
        await service.EvaluateAndAwardBadgesAsync(user.Id);

        var badges = await db.UserBadges.Where(b => b.UserId == user.Id).ToListAsync();
        badges.Count(b => b.BadgeType == BadgeType.TopFoster).Should().Be(1);
    }
}
