using FluentValidation;

namespace HappyPaws.Api.Endpoints.Transports;

public class CreateTransportRequestValidator : AbstractValidator<CreateTransportRequest>
{
    public CreateTransportRequestValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.PickupLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.PickupLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.PickupLocation).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DropoffLongitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.DropoffLatitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.DropoffLocation).NotEmpty().MaximumLength(200);
    }
}

public class TransportStatusUpdateRequestValidator : AbstractValidator<TransportStatusUpdateRequest>
{
    public TransportStatusUpdateRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
