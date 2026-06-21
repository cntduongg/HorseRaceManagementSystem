using MediatR;

namespace Application.Usecases.Legs.GetLegDetail;

public sealed class GetLegDetailQueryHandler
    : IRequestHandler<GetLegDetailQuery, LegDetailResponse?>
{
    public Task<LegDetailResponse?> Handle(
        GetLegDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var response = new LegDetailResponse(
            request.RaceId,
            request.LegNumber,
            "Pending",
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            null
        );

        return Task.FromResult<LegDetailResponse?>(response);
    }
}