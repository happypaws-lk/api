using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

/// <summary>
/// Development stub that logs email content to the console instead of sending real emails.
/// </summary>
public sealed class LocalEmailSender : IEmailSender
{
    private readonly ILogger<LocalEmailSender> _logger;

    public LocalEmailSender(ILogger<LocalEmailSender> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the OTP code for the given email address and returns immediately.
    /// </summary>
    public Task SendOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV EMAIL] OTP for {Email}: {Code}", toEmail, otpCode);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs the KYC decision for the given email address and returns immediately.
    /// </summary>
    public Task SendVerificationDecisionAsync(string toEmail, bool approved, string? reason, CancellationToken cancellationToken = default)
    {
        var decision = approved ? "APPROVED" : $"REJECTED (reason: {reason})";
        _logger.LogInformation("[DEV EMAIL] KYC verification for {Email}: {Decision}", toEmail, decision);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs the password reset OTP for the given email address and returns immediately.
    /// </summary>
    public Task SendPasswordResetOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV EMAIL] Password reset OTP for {Email}: {Code}", toEmail, otpCode);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs the sign-up OTP for the given email address and returns immediately.
    /// </summary>
    public Task SendSignupOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV EMAIL] Signup OTP for {Email}: {Code}", toEmail, otpCode);
        return Task.CompletedTask;
    }
}
