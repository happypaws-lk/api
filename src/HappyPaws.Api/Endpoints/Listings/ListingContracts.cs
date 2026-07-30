using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Listings;

public record CreateListingRequest(
    Guid? RescueCaseId,
    string Name,
    string Species,
    string Breed,
    int AgeMonths,
    string? AgeLabel,
    Gender Gender,
    AnimalSize Size,
    ActivityLevel ActivityLevel,
    string Description,
    double Latitude,
    double Longitude,
    string LocationName);

public record UpdateListingRequest(
    string Name,
    string Species,
    string Breed,
    int AgeMonths,
    string? AgeLabel,
    Gender Gender,
    AnimalSize Size,
    ActivityLevel ActivityLevel,
    string Description,
    double Latitude,
    double Longitude,
    string LocationName);

public record UpdateListingStatusRequest(
    ListingStatus Status);

public record ListingResponse(
    Guid Id,
    string Name,
    string Species,
    string Breed,
    int AgeMonths,
    string? AgeLabel,
    Gender Gender,
    AnimalSize Size,
    ActivityLevel ActivityLevel,
    string LocationName,
    ListingStatus Status,
    string? PrimaryPhotoUrl,
    DateTimeOffset CreatedAt);

public record ListingDetailResponse(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    Guid? RescueCaseId,
    string Name,
    string Species,
    string Breed,
    int AgeMonths,
    string? AgeLabel,
    Gender Gender,
    AnimalSize Size,
    ActivityLevel ActivityLevel,
    string Description,
    double Latitude,
    double Longitude,
    string LocationName,
    ListingStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<ListingPhotoResponse> Photos);

public record ListingPhotoResponse(
    Guid Id,
    string PhotoUrl,
    int SortOrder,
    DateTimeOffset CreatedAt);
