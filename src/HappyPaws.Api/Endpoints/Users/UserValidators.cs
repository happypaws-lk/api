using FluentValidation;

namespace HappyPaws.Api.Endpoints.Users;

public class DeviceRequestValidator : AbstractValidator<DeviceRequest>
{
    public DeviceRequestValidator()
    {
        RuleFor(x => x.FcmToken).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Platform).IsInEnum();
    }
}

public class LifestyleProfileRequestValidator : AbstractValidator<LifestyleProfileRequest>
{
    public LifestyleProfileRequestValidator()
    {
        RuleFor(x => x.HomeSize).IsInEnum();
        RuleFor(x => x.ActivityLevel).IsInEnum();
        RuleFor(x => x.ExistingPetTypes)
            .Must(p => p is null || p.Count <= 10)
            .WithMessage("ExistingPetTypes must not exceed 10 items");
    }
}

public class UpdateMeProfileRequestValidator : AbstractValidator<UpdateMeProfileRequest>
{
    public UpdateMeProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}
