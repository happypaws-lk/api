using System.Collections.Concurrent;
using HappyPaws.Core.Interfaces;

namespace HappyPaws.Tests.Integration;

public class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _signupOtps = new();
    private readonly ConcurrentDictionary<string, string> _passwordResetOtps = new();

    public Task SendOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendVerificationDecisionAsync(string toEmail, bool approved, string? reason, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SendPasswordResetOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        _passwordResetOtps[toEmail] = otpCode;
        return Task.CompletedTask;
    }

    public Task SendSignupOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        _signupOtps[toEmail] = otpCode;
        return Task.CompletedTask;
    }

    public string GetSignupOtp(string email) => _signupOtps[email];
    public string GetPasswordResetOtp(string email) => _passwordResetOtps[email];
}
