using FluentValidation;

namespace HappyPaws.Api.Endpoints.Listings;

public class CreateListingRequestValidator : AbstractValidator<CreateListingRequest>
{
    public CreateListingRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Species).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Breed).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AgeMonths).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AgeLabel).MaximumLength(50);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Size).IsInEnum();
        RuleFor(x => x.ActivityLevel).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.LocationName).NotEmpty().MaximumLength(500);
    }
}

public class UpdateListingRequestValidator : AbstractValidator<UpdateListingRequest>
{
    public UpdateListingRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Species).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Breed).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AgeMonths).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AgeLabel).MaximumLength(50);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Size).IsInEnum();
        RuleFor(x => x.ActivityLevel).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.LocationName).NotEmpty().MaximumLength(500);
    }
}

public class UpdateListingStatusRequestValidator : AbstractValidator<UpdateListingStatusRequest>
{
    public UpdateListingStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
