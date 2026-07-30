using FluentValidation;

namespace HappyPaws.Api.Endpoints.Pledges;

public class CreatePledgeRequestValidator : AbstractValidator<CreatePledgeRequest>
{
    public CreatePledgeRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Note).MaximumLength(500);

        RuleFor(x => x)
            .Must(x => (x.CaseId.HasValue && !x.ListingId.HasValue) || (!x.CaseId.HasValue && x.ListingId.HasValue))
            .WithMessage("A pledge must be associated with either a CaseId or a ListingId, but not both.");
    }
}
