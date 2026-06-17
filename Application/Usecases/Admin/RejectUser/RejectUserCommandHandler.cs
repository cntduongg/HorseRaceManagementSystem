using Application.Common;
using MediatR;

namespace Application.Usecases.Admin.RejectUser;

public sealed class RejectUserCommandHandler : IRequestHandler<RejectUserCommand, RejectUserResponse>
{
    private readonly IUserRepository _userRepository;

    public RejectUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<RejectUserResponse> Handle(RejectUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            throw new KeyNotFoundException($"User with ID {request.UserId} was not found.");

        if (user.Status != "Pending")
            throw new InvalidOperationException($"User {request.UserId} is not in Pending status (current: {user.Status}).");

        user.Status    = "Rejected";
        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;

        return new RejectUserResponse(user.UserId, user.Email, user.FullName, user.Status);
    }
}
