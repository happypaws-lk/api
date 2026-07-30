using FluentValidation;

namespace HappyPaws.Api.Endpoints.Applications;

public class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(x => x.ListingId).NotEmpty();
    }
}
