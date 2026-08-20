using FluentValidation;

namespace HappyPaws.Api.Endpoints.Users;

/// <summary>
/// Validates the FCM token and platform when registering or updating a user's device.
/// </summary>
public class DeviceRequestValidator : AbstractValidator<DeviceRequest>
{
    public DeviceRequestValidator()
    {
        RuleFor(x => x.FcmToken).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Platform).IsInEnum();
    }
}

/// <summary>
/// Validates home size, activity level, and the existing pet types list (max 10 items) for a lifestyle profile.
/// </summary>
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

/// <summary>
/// Validates the display name when a user updates their own profile.
/// </summary>
public class UpdateMeProfileRequestValidator : AbstractValidator<UpdateMeProfileRequest>
{
    public UpdateMeProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}

/// <summary>
/// Validates the current password and the new password strength for a password change request.
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

/// <summary>
/// Validates that the role being assigned is a known enum member.
/// </summary>
public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequest>
{
    public AssignRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class RequestEmailChangeRequestValidator : AbstractValidator<RequestEmailChangeRequest>
{
    public RequestEmailChangeRequestValidator()
    {
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.CurrentPassword).NotEmpty();
    }
}

public class ConfirmEmailChangeRequestValidator : AbstractValidator<ConfirmEmailChangeRequest>
{
    public ConfirmEmailChangeRequestValidator()
    {
        RuleFor(x => x.NewEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
