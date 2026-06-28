using MediatR;

namespace Application.Usecases.Admin.ResultPublication;

public sealed record ReviewRacePublicationQuery(int RaceId)
    : IRequest<ReviewRacePublicationResponse>;