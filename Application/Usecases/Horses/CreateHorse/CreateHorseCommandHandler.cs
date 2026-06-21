using MediatR;

namespace Application.Usecases.Horses.CreateHorse;

public sealed class CreateHorseCommandHandler
    : IRequestHandler<CreateHorseCommand, int>
{
    public Task<int> Handle(
        CreateHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Horse name is required.");
        }

        if (request.OwnerId <= 0)
        {
            throw new InvalidOperationException("OwnerId is invalid.");
        }

        // Tạm thời chưa lưu DB.
        // Sau này sẽ inject IRepository hoặc ApplicationDbContext.

        var horseId = Random.Shared.Next(1, int.MaxValue);

        return Task.FromResult(horseId);
    }
}