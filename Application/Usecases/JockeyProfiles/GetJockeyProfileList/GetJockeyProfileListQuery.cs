using MediatR;

namespace Application.Usecases.JockeyProfiles.GetJockeyProfileList;

public sealed record GetJockeyProfileListQuery()
    : IRequest<List<JockeyProfileListItemResponse>>;