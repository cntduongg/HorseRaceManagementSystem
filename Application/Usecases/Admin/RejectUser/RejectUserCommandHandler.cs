using Application.Common;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;

namespace Application.Usecases.Admin.RejectUser;

public sealed class RejectUserCommandHandler : IRequestHandler<RejectUserCommand, RejectUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public RejectUserCommandHandler(
        IUserRepository userRepository,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _userRepository = userRepository;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<RejectUserResponse> Handle(
        RejectUserCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Reason is required.");

        var user = await _userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
            throw new KeyNotFoundException(
                $"User with ID {request.UserId} was not found.");

        // Cho phép reject lại kể cả không còn Pending
        user.Status = "Rejected";
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _reviewHistoryRepository.AddAsync(
            new ReviewHistory
            {
                EntityType = ReviewEntity.User,
                EntityId = user.UserId,
                Action = ReviewAction.Rejected,
                Reason = request.Reason,
                AdminId = request.AdminId
            },
            cancellationToken);

        return new RejectUserResponse(
            user.UserId,
            user.Email,
            user.FullName,
            user.Status);
    }
}