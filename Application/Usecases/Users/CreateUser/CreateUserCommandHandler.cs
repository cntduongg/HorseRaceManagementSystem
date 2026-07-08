using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Application.Usecases.Users.CreateUser;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<int> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Password is required.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("FullName is required.");

        if (request.RoleId <= 0)
            throw new InvalidOperationException("RoleId is invalid.");
        var exists = await _context.Users
    .AnyAsync(x => x.Email == request.Email, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            var licenseExists = await _context.Users
                .AnyAsync(x => x.LicenseNumber == request.LicenseNumber, cancellationToken);

            if (licenseExists)
                throw new InvalidOperationException("LicenseNumber already exists.");
        }
        if (exists)
            throw new InvalidOperationException("Email already exists.");
        var user = new User
        {
            Email = request.Email.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber,
            AvatarUrl = request.AvatarUrl,
            RoleId = request.RoleId,
            LicenseNumber = request.LicenseNumber,
            Weight = request.Weight,
            Bio = request.Bio,
            IsActive = true,
            IsProfileComplete = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync(cancellationToken);

        return user.UserId;
    }
}