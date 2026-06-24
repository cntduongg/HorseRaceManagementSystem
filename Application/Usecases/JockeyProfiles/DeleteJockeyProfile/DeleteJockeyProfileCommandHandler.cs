using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyProfiles.DeleteJockeyProfile;

public sealed class DeleteJockeyProfileCommandHandler
    : IRequestHandler<DeleteJockeyProfileCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteJockeyProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteJockeyProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _context.JockeyProfiles
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (profile is null)
            return false;

        _context.JockeyProfiles.Remove(profile);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}