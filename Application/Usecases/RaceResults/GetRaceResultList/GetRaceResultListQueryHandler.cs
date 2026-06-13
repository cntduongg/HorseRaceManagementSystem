using MediatR;

namespace Application.Usecases.RaceResults.GetRaceResultList;

public sealed class GetRaceResultListQueryHandler
	: IRequestHandler<GetRaceResultListQuery,
		List<RaceResultListItemResponse>>
{
	public Task<List<RaceResultListItemResponse>> Handle(
		GetRaceResultListQuery request,
		CancellationToken cancellationToken)
	{
		// TODO: Load from database

		var result = new List<RaceResultListItemResponse>
		{
			new(1, 1, 1, 30),
			new(1, 2, 2, 24)
		};

		return Task.FromResult(result);
	}
}