using MediatR;

namespace Application.Usecases.JockeyProfiles.GetJockeyProfileDetail;

public sealed class GetJockeyProfileDetailQueryHandler
	: IRequestHandler<
		GetJockeyProfileDetailQuery,
		JockeyProfileDetailResponse?>
{
	public Task<JockeyProfileDetailResponse?> Handle(
		GetJockeyProfileDetailQuery request,
		CancellationToken cancellationToken)
	{
		// TODO: Load from database

		var response = new JockeyProfileDetailResponse(
			request.UserId,
			"JOCKEY-001",
			54.5m,
			"Professional jockey",
			100,
			25,
			40,
			500
		);

		return Task.FromResult<JockeyProfileDetailResponse?>(response);
	}
}