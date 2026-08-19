using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using HappyPaws.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Infrastructure.Services;

/// <summary>
/// Sends transactional emails via Amazon Simple Email Service (SES).
/// Falls back to instance-profile credentials when no access key is configured.
/// </summary>
public sealed class SesEmailSender : IEmailSender
{
    private readonly AmazonSimpleEmailServiceClient _sesClient;
    private readonly ILogger<SesEmailSender> _logger;
    private readonly string _fromAddress;

    public SesEmailSender(IConfiguration configuration, ILogger<SesEmailSender> logger)
    {
        _logger = logger;
        
        var accessKey = configuration["Ses:AccessKey"];
        var secretKey = configuration["Ses:SecretKey"];
        var regionString = configuration["Ses:Region"] ?? "us-east-1";
        var region = RegionEndpoint.GetBySystemName(regionString);
        
        _fromAddress = configuration["Ses:FromAddress"] ?? "noreply@happypaws.lk";

        if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
        {
            _sesClient = new AmazonSimpleEmailServiceClient(accessKey, secretKey, region);
        }
        else
        {
            _sesClient = new AmazonSimpleEmailServiceClient(region); // Fallback to instance profile if no keys provided
        }
    }

    /// <summary>
    /// Sends a plain-text OTP email for identity verification.
    /// </summary>
    public async Task SendOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        var request = new SendEmailRequest
        {
            Source = _fromAddress,
            Destination = new Destination { ToAddresses = new List<string> { toEmail } },
            Message = new Message
            {
                Subject = new Content("Your HappyPaws.lk OTP Code"),
                Body = new Body
                {
                    Text = new Content($"Your verification code is: {otpCode}. It will expire in a few minutes.")
                }
            }
        };

        await _sesClient.SendEmailAsync(request, cancellationToken);
        _logger.LogInformation("Sent OTP email via SES to {Email}", toEmail);
    }

    /// <summary>
    /// Sends a KYC approval or rejection email. Includes the rejection reason when <paramref name="approved"/> is false.
    /// </summary>
    public async Task SendVerificationDecisionAsync(string toEmail, bool approved, string? reason, CancellationToken cancellationToken = default)
    {
        var subject = approved ? "HappyPaws.lk - KYC Approved" : "HappyPaws.lk - KYC Rejected";
        var body = approved
            ? "Your identity verification has been approved. You now have full access to HappyPaws.lk features."
            : $"Your identity verification was rejected. Reason: {reason ?? "Not provided"}. Please submit a valid document.";

        var request = new SendEmailRequest
        {
            Source = _fromAddress,
            Destination = new Destination { ToAddresses = new List<string> { toEmail } },
            Message = new Message
            {
                Subject = new Content(subject),
                Body = new Body { Text = new Content(body) }
            }
        };

        await _sesClient.SendEmailAsync(request, cancellationToken);
        _logger.LogInformation("Sent verification decision email via SES to {Email}", toEmail);
    }

    /// <summary>
    /// Sends a password reset OTP email with a 15-minute expiry notice.
    /// </summary>
    public async Task SendPasswordResetOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        var request = new SendEmailRequest
        {
            Source = _fromAddress,
            Destination = new Destination { ToAddresses = new List<string> { toEmail } },
            Message = new Message
            {
                Subject = new Content("Reset your HappyPaws.lk password"),
                Body = new Body
                {
                    Text = new Content($"Your password reset code is: {otpCode}. It expires in 15 minutes. If you did not request this, ignore this email.")
                }
            }
        };

        await _sesClient.SendEmailAsync(request, cancellationToken);
        _logger.LogInformation("Sent password reset OTP email via SES to {Email}", toEmail);
    }

    /// <summary>
    /// Sends an email verification OTP for a new sign-up with a 10-minute expiry notice.
    /// </summary>
    public async Task SendSignupOtpAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        var request = new SendEmailRequest
        {
            Source = _fromAddress,
            Destination = new Destination { ToAddresses = new List<string> { toEmail } },
            Message = new Message
            {
                Subject = new Content("Verify your email for HappyPaws.lk"),
                Body = new Body
                {
                    Text = new Content($"Your sign-up verification code is: {otpCode}. It expires in 10 minutes. If you did not request this, ignore this email.")
                }
            }
        };

        await _sesClient.SendEmailAsync(request, cancellationToken);
        _logger.LogInformation("Sent signup OTP email via SES to {Email}", toEmail);
    }
}
