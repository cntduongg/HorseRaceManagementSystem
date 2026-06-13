using MediatR;

namespace Application.Usecases.Users.UpdateUser;

public sealed class UpdateUserCommandHandler
	: IRequestHandler<UpdateUserCommand, bool>
{
	public Task<bool> Handle(
		UpdateUserCommand request,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Email))
			throw new InvalidOperationException("Email is required.");

		if (string.IsNullOrWhiteSpace(request.FullName))
			throw new InvalidOperationException("FullName is required.");

		if (request.RoleId <= 0)
			throw new InvalidOperationException("RoleId is invalid.");

		// TODO: Update database

		return Task.FromResult(true);
	}
}