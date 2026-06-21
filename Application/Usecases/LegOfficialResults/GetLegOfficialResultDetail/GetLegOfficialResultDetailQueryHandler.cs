using MediatR;

namespace Application.Usecases.LegOfficialResults.GetLegOfficialResultDetail;

public sealed class GetLegOfficialResultDetailQueryHandler
	: IRequestHandler<
		GetLegOfficialResultDetailQuery,
		LegOfficialResultDetailResponse?>
{
	public Task<LegOfficialResultDetailResponse?> Handle(
		GetLegOfficialResultDetailQuery request,
		CancellationToken cancellationToken)
	{
		// TODO: Load from database

		var response = new LegOfficialResultDetailResponse(
			request.RaceId,
			request.LegNumber,
			request.EntryId,
			1,
			"Finished",
			10,
			"AutoMatched",
			DateTime.UtcNow,
			1,
			null
		);

		return Task.FromResult<LegOfficialResultDetailResponse?>(response);
	}
}