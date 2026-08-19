using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Services;

/// <summary>
/// Checks badge eligibility criteria for a user and awards any badges they have earned but not yet received.
/// </summary>
public sealed class BadgeEvaluationService(HappyPawsDbContext dbContext) : IBadgeEvaluationService
{
    /// <summary>
    /// Evaluates VerifiedVet, TopFoster, and TrustedTransporter criteria and awards matching badges.
    /// Does nothing if the user is not found.
    /// </summary>
    public async Task EvaluateAndAwardBadgesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Include(u => u.Badges)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null) return;

        var existingBadges = user.Badges.Select(b => b.BadgeType).ToHashSet();

        if (!existingBadges.Contains(BadgeType.VerifiedVet) && user.Roles.Any(r => r.Role == Role.Veterinarian))
        {
            var hasApprovedClinicReg = await dbContext.IdentityDocuments
                .AnyAsync(d => d.UserId == userId && d.DocumentType == DocumentType.ClinicReg && d.Status == DocumentStatus.Approved, cancellationToken)
                .ConfigureAwait(false);

            if (hasApprovedClinicReg)
                await AwardBadgeAsync(userId, BadgeType.VerifiedVet, cancellationToken).ConfigureAwait(false);
        }

        if (!existingBadges.Contains(BadgeType.TopFoster))
        {
            var completedFosters = await dbContext.RescueCases
                .CountAsync(c => c.AssignedFosterId == userId && c.Status == CaseStatus.Resolved, cancellationToken)
                .ConfigureAwait(false);

            if (completedFosters >= 5)
                await AwardBadgeAsync(userId, BadgeType.TopFoster, cancellationToken).ConfigureAwait(false);
        }

        if (!existingBadges.Contains(BadgeType.TrustedTransporter))
        {
            var deliveredTransports = await dbContext.TransportTasks
                .CountAsync(t => t.TransporterId == userId && t.Status == TransportStatus.Delivered, cancellationToken)
                .ConfigureAwait(false);

            if (deliveredTransports >= 10)
                await AwardBadgeAsync(userId, BadgeType.TrustedTransporter, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates and persists a <see cref="UserBadge"/> record for the given user and badge type.
    /// </summary>
    private async Task AwardBadgeAsync(Guid userId, BadgeType badgeType, CancellationToken cancellationToken)
    {
        var badge = new UserBadge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BadgeType = badgeType,
            AwardedAt = DateTimeOffset.UtcNow
        };

        dbContext.UserBadges.Add(badge);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
