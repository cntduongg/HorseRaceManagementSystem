using MediatR;

namespace Application.Usecases.Horses.UpdateHorse;

public sealed class UpdateHorseCommandHandler
    : IRequestHandler<UpdateHorseCommand, bool>
{
    public Task<bool> Handle(
        UpdateHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HorseId <= 0)
        {
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Horse name is required.");
        }

        // TODO: update database

        return Task.FromResult(true);
    }
}