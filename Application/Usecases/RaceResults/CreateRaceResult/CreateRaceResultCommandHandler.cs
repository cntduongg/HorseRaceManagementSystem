using MediatR;

namespace Application.Usecases.RaceResults.CreateRaceResult;

public sealed class CreateRaceResultCommandHandler
    : IRequestHandler<CreateRaceResultCommand, bool>
{
    public Task<bool> Handle(
        CreateRaceResultCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Save race result into database

        return Task.FromResult(true);
    }
}