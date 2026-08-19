namespace HappyPaws.Core.Interfaces;

/// <summary>
/// Sends transactional emails. Implementations use Amazon SES in production and log to the console in development.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends a plain-text OTP email for identity verification.</summary>
    Task SendOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>Sends a KYC approval or rejection email.</summary>
    Task SendVerificationDecisionAsync(string toEmail, bool approved, string? reason, CancellationToken cancellationToken = default);

    /// <summary>Sends a password reset OTP to the user's email.</summary>
    /// <param name="toEmail">The user's registered email address.</param>
    /// <param name="otpCode">The plain-text 6-digit code to include in the email.</param>
    Task SendPasswordResetOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>Sends a sign-up email verification OTP to the supplied address.</summary>
    /// <param name="toEmail">The address entered during sign-up.</param>
    /// <param name="otpCode">The plain-text 6-digit code to include in the email.</param>
    Task SendSignupOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);
}
