using MediatR;

namespace Application.Usecases.Races.OpenRegistration;

public sealed record OpenRaceRegistrationCommand(
    int RaceId
) : IRequest<bool>;