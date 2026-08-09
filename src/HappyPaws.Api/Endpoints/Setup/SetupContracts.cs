namespace HappyPaws.Api.Endpoints.Setup;

/// <summary>Reports whether first-time setup has been completed.</summary>
/// <param name="IsSetupComplete">True once an admin account exists on this instance.</param>
public record SetupStatusResponse(bool IsSetupComplete);

/// <summary>Data submitted to create the first admin account.</summary>
/// <param name="Name">The admin's display name.</param>
/// <param name="Email">The admin's email address.</param>
/// <param name="Password">The admin's plain-text password. Stored as a PBKDF2 hash.</param>
public record SetupCompleteRequest(string Name, string Email, string Password);
