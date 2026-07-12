using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using ShareKernel.UnitOfWork;

namespace Application.Usecases.Auth.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private static readonly Dictionary<string, int> AllowedRoles = new()
    {
        ["SPECTATOR"]   = 4,
        ["HORSE_OWNER"] = 1,
        ["JOCKEY"]      = 2,
    };

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IApplicationDbContext context)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var roleCode = request.RoleCode?.ToUpperInvariant() ?? "SPECTATOR";

        if (!AllowedRoles.TryGetValue(roleCode, out var roleId))
        {
            throw new InvalidOperationException($"Role '{request.RoleCode}' is not allowed for self-registration.");
        }

        if (roleCode == "JOCKEY")
        {
            if (string.IsNullOrWhiteSpace(request.LicenseNumber))
                throw new InvalidOperationException("LicenseNumber is required for Jockey registration.");

            if (request.Weight is null or <= 0)
                throw new InvalidOperationException("Weight must be a positive number for Jockey registration.");
        }

        var emailExists = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailExists)
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var requiresApproval = roleCode is "HORSE_OWNER" or "JOCKEY";

        var user = new User
        {
            Email             = request.Email.ToLowerInvariant(),
            PasswordHash      = _passwordHasher.Hash(request.Password),
            FullName          = request.FullName,
            PhoneNumber       = request.PhoneNumber,
            RoleId            = roleId,
            IsActive          = !requiresApproval,
            Status            = requiresApproval ? "Pending" : "Active",
            IsProfileComplete = false,
        };

        if (roleCode == "JOCKEY")
        {
            user.LicenseNumber = request.LicenseNumber!.Trim();
            user.Weight        = request.Weight;
            user.Bio           = request.Bio?.Trim();
        }

        await _userRepository.AddAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (roleCode == "SPECTATOR")
        {
            var now = DateTime.UtcNow;

            _context.Spectators.Add(new Spectator
            {
                UserId = user.UserId
            });

            // Flow 7 — ví điểm khởi tạo với 100 điểm.
            var wallet = new PointWallet
            {
                SpectatorId = user.UserId,
                Balance = 100,
                IsFrozen = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            // Ghi giao dịch khởi tạo để lịch sử ví khớp số dư (BalanceAfter = 100).
            wallet.Transactions.Add(new WalletTransaction
            {
                SpectatorId = user.UserId,
                Type = "Initial",
                Amount = 100m,
                BalanceAfter = 100m,
                Reason = "Điểm khởi tạo tài khoản (+100)",
                CreatedAt = now
            });

            _context.PointWallets.Add(wallet);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new RegisterResponse(
            UserId:          user.UserId,
            Email:           user.Email,
            FullName:        user.FullName,
            Status:          user.Status,
            RequiresApproval: requiresApproval
        );
    }
}