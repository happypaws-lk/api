using FluentAssertions;
using HappyPaws.Core.Entities;
using HappyPaws.Core.Enums;
using HappyPaws.Core.Services;

namespace HappyPaws.Tests.Unit;

public class ListingMatchServiceTests
{
    private readonly ListingMatchService _sut = new();

    [Fact]
    public void GetMatches_ExactActivityLevel_Scores1Point()
    {
        var profile = new LifestyleProfile { ActivityLevel = ActivityLevel.High };
        var listings = new[]
        {
            new AnimalListing { Id = Guid.NewGuid(), ActivityLevel = ActivityLevel.High, Breed = "Dog" }
        };

        var matches = _sut.GetMatches(profile, listings);

        matches.Should().ContainSingle();
        matches.First().Id.Should().Be(listings[0].Id);
    }

    [Fact]
    public void GetMatches_AdjacentActivityLevel_ScoresHalfPoint()
    {
        var profile = new LifestyleProfile { ActivityLevel = ActivityLevel.Moderate };
        var listings = new[]
        {
            new AnimalListing { Id = Guid.NewGuid(), ActivityLevel = ActivityLevel.High, Breed = "Dog" },
            new AnimalListing { Id = Guid.NewGuid(), ActivityLevel = ActivityLevel.Low, Breed = "Dog" }
        };

        var matches = _sut.GetMatches(profile, listings);

        matches.Should().HaveCount(2);
    }

    [Fact]
    public void GetMatches_LargeAnimalInApartment_IsExcluded()
    {
        var profile = new LifestyleProfile { HomeSize = HomeSize.Apartment };
        var listings = new[]
        {
            new AnimalListing { Id = Guid.NewGuid(), Size = AnimalSize.Large, Breed = "Dog" }
        };

        var matches = _sut.GetMatches(profile, listings);

        matches.Should().BeEmpty();
    }

    [Fact]
    public void GetMatches_HasYard_WithLargeOrHighActivity_Scores1Point()
    {
        var profile = new LifestyleProfile { HasYard = true, HomeSize = HomeSize.House };
        var listings = new[]
        {
            new AnimalListing { Id = Guid.NewGuid(), Size = AnimalSize.Large, ActivityLevel = ActivityLevel.Low, Breed = "Dog" },
            new AnimalListing { Id = Guid.NewGuid(), Size = AnimalSize.Small, ActivityLevel = ActivityLevel.High, Breed = "Dog" }
        };

        var matches = _sut.GetMatches(profile, listings);

        matches.Should().HaveCount(2);
    }

    [Fact]
    public void GetMatches_HasChildren_ExcludesIncompatibleBreeds()
    {
        var profile = new LifestyleProfile { HasChildren = true };
        var listings = new[]
        {
            new AnimalListing { Id = Guid.NewGuid(), Breed = "Pitbull mix" },
            new AnimalListing { Id = Guid.NewGuid(), Breed = "Golden Retriever" }
        };

        var matches = _sut.GetMatches(profile, listings);

        matches.Should().ContainSingle();
        matches.First().Breed.Should().Be("Golden Retriever");
    }

    [Fact]
    public void GetMatches_ExistingPetTypes_ExcludesIncompatibleBreeds()
    {
        var profile = new LifestyleProfile { ExistingPetTypes = ["Cat"] };
        var listings = new[]
        {
            new AnimalListing { Id = Guid.NewGuid(), Breed = "Greyhound" },
            new AnimalListing { Id = Guid.NewGuid(), Breed = "Pug" }
        };

        var matches = _sut.GetMatches(profile, listings);

        matches.Should().ContainSingle();
        matches.First().Breed.Should().Be("Pug");
    }
}
