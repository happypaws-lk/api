using System.Security.Claims;
using HappyPaws.Api.Endpoints.Applications;
using HappyPaws.Api.Extensions;
using HappyPaws.Api.Filters;
using HappyPaws.Core.Common;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Interfaces;
using HappyPaws.Core.Services;
using HappyPaws.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace HappyPaws.Api.Endpoints.Listings;

public class ListingsEndpoints : IEndpointGroup
{
    public void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", CreateListingAsync)
            .RequireAuthorization("Verified")
            .AddEndpointFilter<HtmlSanitizationFilter<CreateListingRequest>>()
            .AddEndpointFilter<ValidationFilter<CreateListingRequest>>()
            .WithName("CreateListing")
            .WithSummary("Create a new animal adoption listing");

        group.MapGet("/", BrowseListingsAsync)
            .CacheOutput("Listings30")
            .WithName("BrowseListings")
            .WithSummary("Browse active animal listings with filters");

        group.MapGet("/{id:guid}", GetListingAsync)
            .CacheOutput("Listings30")
            .WithName("GetListing")
            .WithSummary("Get full details of a single listing");

        group.MapPut("/{id:guid}", UpdateListingAsync)
            .RequireAuthorization("Verified")
            .AddEndpointFilter<HtmlSanitizationFilter<UpdateListingRequest>>()
            .AddEndpointFilter<ValidationFilter<UpdateListingRequest>>()
            .WithName("UpdateListing")
            .WithSummary("Update a listing owned by the authenticated user");

        group.MapPut("/{id:guid}/status", UpdateStatusAsync)
            .RequireAuthorization("Verified")
            .AddEndpointFilter<ValidationFilter<UpdateListingStatusRequest>>()
            .WithName("UpdateListingStatus")
            .WithSummary("Update the adoption status of a listing");

        group.MapDelete("/{id:guid}", DeleteListingAsync)
            .RequireAuthorization("Verified")
            .WithName("DeleteListing")
            .WithSummary("Soft-delete a listing");

        group.MapGet("/matches", GetMatchesAsync)
            .RequireAuthorization("Verified")
            .WithName("GetListingMatches")
            .WithSummary("Get animal listings matched to the user's lifestyle profile");

        group.MapGet("/{id:guid}/photos", GetPhotosAsync)
            .CacheOutput("Listings30")
            .WithName("GetListingPhotos")
            .WithSummary("Get all photos for a listing");

        group.MapPost("/{id:guid}/photos", UploadPhotoAsync)
            .RequireAuthorization("Verified")
            .DisableAntiforgery()
            .RequireRateLimiting("UploadLimiter")
            .AddEndpointFilter(new RequestSizeLimitFilter(10_485_760))
            .WithName("UploadListingPhoto")
            .WithSummary("Upload a photo to a listing");

        group.MapDelete("/{id:guid}/photos/{photoId:guid}", DeletePhotoAsync)
            .RequireAuthorization("Verified")
            .WithName("DeleteListingPhoto")
            .WithSummary("Delete a photo from a listing");

        group.MapGet("/{id:guid}/applications", GetListingApplicationsAsync)
            .RequireAuthorization()
            .WithName("GetListingApplications")
            .WithSummary("Get all adoption applications for a listing (owner only)");
    }

    private static async Task<Results<Created<ListingDetailResponse>, ForbidHttpResult, NotFound<string>>> CreateListingAsync(
        CreateListingRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IOutputCacheStore cacheStore,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        if (request.RescueCaseId.HasValue)
        {
            var rescueCase = await db.RescueCases.FirstOrDefaultAsync(rc => rc.Id == request.RescueCaseId, ct);
            if (rescueCase is null)
                return TypedResults.NotFound("Rescue case not found.");

            if (rescueCase.Status != CaseStatus.Resolved || rescueCase.AssignedFosterId != userId)
                return TypedResults.Forbid();
        }

        var listing = new AnimalListing
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            RescueCaseId = request.RescueCaseId,
            Name = request.Name,
            Species = request.Species,
            Breed = request.Breed,
            AgeMonths = request.AgeMonths,
            AgeLabel = request.AgeLabel,
            Gender = request.Gender,
            Size = request.Size,
            ActivityLevel = request.ActivityLevel,
            Description = request.Description,
            LocationCoords = new Point(request.Longitude, request.Latitude) { SRID = 4326 },
            LocationName = request.LocationName,
            Status = ListingStatus.Available,
            IsActive = true
        };

        db.AnimalListings.Add(listing);
        await db.SaveChangesAsync(ct);

        await cacheStore.EvictByTagAsync("listings", ct);

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);

        var response = new ListingDetailResponse(
            listing.Id, listing.OwnerId, user.Name, listing.RescueCaseId, listing.Name, listing.Species, listing.Breed,
            listing.AgeMonths, listing.AgeLabel, listing.Gender, listing.Size, listing.ActivityLevel, listing.Description,
            listing.LocationCoords.Y, listing.LocationCoords.X, listing.LocationName, listing.Status, listing.CreatedAt, listing.UpdatedAt, []);

        return TypedResults.Created($"/api/v1/listings/{listing.Id}", response);
    }

    private static async Task<Ok<PagedResult<ListingResponse>>> BrowseListingsAsync(
        [AsParameters] PaginationQuery pagination,
        string? species,
        AnimalSize? size,
        Gender? gender,
        ListingStatus? status,
        string? locationName,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var query = db.AnimalListings
            .AsNoTracking()
            .Where(l => l.IsActive);

        if (!string.IsNullOrEmpty(species))
            query = query.Where(l => l.Species.ToLower() == species.ToLower());

        if (size.HasValue)
            query = query.Where(l => l.Size == size.Value);

        if (gender.HasValue)
            query = query.Where(l => l.Gender == gender.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (!string.IsNullOrEmpty(locationName))
            query = query.Where(l => l.LocationName.Contains(locationName));

        var totalCount = await query.CountAsync(ct);

        var listings = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Species,
                l.Breed,
                l.AgeMonths,
                l.AgeLabel,
                l.Gender,
                l.Size,
                l.ActivityLevel,
                l.LocationName,
                l.Status,
                l.CreatedAt,
                CoverPhotoKey = l.Photos.OrderBy(p => p.SortOrder).Select(p => p.StorageKey).FirstOrDefault()
            })
            .ToListAsync(ct);

        var responses = listings.Select(l => new ListingResponse(
            l.Id, l.Name, l.Species, l.Breed, l.AgeMonths, l.AgeLabel, l.Gender, l.Size, l.ActivityLevel, l.LocationName, l.Status,
            l.CoverPhotoKey is not null ? storageService.GetPublicUrl(l.CoverPhotoKey) : null,
            l.CreatedAt)).ToList();

        return TypedResults.Ok(new PagedResult<ListingResponse>(responses, totalCount, pagination.Page, pagination.PageSize));
    }

    private static async Task<Results<Ok<ListingDetailResponse>, NotFound>> GetListingAsync(
        Guid id,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var listing = await db.AnimalListings
            .AsNoTracking()
            .Where(l => l.Id == id && l.IsActive)
            .Select(l => new
            {
                l.Id,
                l.OwnerId,
                OwnerName = l.Owner.Name,
                l.RescueCaseId,
                l.Name,
                l.Species,
                l.Breed,
                l.AgeMonths,
                l.AgeLabel,
                l.Gender,
                l.Size,
                l.ActivityLevel,
                l.Description,
                Latitude = l.LocationCoords.Y,
                Longitude = l.LocationCoords.X,
                l.LocationName,
                l.Status,
                l.CreatedAt,
                l.UpdatedAt,
                Photos = l.Photos
                    .OrderBy(p => p.SortOrder)
                    .Select(p => new { p.Id, p.StorageKey, p.SortOrder, p.CreatedAt })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (listing is null)
            return TypedResults.NotFound();

        var photos = listing.Photos.Select(p =>
            new ListingPhotoResponse(p.Id, storageService.GetPublicUrl(p.StorageKey), p.SortOrder, p.CreatedAt)).ToList();

        var response = new ListingDetailResponse(
            listing.Id, listing.OwnerId, listing.OwnerName, listing.RescueCaseId, listing.Name, listing.Species, listing.Breed,
            listing.AgeMonths, listing.AgeLabel, listing.Gender, listing.Size, listing.ActivityLevel, listing.Description,
            listing.Latitude, listing.Longitude, listing.LocationName, listing.Status, listing.CreatedAt, listing.UpdatedAt, photos);

        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> UpdateListingAsync(
        Guid id,
        UpdateListingRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IOutputCacheStore cacheStore,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var listing = await db.AnimalListings.FirstOrDefaultAsync(l => l.Id == id && l.IsActive, ct);
        if (listing is null)
            return TypedResults.NotFound();

        if (listing.OwnerId != userId)
            return TypedResults.Forbid();

        listing.Name = request.Name;
        listing.Species = request.Species;
        listing.Breed = request.Breed;
        listing.AgeMonths = request.AgeMonths;
        listing.AgeLabel = request.AgeLabel;
        listing.Gender = request.Gender;
        listing.Size = request.Size;
        listing.ActivityLevel = request.ActivityLevel;
        listing.Description = request.Description;
        listing.LocationCoords = new Point(request.Longitude, request.Latitude) { SRID = 4326 };
        listing.LocationName = request.LocationName;

        await db.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync("listings", ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> UpdateStatusAsync(
        Guid id,
        UpdateListingStatusRequest request,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IOutputCacheStore cacheStore,
        IReputationService reputationService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var listing = await db.AnimalListings.FirstOrDefaultAsync(l => l.Id == id && l.IsActive, ct);
        if (listing is null)
            return TypedResults.NotFound();

        if (listing.OwnerId != userId)
            return TypedResults.Forbid();

        var wasAdopted = listing.Status == ListingStatus.Adopted;
        listing.Status = request.Status;
        await db.SaveChangesAsync(ct);

        await cacheStore.EvictByTagAsync("listings", ct);

        if (!wasAdopted && listing.Status == ListingStatus.Adopted)
        {
            await reputationService.AwardPointsAsync(
                userId, "AdoptionCompleted", 15, listing.Id, "AnimalListing", ct);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeleteListingAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IOutputCacheStore cacheStore,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var listing = await db.AnimalListings.FirstOrDefaultAsync(l => l.Id == id && l.IsActive, ct);
        if (listing is null)
            return TypedResults.NotFound();

        if (listing.OwnerId != userId)
            return TypedResults.Forbid();

        listing.IsActive = false;
        await db.SaveChangesAsync(ct);
        await cacheStore.EvictByTagAsync("listings", ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<List<ListingResponse>>, NotFound<string>>> GetMatchesAsync(
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        [FromServices] ListingMatchService matchService,
        IStorageService storageService,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var profile = await db.LifestyleProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is null)
            return TypedResults.NotFound("Lifestyle profile not found. Create one first.");

        var availableListings = await db.AnimalListings
            .AsNoTracking()
            .Include(l => l.Photos.OrderBy(p => p.SortOrder).Take(1))
            .Where(l => l.IsActive && l.Status == ListingStatus.Available)
            .ToListAsync(ct);

        var matchedListings = matchService.GetMatches(profile, availableListings);

        var responses = matchedListings.Select(l => new ListingResponse(
            l.Id, l.Name, l.Species, l.Breed, l.AgeMonths, l.AgeLabel, l.Gender, l.Size, l.ActivityLevel, l.LocationName, l.Status,
            l.Photos.FirstOrDefault() is not null ? storageService.GetPublicUrl(l.Photos.First().StorageKey) : null,
            l.CreatedAt)).ToList();

        return TypedResults.Ok(responses);
    }

    private static async Task<Results<Ok<List<ListingPhotoResponse>>, NotFound>> GetPhotosAsync(
        Guid id,
        HappyPawsDbContext db,
        IStorageService storageService,
        CancellationToken ct)
    {
        var listing = await db.AnimalListings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id && l.IsActive, ct);
        if (listing is null)
            return TypedResults.NotFound();

        var photos = await db.ListingPhotos
            .AsNoTracking()
            .Where(p => p.ListingId == id)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        var response = photos.Select(p => new ListingPhotoResponse(
            p.Id, storageService.GetPublicUrl(p.StorageKey), p.SortOrder, p.CreatedAt)).ToList();

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created<ListingPhotoResponse>, BadRequest<string>, NotFound, ForbidHttpResult>> UploadPhotoAsync(
        Guid id,
        IFormFile photo,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        IOutputCacheStore cacheStore,
        CancellationToken ct)
    {
        if (photo.Length > 10 * 1024 * 1024)
            return TypedResults.BadRequest("Photo must not exceed 10MB");

        var userId = principal.GetUserId();

        var listing = await db.AnimalListings.Include(l => l.Photos).FirstOrDefaultAsync(l => l.Id == id && l.IsActive, ct);
        if (listing is null)
            return TypedResults.NotFound();

        if (listing.OwnerId != userId)
            return TypedResults.Forbid();

        if (listing.Photos.Count >= 10)
            return TypedResults.BadRequest("Maximum 10 photos allowed per listing.");

        var photoId = Guid.NewGuid();
        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        var key = $"listings/{id}/{photoId}{extension}";

        await using var uploadStream = photo.OpenReadStream();
        await storageService.UploadAsync(key, uploadStream, photo.ContentType, cancellationToken: ct);

        var maxSort = listing.Photos.Any() ? listing.Photos.Max(p => p.SortOrder) : 0;

        var listingPhoto = new ListingPhoto
        {
            Id = photoId,
            ListingId = id,
            StorageKey = key,
            SortOrder = maxSort + 1
        };

        db.ListingPhotos.Add(listingPhoto);
        await db.SaveChangesAsync(ct);

        await cacheStore.EvictByTagAsync("listings", ct);

        var photoUrl = storageService.GetPublicUrl(key);
        var response = new ListingPhotoResponse(photoId, photoUrl, listingPhoto.SortOrder, listingPhoto.CreatedAt);

        return TypedResults.Created($"/api/v1/listings/{id}/photos/{photoId}", response);
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeletePhotoAsync(
        Guid id,
        Guid photoId,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        IStorageService storageService,
        IOutputCacheStore cacheStore,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var listing = await db.AnimalListings.Include(l => l.Photos).FirstOrDefaultAsync(l => l.Id == id && l.IsActive, ct);
        if (listing is null)
            return TypedResults.NotFound();

        if (listing.OwnerId != userId)
            return TypedResults.Forbid();

        var photo = listing.Photos.FirstOrDefault(p => p.Id == photoId);
        if (photo is null)
            return TypedResults.NotFound();

        await storageService.DeleteAsync(photo.StorageKey, ct);

        db.ListingPhotos.Remove(photo);
        await db.SaveChangesAsync(ct);

        await cacheStore.EvictByTagAsync("listings", ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<List<ApplicationResponse>>, NotFound, ForbidHttpResult>> GetListingApplicationsAsync(
        Guid id,
        ClaimsPrincipal principal,
        HappyPawsDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var listing = await db.AnimalListings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id && l.IsActive, ct);
        if (listing is null)
            return TypedResults.NotFound();

        if (listing.OwnerId != userId)
            return TypedResults.Forbid();

        var applications = await db.AdoptionApplications
            .AsNoTracking()
            .Include(a => a.Applicant)
            .Where(a => a.ListingId == id)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new ApplicationResponse(
                a.Id, a.ListingId, listing.Name, a.ApplicantId, a.Applicant.Name,
                a.Status, a.ReviewNotes, a.AppliedAt, a.UpdatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(applications);
    }
}
