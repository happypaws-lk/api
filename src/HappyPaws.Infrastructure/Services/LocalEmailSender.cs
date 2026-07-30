using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

public sealed class LocalEmailSender : IEmailSender
{
    private readonly ILogger<LocalEmailSender> _logger;

    public LocalEmailSender(ILogger<LocalEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV EMAIL] OTP for {Email}: {Code}", toEmail, otpCode);
        return Task.CompletedTask;
    }

    public Task SendVerificationDecisionAsync(string toEmail, bool approved, string? reason, CancellationToken cancellationToken = default)
    {
        var decision = approved ? "APPROVED" : $"REJECTED (reason: {reason})";
        _logger.LogInformation("[DEV EMAIL] KYC verification for {Email}: {Decision}", toEmail, decision);
        return Task.CompletedTask;
    }
}
