using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyProfiles.UpdateJockeyProfile;

public sealed class UpdateJockeyProfileCommandHandler
    : IRequestHandler<UpdateJockeyProfileCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateJockeyProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateJockeyProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _context.JockeyProfiles
            .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

        if (profile is null)
            return false;

        if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            var duplicate = await _context.JockeyProfiles
                .AnyAsync(x =>
                    x.LicenseNumber == request.LicenseNumber
                    && x.UserId != request.UserId,
                    cancellationToken);

            if (duplicate)
                throw new InvalidOperationException("LicenseNumber already exists.");
        }

        profile.LicenseNumber = request.LicenseNumber;
        profile.Weight = request.Weight;
        profile.Bio = request.Bio;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}