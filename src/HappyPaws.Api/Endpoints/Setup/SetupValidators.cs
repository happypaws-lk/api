using FluentValidation;

namespace HappyPaws.Api.Endpoints.Setup;

public class SetupCompleteRequestValidator : AbstractValidator<SetupCompleteRequest>
{
    public SetupCompleteRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}
