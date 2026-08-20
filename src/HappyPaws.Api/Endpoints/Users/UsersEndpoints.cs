using System.Security.Claims;
using System.Security.Cryptography;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Filters;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace HappyPaws.Api.Endpoints.Users;

public class UsersEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("/me", GetMeAsync)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Get the authenticated user's profile")
            .WithDescription("Returns the authenticated user's profile including avatar URL, verification status, reputation points, and earned badges.")
            .Produces<UserProfileResponse>();

        group.MapPut("/me", UpdateMeAsync)
            .RequireAuthorization()
            .WithName("UpdateCurrentUser")
            .WithSummary("Update name, avatar, or location for the authenticated user")
            .WithDescription("Updates name, avatar image, or last known location. Accepts multipart/form-data. All fields are optional.")
            .Produces<UserProfileResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .DisableAntiforgery();

        group.MapGet("/{id:guid}", GetPublicUserAsync)
            .CacheOutput("UserProfile30")
            .WithName("GetPublicUser")
            .WithSummary("Get a user's public profile")
            .WithDescription("Returns the public-facing profile for any user: name, reputation points, and badges. Results are cached for 30 seconds.")
            .Produces<PublicUserResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/me/devices", GetDevicesAsync)
            .RequireAuthorization()
            .WithName("GetDevices")
            .WithSummary("List registered FCM devices for the authenticated user")
            .WithDescription("Returns all FCM device tokens registered to the authenticated user.")
            .Produces<List<DeviceResponse>>();

        group.MapPost("/me/devices", RegisterDeviceAsync)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<DeviceRequest>>()
            .WithName("RegisterDevice")
            .WithSummary("Register or refresh an FCM device token")
            .WithDescription("Registers a new FCM device or refreshes an existing one. If the FCM token already exists, it updates the device name and last active timestamp instead of creating a duplicate.")
            .Produces<DeviceResponse>()
            .ProducesValidationProblem();

        group.MapDelete("/me/devices/{id:guid}", RemoveDeviceAsync)
            .RequireAuthorization()
            .WithName("RemoveDevice")
            .WithSummary("Remove a registered FCM device")
            .WithDescription("Unregisters a device so it no longer receives push notifications. Returns 403 if the request targets the caller's own current device.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/me/profile", GetMeProfileAsync)
            .RequireAuthorization()
            .WithName("GetMeProfile")
            .WithSummary("Get the authenticated user's account profile")
            .WithDescription("Returns the full account profile including all user fields, roles, and suspension status.")
            .Produces<MeProfileResponse>();

        group.MapPut("/me/profile", UpdateMeProfileAsync)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<UpdateMeProfileRequest>>()
            .WithName("UpdateMeProfile")
            .WithSummary("Update the authenticated user's display name")
            .WithDescription("Updates the user's display name. To change the avatar, use POST /me/avatar instead.")
            .Produces<MeProfileResponse>()
            .ProducesValidationProblem();

        group.MapPost("/me/avatar", UploadAvatarAsync)
            .RequireAuthorization()
            .WithName("UploadAvatar")
            .WithSummary("Upload or replace the authenticated user's avatar")
            .WithDescription("Accepts a single image file (jpg, jpeg, png, or webp, max 5 MB). If the user already has an avatar, the old file is deleted from storage before the new one is saved. Returns the storage key and public URL of the new avatar.")
            .Produces<AvatarUploadResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting("UploadLimiter")
            .AddEndpointFilter(new RequestSizeLimitFilter(5_242_880))
            .DisableAntiforgery();

        group.MapDelete("/me/avatar", DeleteAvatarAsync)
            .RequireAuthorization()
            .WithName("DeleteAvatar")
            .WithSummary("Remove the authenticated user's avatar")
            .WithDescription("Deletes the user's avatar image from storage and clears the avatar reference.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/me/change-password", ChangePasswordAsync)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<ChangePasswordRequest>>()
            .WithName("ChangePassword")
            .WithSummary("Change the authenticated user's password")
            .WithDescription("Verifies the current password and replaces it with the new one. Returns 400 if the current password is incorrect.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesValidationProblem();

        group.MapGet("/me/lifestyle-profile", GetLifestyleProfileAsync)
            .RequireAuthorization()
            .WithName("GetLifestyleProfile")
            .WithSummary("Get the authenticated user's lifestyle profile")
            .WithDescription("Returns the user's lifestyle compatibility profile used for the animal matching algorithm.")
            .Produces<LifestyleProfileResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/me/lifestyle-profile", UpsertLifestyleProfileAsync)
            .RequireAuthorization("Verified")
            .AddEndpointFilter<ValidationFilter<LifestyleProfileRequest>>()
            .WithName("UpsertLifestyleProfile")
            .WithSummary("Create or update the authenticated user's lifestyle profile")
            .WithDescription("Creates or replaces the lifestyle profile. This data powers the GET /listings/matches endpoint.")
            .Produces<LifestyleProfileResponse>()
            .ProducesValidationProblem();

        group.MapPost("/me/kyc", UploadKycAsync)
            .RequireAuthorization()
            .WithName("UploadKyc")
            .WithSummary("Upload a KYC identity document")
            .WithDescription("Uploads an identity document for KYC verification. The document is stored privately and only accessible to admins via a short-lived presigned URL.")
            .Produces<KycDocumentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireRateLimiting("UploadLimiter")
            .AddEndpointFilter(new RequestSizeLimitFilter(10_485_760))
            .DisableAntiforgery();

        group.MapPost("/me/roles", AssignRoleAsync)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<AssignRoleRequest>>()
            .WithName("AssignRole")
            .WithSummary("Add a role to the authenticated user's account")
            .WithDescription("Assigns an additional role to the user. Admin and Veterinarian roles cannot be self-assigned. Roles requiring KYC (Foster, Transporter, Sponsor) require the user to be verified first.")
            .Produces<RoleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapGet("/me/kyc", GetMyKycDocumentsAsync)
            .RequireAuthorization()
            .WithName("GetMyKycDocuments")
            .WithSummary("Get the authenticated user's KYC documents")
            .WithDescription("Returns all identity documents submitted by the user, ordered by upload date descending.")
            .Produces<List<KycDocumentResponse>>();

        group.MapPost("/me/role-requests", SubmitRoleRequestAsync)
            .RequireAuthorization()
            .WithName("SubmitRoleRequest")
            .WithSummary("Submit a role request with a supporting document")
            .WithDescription("Creates a pending role request. An admin must approve the request before the role is granted. Roles Admin and Adopter cannot be requested. Duplicate pending requests for the same role are rejected with 409.")
            .Produces<RoleRequestResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRateLimiting("UploadLimiter")
            .AddEndpointFilter(new RequestSizeLimitFilter(10_485_760))
            .DisableAntiforgery();

        group.MapGet("/me/role-requests", GetMyRoleRequestsAsync)
            .RequireAuthorization()
            .WithName("GetMyRoleRequests")
            .WithSummary("Get the authenticated user's role requests")
            .WithDescription("Returns all role requests submitted by the user, ordered by creation date descending.")
            .Produces<List<RoleRequestResponse>>();

        group.MapPost("/me/email/request-change", RequestEmailChangeAsync)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<RequestEmailChangeRequest>>()
            .RequireRateLimiting("OtpLimiter")
            .WithName("RequestEmailChange")
            .WithSummary("Request an email change by verifying current password and sending OTP code")
            .WithDescription("Verifies the user's current password and dispatches a 6-digit OTP code to the requested new email address.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapPost("/me/email/confirm-change", ConfirmEmailChangeAsync)
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<ConfirmEmailChangeRequest>>()
            .WithName("ConfirmEmailChange")
            .WithSummary("Confirm email change using 6-digit OTP code")
            .WithDescription("Validates the 6-digit OTP code sent to the new email address and updates the user's email on success.")
            .Produces<MeProfileResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();
    }

    private static async Task<Ok<UserProfileResponse>> GetMeAsync(
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var projection = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.AvatarKey,
                u.IsVerified,
                u.ReputationPoints,
                Badges = u.Badges.Select(b => new { b.BadgeType, b.AwardedAt }).ToList()
            })
            .FirstAsync(ct);

        var avatarUrl = projection.AvatarKey is not null ? storageService.GetPublicUrl(projection.AvatarKey) : null;

        return TypedResults.Ok(new UserProfileResponse(
            projection.Id,
            projection.Name,
            projection.Email,
            avatarUrl,
            projection.IsVerified,
            projection.ReputationPoints,
            projection.Badges.Select(b => new BadgeResponse(b.BadgeType.ToString(), b.AwardedAt))));
    }

    private static async Task<Results<Ok<UserProfileResponse>, BadRequest<string>>> UpdateMeAsync(
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        IOutputCacheStore cacheStore,
        IFormFile? avatar,
        string? name,
        double? latitude,
        double? longitude,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var user = await db.Users.Include(u => u.Badges).FirstAsync(u => u.Id == userId, ct);

        if (name is not null)
        {
            if (name.Length < 2 || name.Length > 100)
                return TypedResults.BadRequest("Name must be between 2 and 100 characters");
            user.Name = name;
        }

        if (avatar is not null)
        {
            if (avatar.Length > 5 * 1024 * 1024)
                return TypedResults.BadRequest("Avatar must not exceed 5MB");

            if (user.AvatarKey is not null)
                await storageService.DeleteAsync(user.AvatarKey, ct);

            var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
            var key = $"avatars/{userId}/{Guid.NewGuid()}{extension}";

            await using var stream = avatar.OpenReadStream();
            await storageService.UploadAsync(key, stream, avatar.ContentType, cancellationToken: ct);
            user.AvatarKey = key;
        }

        if (latitude.HasValue && longitude.HasValue)
        {
            user.LastKnownLocation = new Point(longitude.Value, latitude.Value) { SRID = 4326 };
        }

        await db.SaveChangesAsync(ct);

        await cacheStore.EvictByTagAsync("users", ct);

        var avatarUrl = user.AvatarKey is not null ? storageService.GetPublicUrl(user.AvatarKey) : null;

        return TypedResults.Ok(new UserProfileResponse(
            user.Id,
            user.Name,
            user.Email,
            avatarUrl,
            user.IsVerified,
            user.ReputationPoints,
            user.Badges.Select(b => new BadgeResponse(b.BadgeType.ToString(), b.AwardedAt))));
    }

    private static async Task<Results<Ok<PublicUserResponse>, NotFound>> GetPublicUserAsync(
        Guid id,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var projection = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.ReputationPoints,
                Badges = u.Badges.Select(b => new { b.BadgeType, b.AwardedAt }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (projection is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(new PublicUserResponse(
            projection.Id,
            projection.Name,
            projection.ReputationPoints,
            projection.Badges.Select(b => new BadgeResponse(b.BadgeType.ToString(), b.AwardedAt))));
    }

    private static async Task<Ok<List<DeviceResponse>>> GetDevicesAsync(
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var currentDeviceIdHeader = httpContext.Request.Headers["X-Device-Id"].FirstOrDefault();
        Guid? currentDeviceId = Guid.TryParse(currentDeviceIdHeader, out var parsedId) ? parsedId : null;

        var devices = await db.UserDevices
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => new DeviceResponse(d.Id, d.FcmToken, d.DeviceName, d.Platform, d.LastActiveAt,
                currentDeviceId.HasValue && d.Id == currentDeviceId.Value))
            .ToListAsync(ct);

        return TypedResults.Ok(devices);
    }

    private static async Task<Ok<DeviceResponse>> RegisterDeviceAsync(
        DeviceRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var existing = await db.UserDevices
            .FirstOrDefaultAsync(d => d.FcmToken == request.FcmToken, ct);

        if (existing is not null)
        {
            existing.LastActiveAt = DateTimeOffset.UtcNow;
            existing.DeviceName = request.DeviceName;
            existing.UserId = userId;
            await db.SaveChangesAsync(ct);

            return TypedResults.Ok(new DeviceResponse(existing.Id, existing.FcmToken, existing.DeviceName, existing.Platform, existing.LastActiveAt, true));
        }

        var device = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FcmToken = request.FcmToken,
            DeviceName = request.DeviceName,
            Platform = request.Platform,
            LastActiveAt = DateTimeOffset.UtcNow
        };

        db.UserDevices.Add(device);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new DeviceResponse(device.Id, device.FcmToken, device.DeviceName, device.Platform, device.LastActiveAt, true));
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> RemoveDeviceAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var currentDeviceIdHeader = httpContext.Request.Headers["X-Device-Id"].FirstOrDefault();
        if (Guid.TryParse(currentDeviceIdHeader, out var currentDeviceId) && id == currentDeviceId)
            return TypedResults.Forbid();

        var userId = principal.GetUserId();

        var device = await db.UserDevices
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId, ct);

        if (device is null)
            return TypedResults.NotFound();

        db.UserDevices.Remove(device);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<MeProfileResponse>> GetMeProfileAsync(
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .FirstAsync(u => u.Id == userId, ct);

        var location = user.LastKnownLocation is not null
            ? new LocationResponse(user.LastKnownLocation.Y, user.LastKnownLocation.X)
            : null;

        return TypedResults.Ok(new MeProfileResponse(
            user.Id,
            user.Name,
            user.Email,
            user.AvatarKey,
            user.IsVerified,
            user.ReputationPoints,
            user.IsSuspended,
            user.SuspendedAt,
            user.SuspendedReason,
            user.CreatedAt,
            user.UpdatedAt,
            location,
            user.Roles.Select(r => new RoleResponse(r.Role.ToString(), r.AssignedAt))));
    }

    private static async Task<Ok<MeProfileResponse>> UpdateMeProfileAsync(
        UpdateMeProfileRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var user = await db.Users
            .Include(u => u.Roles)
            .FirstAsync(u => u.Id == userId, ct);

        user.Name = request.Name;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var location = user.LastKnownLocation is not null
            ? new LocationResponse(user.LastKnownLocation.Y, user.LastKnownLocation.X)
            : null;

        return TypedResults.Ok(new MeProfileResponse(
            user.Id,
            user.Name,
            user.Email,
            user.AvatarKey,
            user.IsVerified,
            user.ReputationPoints,
            user.IsSuspended,
            user.SuspendedAt,
            user.SuspendedReason,
            user.CreatedAt,
            user.UpdatedAt,
            location,
            user.Roles.Select(r => new RoleResponse(r.Role.ToString(), r.AssignedAt))));
    }

    private static readonly HashSet<string> AllowedAvatarExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private static async Task<Results<Ok<AvatarUploadResponse>, BadRequest<string>>> UploadAvatarAsync(
        IFormFile file,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedAvatarExtensions.Contains(extension))
            return TypedResults.BadRequest("File must be .jpg, .jpeg, .png, or .webp");

        if (file.Length > 5 * 1024 * 1024)
            return TypedResults.BadRequest("Avatar must not exceed 5 MB");

        var userId = principal.GetUserId();
        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);

        if (user.AvatarKey is not null)
            await storageService.DeleteAsync(user.AvatarKey, ct);

        var key = $"avatars/{userId}/{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();
        await storageService.UploadAsync(key, stream, file.ContentType, cancellationToken: ct);

        user.AvatarKey = key;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new AvatarUploadResponse(key, storageService.GetPublicUrl(key)));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAvatarAsync(
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        IOutputCacheStore cacheStore,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);

        if (user.AvatarKey is null)
            return TypedResults.NotFound();

        await storageService.DeleteAsync(user.AvatarKey, ct);
        user.AvatarKey = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await cacheStore.EvictByTagAsync("users", ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok, BadRequest<string>>> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);

        var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verifyResult == PasswordVerificationResult.Failed)
            return TypedResults.BadRequest("Current password is incorrect");

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<LifestyleProfileResponse>, NotFound>> GetLifestyleProfileAsync(
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var profile = await db.LifestyleProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(new LifestyleProfileResponse(
            profile.HomeSize,
            profile.ActivityLevel,
            profile.ExistingPetTypes,
            profile.HasChildren,
            profile.HasYard,
            profile.UpdatedAt));
    }

    private static async Task<Ok<LifestyleProfileResponse>> UpsertLifestyleProfileAsync(
        LifestyleProfileRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var profile = await db.LifestyleProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (profile is null)
        {
            profile = new LifestyleProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            db.LifestyleProfiles.Add(profile);
        }

        profile.HomeSize = request.HomeSize;
        profile.ActivityLevel = request.ActivityLevel;
        profile.ExistingPetTypes = request.ExistingPetTypes;
        profile.HasChildren = request.HasChildren;
        profile.HasYard = request.HasYard;

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new LifestyleProfileResponse(
            profile.HomeSize,
            profile.ActivityLevel,
            profile.ExistingPetTypes,
            profile.HasChildren,
            profile.HasYard,
            profile.UpdatedAt));
    }

    private static async Task<Results<Created<KycDocumentResponse>, BadRequest<string>>> UploadKycAsync(
        IFormFile document,
        DocumentType documentType,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        if (document.Length > 10 * 1024 * 1024)
            return TypedResults.BadRequest("Document must not exceed 10MB");

        var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
        var key = $"kyc/{userId}/{Guid.NewGuid()}{extension}";

        await using var stream = document.OpenReadStream();
        await storageService.UploadAsync(key, stream, document.ContentType, isPrivate: true, ct);

        var identityDocument = new IdentityDocument
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentKey = key,
            DocumentType = documentType,
            Status = DocumentStatus.Pending,
            UploadedAt = DateTimeOffset.UtcNow
        };

        db.IdentityDocuments.Add(identityDocument);
        await db.SaveChangesAsync(ct);

        var response = new KycDocumentResponse(
            identityDocument.Id,
            identityDocument.DocumentType,
            identityDocument.Status,
            null,
            identityDocument.UploadedAt,
            null);

        return TypedResults.Created($"/api/v1/users/me/kyc", response);
    }

    private static readonly HashSet<Role> RoleRequestRequiredRoles =
        [Role.Foster, Role.Transporter, Role.Sponsor, Role.Veterinarian];

    private static async Task<Ok<List<KycDocumentResponse>>> GetMyKycDocumentsAsync(
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var documents = await db.IdentityDocuments
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new KycDocumentResponse(
                d.Id,
                d.DocumentType,
                d.Status,
                d.RejectionReason,
                d.UploadedAt,
                d.ReviewedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(documents);
    }

    private static async Task<Results<Created<RoleRequestResponse>, BadRequest<string>, Conflict<string>>> SubmitRoleRequestAsync(
        IFormFile document,
        Role role,
        DocumentType documentType,
        string? justification,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        if (role == Role.Admin)
            return TypedResults.BadRequest("The Admin role cannot be requested.");

        if (role == Role.Adopter)
            return TypedResults.BadRequest("Adopter is the default role and does not require a request.");

        var hasPending = await db.RoleRequests
            .AnyAsync(r => r.UserId == userId && r.Role == role && r.Status == RoleRequestStatus.Pending, ct);

        if (hasPending)
            return TypedResults.Conflict("A pending request for this role already exists.");

        var alreadyAssigned = await db.UserRoles
            .AnyAsync(r => r.UserId == userId && r.Role == role, ct);

        if (alreadyAssigned)
            return TypedResults.Conflict("You already have this role.");

        if (document is null || document.Length == 0)
            return TypedResults.BadRequest("A supporting document is required.");

        if (document.Length > 10 * 1024 * 1024)
            return TypedResults.BadRequest("Document must not exceed 10 MB.");

        var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
        var key = $"role-requests/{userId}/{Guid.NewGuid()}{extension}";

        await using var stream = document.OpenReadStream();
        await storageService.UploadAsync(key, stream, document.ContentType, isPrivate: true, ct);

        var roleRequest = new RoleRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Role = role,
            DocumentType = documentType,
            DocumentKey = key,
            Status = RoleRequestStatus.Pending,
            Justification = string.IsNullOrWhiteSpace(justification) ? null : justification.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.RoleRequests.Add(roleRequest);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(
            $"/api/v1/users/me/role-requests/{roleRequest.Id}",
            new RoleRequestResponse(
                roleRequest.Id,
                roleRequest.Role.ToString(),
                roleRequest.Status,
                roleRequest.Justification,
                null,
                roleRequest.CreatedAt,
                null));
    }

    private static async Task<Ok<List<RoleRequestResponse>>> GetMyRoleRequestsAsync(
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var requests = await db.RoleRequests
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RoleRequestResponse(
                r.Id,
                r.Role.ToString(),
                r.Status,
                r.Justification,
                r.RejectionReason,
                r.CreatedAt,
                r.ReviewedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(requests);
    }

    private static async Task<Results<Created<RoleResponse>, BadRequest<string>, Conflict<string>>> AssignRoleAsync(
        AssignRoleRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        if (request.Role == Role.Admin)
            return TypedResults.BadRequest("The Admin role cannot be self-assigned.");

        if (RoleRequestRequiredRoles.Contains(request.Role))
            return TypedResults.BadRequest($"The {request.Role} role requires a role request and admin approval. Use POST /me/role-requests instead.");

        var user = await db.Users
            .Include(u => u.Roles)
            .FirstAsync(u => u.Id == userId, ct);

        if (user.Roles.Any(r => r.Role == request.Role))
            return TypedResults.Conflict("You already have this role.");

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Role = request.Role,
            AssignedAt = DateTimeOffset.UtcNow
        };

        db.UserRoles.Add(userRole);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(
            $"/api/v1/users/me/roles",
            new RoleResponse(userRole.Role.ToString(), userRole.AssignedAt));
    }

    private static async Task<Results<Ok, BadRequest<string>, Conflict<string>>> RequestEmailChangeAsync(
        RequestEmailChangeRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        IEmailSender emailSender,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var user = await db.Users.FirstAsync(u => u.Id == userId, ct);

        // Verify current password
        var passResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (passResult == PasswordVerificationResult.Failed)
            return TypedResults.BadRequest("Current password is incorrect.");

        var newEmail = request.NewEmail.Trim().ToLowerInvariant();
        if (newEmail == user.Email.ToLowerInvariant())
            return TypedResults.BadRequest("New email address must be different from your current email address.");

        // Check if email is already taken
        var isTaken = await db.Users.AnyAsync(u => u.Email.ToLower() == newEmail && u.Id != userId, ct);
        if (isTaken)
            return TypedResults.Conflict("A user with this email address already exists.");

        // Generate 6-digit OTP code
        var code = GenerateOtpCode();
        var hashedCode = passwordHasher.HashPassword(user, code);

        db.OtpCodes.Add(new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = hashedCode,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        });

        await db.SaveChangesAsync(ct);

        await emailSender.SendOtpAsync(newEmail, code, ct);

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<MeProfileResponse>, BadRequest<string>, Conflict<string>>> ConfirmEmailChangeAsync(
        ConfirmEmailChangeRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IPasswordHasher<User> passwordHasher,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var user = await db.Users
            .Include(u => u.Roles)
            .FirstAsync(u => u.Id == userId, ct);

        var newEmail = request.NewEmail.Trim().ToLowerInvariant();
        var isTaken = await db.Users.AnyAsync(u => u.Email.ToLower() == newEmail && u.Id != userId, ct);
        if (isTaken)
            return TypedResults.Conflict("A user with this email address already exists.");

        // Find active OTP code for user
        var now = DateTimeOffset.UtcNow;
        var validCodes = await db.OtpCodes
            .Where(o => o.UserId == userId && o.ExpiresAt > now)
            .ToListAsync(ct);

        var matchingOtp = validCodes.FirstOrDefault(o =>
            passwordHasher.VerifyHashedPassword(user, o.Code, request.Code) != PasswordVerificationResult.Failed);

        if (matchingOtp is null)
            return TypedResults.BadRequest("Invalid or expired verification code.");

        // Consume OTP and update email
        db.OtpCodes.RemoveRange(validCodes);
        user.Email = newEmail;
        user.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        var location = user.LastKnownLocation is not null
            ? new LocationResponse(user.LastKnownLocation.Y, user.LastKnownLocation.X)
            : null;

        return TypedResults.Ok(new MeProfileResponse(
            user.Id,
            user.Name,
            user.Email,
            user.AvatarKey,
            user.IsVerified,
            user.ReputationPoints,
            user.IsSuspended,
            user.SuspendedAt,
            user.SuspendedReason,
            user.CreatedAt,
            user.UpdatedAt,
            location,
            user.Roles.Select(r => new RoleResponse(r.Role.ToString(), r.AssignedAt))));
    }

    private static string GenerateOtpCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
    }
}
