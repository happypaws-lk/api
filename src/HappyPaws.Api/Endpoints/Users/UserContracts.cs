using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Users;

/// <summary>The authenticated user's full profile.</summary>
/// <param name="Id">Unique identifier of the user.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="AvatarUrl">Public URL of the user's avatar image. Null if no avatar has been uploaded.</param>
/// <param name="IsVerified">Whether the user has completed KYC verification.</param>
/// <param name="ReputationPoints">The user's total reputation score.</param>
/// <param name="Badges">Badges the user has earned.</param>
public record UserProfileResponse(
    Guid Id,
    string Name,
    string Email,
    string? AvatarUrl,
    bool IsVerified,
    int ReputationPoints,
    IEnumerable<BadgeResponse> Badges);

/// <summary>A badge earned by a user.</summary>
/// <param name="BadgeType">The type identifier of the badge.</param>
/// <param name="AwardedAt">UTC timestamp when the badge was awarded.</param>
public record BadgeResponse(string BadgeType, DateTimeOffset AwardedAt);

/// <summary>A user's public profile, visible to all authenticated users.</summary>
/// <param name="Id">Unique identifier of the user.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="ReputationPoints">The user's total reputation score.</param>
/// <param name="Badges">Badges the user has earned.</param>
public record PublicUserResponse(
    Guid Id,
    string Name,
    int ReputationPoints,
    IEnumerable<BadgeResponse> Badges);

/// <summary>Data submitted when updating the authenticated user's profile name.</summary>
/// <param name="Name">The new display name. Optional.</param>
public record UpdateProfileRequest(string? Name);

/// <summary>Data submitted when registering an FCM device for push notifications.</summary>
/// <param name="FcmToken">The Firebase Cloud Messaging device token.</param>
/// <param name="DeviceName">A friendly label for the device (for example, "My iPhone"). Optional.</param>
/// <param name="Platform">The device platform (Android or iOS).</param>
public record DeviceRequest(string FcmToken, string? DeviceName, Platform Platform);

/// <summary>A registered FCM device belonging to a user.</summary>
/// <param name="Id">Unique identifier of the device record.</param>
/// <param name="FcmToken">The Firebase Cloud Messaging token for this device.</param>
/// <param name="DeviceName">Friendly label for the device. Null if not provided.</param>
/// <param name="Platform">The device platform.</param>
/// <param name="LastActiveAt">UTC timestamp when this device last registered or refreshed its token.</param>
public record DeviceResponse(
    Guid Id,
    string FcmToken,
    string? DeviceName,
    Platform Platform,
    DateTimeOffset LastActiveAt);

/// <summary>Data submitted when creating or updating the lifestyle profile used for animal matching.</summary>
/// <param name="HomeSize">The size of the user's home.</param>
/// <param name="ActivityLevel">The user's activity level.</param>
/// <param name="ExistingPetTypes">Types of pets the user already owns. Null or empty if none.</param>
/// <param name="HasChildren">Whether the user has children at home.</param>
/// <param name="HasYard">Whether the user has a yard or outdoor space.</param>
public record LifestyleProfileRequest(
    HomeSize HomeSize,
    ActivityLevel ActivityLevel,
    List<string>? ExistingPetTypes,
    bool HasChildren,
    bool HasYard);

/// <summary>The user's lifestyle compatibility profile used for animal matching.</summary>
/// <param name="HomeSize">The size of the user's home.</param>
/// <param name="ActivityLevel">The user's activity level.</param>
/// <param name="ExistingPetTypes">Types of pets the user already owns. Null or empty if none.</param>
/// <param name="HasChildren">Whether the user has children at home.</param>
/// <param name="HasYard">Whether the user has a yard or outdoor space.</param>
/// <param name="UpdatedAt">UTC timestamp when this profile was last saved.</param>
public record LifestyleProfileResponse(
    HomeSize HomeSize,
    ActivityLevel ActivityLevel,
    List<string>? ExistingPetTypes,
    bool HasChildren,
    bool HasYard,
    DateTimeOffset UpdatedAt);

/// <summary>A KYC identity document uploaded by the user.</summary>
/// <param name="Id">Unique identifier of the document record.</param>
/// <param name="DocumentType">The type of identity document.</param>
/// <param name="Status">The current review status of the document.</param>
/// <param name="RejectionReason">The reason for rejection if the document was rejected. Null otherwise.</param>
/// <param name="UploadedAt">UTC timestamp when the document was uploaded.</param>
/// <param name="ReviewedAt">UTC timestamp when an admin reviewed the document. Null if not yet reviewed.</param>
public record KycDocumentResponse(
    Guid Id,
    DocumentType DocumentType,
    DocumentStatus Status,
    string? RejectionReason,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ReviewedAt);
