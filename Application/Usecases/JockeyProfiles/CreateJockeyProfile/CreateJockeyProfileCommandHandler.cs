using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyProfiles.CreateJockeyProfile;

public sealed class CreateJockeyProfileCommandHandler
    : IRequestHandler<CreateJockeyProfileCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateJockeyProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateJockeyProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
            throw new InvalidOperationException("UserId is required.");

        var userExists = await _context.Users
            .AnyAsync(x => x.UserId == request.UserId, cancellationToken);

        if (!userExists)
            throw new InvalidOperationException("User does not exist.");

        var exists = await _context.JockeyProfiles
            .AnyAsync(x => x.UserId == request.UserId, cancellationToken);

        if (exists)
            throw new InvalidOperationException("JockeyProfile already exists.");

        if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            var licenseExists = await _context.JockeyProfiles
                .AnyAsync(x => x.LicenseNumber == request.LicenseNumber, cancellationToken);

            if (licenseExists)
                throw new InvalidOperationException("LicenseNumber already exists.");
        }

        var profile = new JockeyProfile
        {
            UserId = request.UserId,
            LicenseNumber = request.LicenseNumber,
            Weight = request.Weight,
            Bio = request.Bio,
            TotalRaces = 0,
            TotalWins = 0,
            TotalTop3 = 0,
            CareerPrizePoints = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.JockeyProfiles.Add(profile);
        await _context.SaveChangesAsync(cancellationToken);

        return profile.UserId;
    }
}