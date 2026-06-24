using Application.Common;
using MediatR;

namespace Application.Usecases.Horses.DeleteHorse;

public sealed class DeleteHorseCommandHandler
    : IRequestHandler<DeleteHorseCommand, bool>
{
    private readonly IHorseRepository _horseRepository;

    public DeleteHorseCommandHandler(IHorseRepository horseRepository)
    {
        _horseRepository = horseRepository;
    }

    public async Task<bool> Handle(
        DeleteHorseCommand request,
        CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken);
        if (horse is null)
        {
            return false;
        }

        if (horse.OwnerId != request.RequesterId)
        {
            throw new UnauthorizedAccessException("You can only delete your own horses.");
        }

        _horseRepository.Remove(horse);
        return true;
    }
}
