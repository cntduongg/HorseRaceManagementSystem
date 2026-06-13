using MediatR;

namespace Application.Usecases.Horses.DeleteHorse;

public sealed class DeleteHorseCommandHandler
    : IRequestHandler<DeleteHorseCommand, bool>
{
    public Task<bool> Handle(
        DeleteHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HorseId <= 0)
        {
            return Task.FromResult(false);
        }

        // TODO: delete database

        return Task.FromResult(true);
    }
}