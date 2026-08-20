using HappyPaws.Core.Enums;

namespace HappyPaws.Api.Endpoints.Listings;

/// <summary>Data submitted when creating a new animal adoption listing.</summary>
/// <param name="RescueCaseId">Optional rescue case this listing originated from. Only the assigned foster of a resolved case can set this.</param>
/// <param name="Name">The animal's name or a short identifier.</param>
/// <param name="Species">The animal's species (for example, Dog or Cat).</param>
/// <param name="Breed">The animal's breed.</param>
/// <param name="AgeMonths">The animal's age in months.</param>
/// <param name="AgeLabel">Human-readable age description (for example, "3 months old"). Optional.</param>
/// <param name="Gender">The animal's gender.</param>
/// <param name="Size">The animal's size category.</param>
/// <param name="ActivityLevel">The animal's activity level.</param>
/// <param name="Description">A full description of the animal's personality and needs.</param>
/// <param name="Latitude">WGS84 latitude of the listing location.</param>
/// <param name="Longitude">WGS84 longitude of the listing location.</param>
/// <param name="LocationName">Human-readable name of the listing location.</param>
public record CreateListingRequest(
    Guid? RescueCaseId,
    string Title,
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
    List<string> Tags);

/// <summary>Data submitted when updating an existing animal listing.</summary>
/// <param name="Name">The animal's name or a short identifier.</param>
/// <param name="Species">The animal's species.</param>
/// <param name="Breed">The animal's breed.</param>
/// <param name="AgeMonths">The animal's age in months.</param>
/// <param name="AgeLabel">Human-readable age description. Optional.</param>
/// <param name="Gender">The animal's gender.</param>
/// <param name="Size">The animal's size category.</param>
/// <param name="ActivityLevel">The animal's activity level.</param>
/// <param name="Description">A full description of the animal's personality and needs.</param>
/// <param name="Latitude">WGS84 latitude of the listing location.</param>
/// <param name="Longitude">WGS84 longitude of the listing location.</param>
/// <param name="LocationName">Human-readable name of the listing location.</param>
public record UpdateListingRequest(
    string Title,
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
    List<string> Tags);

/// <summary>Data submitted when changing a listing's adoption status.</summary>
/// <param name="Status">The new adoption status.</param>
public record UpdateListingStatusRequest(
    ListingStatus Status);

/// <summary>A summary of an animal listing, used in browse and match results.</summary>
/// <param name="Id">Unique identifier of the listing.</param>
/// <param name="Name">The animal's name.</param>
/// <param name="Species">The animal's species.</param>
/// <param name="Breed">The animal's breed.</param>
/// <param name="AgeMonths">The animal's age in months.</param>
/// <param name="AgeLabel">Human-readable age label. Null if not set.</param>
/// <param name="Gender">The animal's gender.</param>
/// <param name="Size">The animal's size category.</param>
/// <param name="ActivityLevel">The animal's activity level.</param>
/// <param name="LocationName">Human-readable location name.</param>
/// <param name="Status">The current adoption status.</param>
/// <param name="PrimaryPhotoUrl">Public URL of the first photo. Null if no photos have been uploaded.</param>
/// <param name="CreatedAt">UTC timestamp when the listing was created.</param>
public record ListingResponse(
    Guid Id,
    string Title,
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
    List<string> Tags,
    string? PrimaryPhotoUrl,
    DateTimeOffset CreatedAt);

/// <summary>Full details of an animal listing, including all photos.</summary>
/// <param name="Id">Unique identifier of the listing.</param>
/// <param name="OwnerId">ID of the user who created the listing.</param>
/// <param name="OwnerName">Display name of the listing owner.</param>
/// <param name="RescueCaseId">ID of the rescue case this listing originated from. Null if not linked.</param>
/// <param name="Name">The animal's name.</param>
/// <param name="Species">The animal's species.</param>
/// <param name="Breed">The animal's breed.</param>
/// <param name="AgeMonths">The animal's age in months.</param>
/// <param name="AgeLabel">Human-readable age label. Null if not set.</param>
/// <param name="Gender">The animal's gender.</param>
/// <param name="Size">The animal's size category.</param>
/// <param name="ActivityLevel">The animal's activity level.</param>
/// <param name="Description">Full description of the animal's personality and needs.</param>
/// <param name="Latitude">WGS84 latitude of the listing location.</param>
/// <param name="Longitude">WGS84 longitude of the listing location.</param>
/// <param name="LocationName">Human-readable location name.</param>
/// <param name="Status">The current adoption status.</param>
/// <param name="CreatedAt">UTC timestamp when the listing was created.</param>
/// <param name="UpdatedAt">UTC timestamp of the last update.</param>
/// <param name="Photos">All photos attached to the listing, ordered by sort position.</param>
public record ListingDetailResponse(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    Guid? RescueCaseId,
    string Title,
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
    List<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<ListingPhotoResponse> Photos);

/// <summary>A single photo attached to a listing.</summary>
/// <param name="Id">Unique identifier of the photo.</param>
/// <param name="PhotoUrl">Public URL of the photo.</param>
/// <param name="SortOrder">Display order of the photo within the listing.</param>
/// <param name="CreatedAt">UTC timestamp when the photo was uploaded.</param>
public record ListingPhotoResponse(
    Guid Id,
    string PhotoUrl,
    int SortOrder,
    DateTimeOffset CreatedAt);
