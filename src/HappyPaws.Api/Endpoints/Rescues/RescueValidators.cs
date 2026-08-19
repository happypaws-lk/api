using FluentValidation;

namespace HappyPaws.Api.Endpoints.Rescues;

/// <summary>
/// Validates the coordinates, location name, description, and optional condition notes for a new rescue case.
/// </summary>
public class CreateRescueRequestValidator : AbstractValidator<CreateRescueRequest>
{
    public CreateRescueRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.LocationName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.ConditionNotes).MaximumLength(2000).When(x => x.ConditionNotes is not null);
    }
}

/// <summary>
/// Validates the update type and update text for a case progress post.
/// </summary>
public class PostCaseUpdateRequestValidator : AbstractValidator<PostCaseUpdateRequest>
{
    public PostCaseUpdateRequestValidator()
    {
        RuleFor(x => x.UpdateType).IsInEnum();
        RuleFor(x => x.UpdateText).NotEmpty().MaximumLength(5000);
    }
}

/// <summary>
/// Validates that the urgency override value is a known enum member.
/// </summary>
public class OverrideUrgencyRequestValidator : AbstractValidator<OverrideUrgencyRequest>
{
    public OverrideUrgencyRequestValidator()
    {
        RuleFor(x => x.Urgency).IsInEnum();
    }
}
