namespace HappyPaws.Core.Interfaces;

public interface IEmailSender
{
    Task SendOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);
    Task SendVerificationDecisionAsync(string toEmail, bool approved, string? reason, CancellationToken cancellationToken = default);
}
