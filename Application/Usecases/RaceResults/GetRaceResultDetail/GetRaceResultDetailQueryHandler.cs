using MediatR;

namespace Application.Usecases.RaceResults.GetRaceResultDetail;

public sealed class GetRaceResultDetailQueryHandler
	: IRequestHandler<GetRaceResultDetailQuery, RaceResultDetailResponse?>
{
	public Task<RaceResultDetailResponse?> Handle(
		GetRaceResultDetailQuery request,
		CancellationToken cancellationToken)
	{
		// TODO: Load from database

		var response = new RaceResultDetailResponse(
			request.RaceId,
			request.EntryId,
			30,
			1,
			false,
			2,
			3,
			DateTime.UtcNow
		);

		return Task.FromResult<RaceResultDetailResponse?>(response);
	}
}