using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.LegOfficialResults.DeleteLegOfficialResult;

public sealed class DeleteLegOfficialResultCommandHandler
    : IRequestHandler<DeleteLegOfficialResultCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteLegOfficialResultCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteLegOfficialResultCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _context.LegOfficialResults.FirstOrDefaultAsync(x =>
            x.RaceId == request.RaceId &&
            x.LegNumber == request.LegNumber &&
            x.EntryId == request.EntryId,
            cancellationToken);

        if (entity is null)
            return false;

        _context.LegOfficialResults.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}