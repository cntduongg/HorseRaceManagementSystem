using Application.Common;
using MediatR;

namespace Application.Usecases.Admin.ApproveUser;

public sealed class ApproveUserCommandHandler : IRequestHandler<ApproveUserCommand, ApproveUserResponse>
{
    private readonly IUserRepository _userRepository;

    public ApproveUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ApproveUserResponse> Handle(ApproveUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            throw new KeyNotFoundException($"User with ID {request.UserId} was not found.");

        if (user.Status != "Pending")
            throw new InvalidOperationException($"User {request.UserId} is not in Pending status (current: {user.Status}).");

        user.Status   = "Active";
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        return new ApproveUserResponse(user.UserId, user.Email, user.FullName, user.Status);
    }
}
