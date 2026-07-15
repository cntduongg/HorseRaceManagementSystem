using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Horses.DeleteHorse;

public sealed class DeleteHorseCommandHandler
    : IRequestHandler<DeleteHorseCommand, DeleteHorseResult>
{
    private readonly IApplicationDbContext _context;

    public DeleteHorseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeleteHorseResult> Handle(
        DeleteHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HorseId <= 0)
            return new DeleteHorseResult(false, DeleteHorseError.NotFound);

        var horse = await _context.Horses
            .FirstOrDefaultAsync(x => x.HorseId == request.HorseId, cancellationToken);

        if (horse is null)
            return new DeleteHorseResult(false, DeleteHorseError.NotFound);

        if (horse.OwnerId != request.OwnerId)
            return new DeleteHorseResult(false, DeleteHorseError.Forbidden);

        _context.Horses.Remove(horse);
        await _context.SaveChangesAsync(cancellationToken);

        return new DeleteHorseResult(true, DeleteHorseError.None);
    }
}