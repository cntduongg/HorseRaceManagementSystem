using MediatR;

namespace Application.Usecases.Races.CloseRegistration;

public sealed record CloseRaceRegistrationCommand(
    int RaceId
) : IRequest<bool>;