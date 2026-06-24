using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Horses.DeleteHorse;

public sealed class DeleteHorseCommandHandler
    : IRequestHandler<DeleteHorseCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteHorseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HorseId <= 0)
            return false;

        var horse = await _context.Horses
            .FirstOrDefaultAsync(x => x.HorseId == request.HorseId, cancellationToken);

        if (horse is null)
            return false;

        _context.Horses.Remove(horse);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}