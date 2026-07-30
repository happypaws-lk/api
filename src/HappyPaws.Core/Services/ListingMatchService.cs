using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;

namespace HappyPaws.Core.Services;

public class ListingMatchService
{
    public IReadOnlyList<AnimalListing> GetMatches(LifestyleProfile profile, IEnumerable<AnimalListing> listings)
    {
        var scoredListings = new List<(AnimalListing Listing, double Score)>();

        foreach (var listing in listings)
        {
            double score = 0;

            // Exclusion: Large animals in Apartment
            if (listing.Size == AnimalSize.Large && profile.HomeSize == HomeSize.Apartment)
                continue;

            // Exclusion: Children incompatibility (species/breed-based logic)
            if (profile.HasChildren)
            {
                var lowerBreed = listing.Breed.ToLowerInvariant();
                bool isIncompatibleWithChildren = lowerBreed.Contains("pitbull") || lowerBreed.Contains("rottweiler") || lowerBreed.Contains("mastiff");
                if (isIncompatibleWithChildren)
                    continue;
            }

            // Exclusion: ExistingPetTypes compatibility filter
            if (profile.ExistingPetTypes != null && profile.ExistingPetTypes.Any())
            {
                var hasCats = profile.ExistingPetTypes.Contains("Cat", StringComparer.OrdinalIgnoreCase) || profile.ExistingPetTypes.Contains("Cats", StringComparer.OrdinalIgnoreCase);
                var lowerBreed = listing.Breed.ToLowerInvariant();
                if (hasCats && (lowerBreed.Contains("greyhound") || lowerBreed.Contains("husky")))
                {
                    continue; // Exclude due to high prey drive (example logic)
                }
            }

            // Score: ActivityLevel
            if (listing.ActivityLevel == profile.ActivityLevel)
            {
                score += 1.0;
            }
            else if (Math.Abs((int)listing.ActivityLevel - (int)profile.ActivityLevel) == 1)
            {
                score += 0.5;
            }

            // Score: HasYard
            if (profile.HasYard && (listing.Size == AnimalSize.Large || listing.ActivityLevel == ActivityLevel.High))
            {
                score += 1.0;
            }

            scoredListings.Add((listing, score));
        }

        return scoredListings
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Listing.CreatedAt)
            .Select(x => x.Listing)
            .ToList();
    }
}
