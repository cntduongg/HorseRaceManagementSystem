using MediatR;

namespace Application.Usecases.Violations.GetViolationDetail;

public sealed class GetViolationDetailQueryHandler
    : IRequestHandler<
        GetViolationDetailQuery,
        ViolationDetailResponse?>
{
    public Task<ViolationDetailResponse?> Handle(
        GetViolationDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var response = new ViolationDetailResponse(
            request.ViolationId,
            1,
            1,
            1,
            2,
            "LaneViolation",
            "Demo violation",
            "Warning",
            "Pending",
            null,
            null,
            null,
            DateTime.UtcNow
        );

        return Task.FromResult<ViolationDetailResponse?>(response);
    }
}