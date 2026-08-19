using FluentValidation;

namespace HappyPaws.Api.Endpoints.Admin;

/// <summary>
/// Validates that a KYC rejection reason is provided and within the character limit.
/// </summary>
public class KycRejectRequestValidator : AbstractValidator<KycRejectRequest>
{
    public KycRejectRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

/// <summary>
/// Validates that a suspension reason is provided and within the character limit.
/// </summary>
public class SuspendRequestValidator : AbstractValidator<SuspendRequest>
{
    public SuspendRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

/// <summary>
/// Validates the target type, target ID, action type, and reason for a moderation action.
/// </summary>
public class ModerationRequestValidator : AbstractValidator<ModerationRequest>
{
    public ModerationRequestValidator()
    {
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.ActionType).IsInEnum();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

/// <summary>
/// Validates that the points adjustment is non-zero and a reason is provided.
/// </summary>
public class ReputationAdjustRequestValidator : AbstractValidator<ReputationAdjustRequest>
{
    public ReputationAdjustRequestValidator()
    {
        RuleFor(x => x.PointsToAdjust).NotEqual(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
