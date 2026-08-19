using FluentValidation;

namespace HappyPaws.Api.Endpoints.Applications;

/// <summary>
/// Validates that the target listing ID is present when submitting an adoption application.
/// </summary>
public class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty();
    }
}
