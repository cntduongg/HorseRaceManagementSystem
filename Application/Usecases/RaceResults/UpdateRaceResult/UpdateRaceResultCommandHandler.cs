using MediatR;

namespace Application.Usecases.RaceResults.UpdateRaceResult;

public sealed class UpdateRaceResultCommandHandler
    : IRequestHandler<UpdateRaceResultCommand, bool>
{
    public Task<bool> Handle(
        UpdateRaceResultCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update race result in database

        return Task.FromResult(true);
    }
}