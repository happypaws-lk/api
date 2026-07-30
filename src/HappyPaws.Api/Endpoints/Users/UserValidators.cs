using FluentValidation;

namespace HappyPaws.Api.Endpoints.Users;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .MinimumLength(2).MaximumLength(100)
            .When(x => x.Name is not null);
    }
}

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
