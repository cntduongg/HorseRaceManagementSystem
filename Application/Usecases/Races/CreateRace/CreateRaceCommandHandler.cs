using MediatR;

namespace Application.Usecases.Races.CreateRace;

public sealed class CreateRaceCommandHandler
    : IRequestHandler<CreateRaceCommand, int>
{
    public Task<int> Handle(
        CreateRaceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.NumberOfLegs < 1 || request.NumberOfLegs > 10)
        {
            throw new InvalidOperationException(
                "NumberOfLegs must be between 1 and 10.");
        }

        if (request.Referee1Id <= 0)
        {
            throw new InvalidOperationException(
                "Referee1Id is required.");
        }

        if (request.Referee2Id <= 0)
        {
            throw new InvalidOperationException(
                "Referee2Id is required.");
        }

        if (request.Referee1Id == request.Referee2Id)
        {
            throw new InvalidOperationException(
                "Referee1Id and Referee2Id must be different.");
        }

        // TODO: Save race into database

        var raceId = 1;

        return Task.FromResult(raceId);
    }
}