using FluentValidation;

namespace HappyPaws.Api.Endpoints.Admin;

public class KycRejectRequestValidator : AbstractValidator<KycRejectRequest>
{
    public KycRejectRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class SuspendRequestValidator : AbstractValidator<SuspendRequest>
{
    public SuspendRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

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

public class ReputationAdjustRequestValidator : AbstractValidator<ReputationAdjustRequest>
{
    public ReputationAdjustRequestValidator()
    {
        RuleFor(x => x.PointsToAdjust).NotEqual(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
