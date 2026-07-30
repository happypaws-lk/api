using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Users;

public record UserProfileResponse(
    Guid Id,
    string Name,
    string Email,
    string? AvatarUrl,
    bool IsVerified,
    int ReputationPoints,
    IEnumerable<BadgeResponse> Badges);

public record BadgeResponse(string BadgeType, DateTimeOffset AwardedAt);

public record PublicUserResponse(
    Guid Id,
    string Name,
    int ReputationPoints,
    IEnumerable<BadgeResponse> Badges);

public record UpdateProfileRequest(string? Name);

public record DeviceRequest(string FcmToken, string? DeviceName, Platform Platform);

public record DeviceResponse(
    Guid Id,
    string FcmToken,
    string? DeviceName,
    Platform Platform,
    DateTimeOffset LastActiveAt);

public record LifestyleProfileRequest(
    HomeSize HomeSize,
    ActivityLevel ActivityLevel,
    List<string>? ExistingPetTypes,
    bool HasChildren,
    bool HasYard);

public record LifestyleProfileResponse(
    HomeSize HomeSize,
    ActivityLevel ActivityLevel,
    List<string>? ExistingPetTypes,
    bool HasChildren,
    bool HasYard,
    DateTimeOffset UpdatedAt);

public record KycDocumentResponse(
    Guid Id,
    DocumentType DocumentType,
    DocumentStatus Status,
    string? RejectionReason,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ReviewedAt);
