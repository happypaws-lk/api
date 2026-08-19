using FluentValidation;
using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Auth;

/// <summary>
/// Validates the email address for the first step of the sign-up flow.
/// </summary>
public class SignupSendCodeRequestValidator : AbstractValidator<SignupSendCodeRequest>
{
    public SignupSendCodeRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    }
}

/// <summary>
/// Validates the email address and the 6-digit numeric verification code for the sign-up OTP step.
/// </summary>
public class SignupVerifyCodeRequestValidator : AbstractValidator<SignupVerifyCodeRequest>
{
    public SignupVerifyCodeRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches(@"^\d{6}$");
    }
}

/// <summary>
/// Validates the signup token, display name, password strength, and that the selected role is not Admin or Veterinarian.
/// </summary>
public class SignupCompleteRequestValidator : AbstractValidator<SignupCompleteRequest>
{
    public SignupCompleteRequestValidator()
    {
        RuleFor(x => x.SignupToken).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.Role).IsInEnum()
            .Must(r => r != Role.Admin && r != Role.Veterinarian)
            .WithMessage("Sign-up is not available for the Admin and Veterinarian roles.");
    }
}

/// <summary>
/// Validates the email address and password for login.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

/// <summary>
/// Validates that a refresh token is present in the request.
/// </summary>
public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

/// <summary>
/// Validates the email address for an OTP send request.
/// </summary>
public class OtpRequestValidator : AbstractValidator<OtpRequest>
{
    public OtpRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

/// <summary>
/// Validates the email address and the 6-digit numeric OTP code.
/// </summary>
public class OtpVerifyRequestValidator : AbstractValidator<OtpVerifyRequest>
{
    public OtpVerifyRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches(@"^\d{6}$");
    }
}

/// <summary>
/// Validates the email address for the forgot-password flow.
/// </summary>
public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    }
}

/// <summary>
/// Validates the email address and the 6-digit numeric password reset code.
/// </summary>
public class VerifyResetCodeRequestValidator : AbstractValidator<VerifyResetCodeRequest>
{
    public VerifyResetCodeRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches(@"^\d{6}$");
    }
}

/// <summary>
/// Validates the email, reset token, and new password for the final password reset step.
/// </summary>
public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.ResetToken).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

/// <summary>
/// Validates the current password and the new password strength for an authenticated password change.
/// </summary>
public class ChangePasswordAuthRequestValidator : AbstractValidator<ChangePasswordAuthRequest>
{
    public ChangePasswordAuthRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}
