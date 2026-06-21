using MediatR;

namespace Application.Usecases.JockeyProfiles.GetJockeyProfileDetail;

public sealed record GetJockeyProfileDetailQuery(
	int UserId
) : IRequest<JockeyProfileDetailResponse?>;