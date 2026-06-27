using Application.Common;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;

namespace Application.Usecases.Admin.ApproveUser;

public sealed class ApproveUserCommandHandler : IRequestHandler<ApproveUserCommand, ApproveUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public ApproveUserCommandHandler(
        IUserRepository userRepository,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _userRepository = userRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<ApproveUserResponse> Handle(
        ApproveUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
            throw new KeyNotFoundException(
                $"User with ID {request.UserId} was not found.");

        // Cho phép approve lại kể cả không còn Pending
        user.Status = "Active";
        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await _reviewHistoryRepository.AddAsync(
            new ReviewHistory
            {
                EntityType = ReviewEntity.User,
                EntityId = user.UserId,
                Action = ReviewAction.Approved,
                Reason = request.Reason,
                AdminId = request.AdminId
            },
            cancellationToken);

        return new ApproveUserResponse(
            user.UserId,
            user.Email,
            user.FullName,
            user.Status);
    }
}