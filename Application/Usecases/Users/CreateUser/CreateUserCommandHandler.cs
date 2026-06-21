using MediatR;

namespace Application.Usecases.Users.CreateUser;

public sealed class CreateUserCommandHandler
	: IRequestHandler<CreateUserCommand, int>
{
	public Task<int> Handle(
		CreateUserCommand request,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Email))
			throw new InvalidOperationException("Email is required.");

		if (string.IsNullOrWhiteSpace(request.PasswordHash))
			throw new InvalidOperationException("PasswordHash is required.");

		if (string.IsNullOrWhiteSpace(request.FullName))
			throw new InvalidOperationException("FullName is required.");

		if (request.RoleId <= 0)
			throw new InvalidOperationException("RoleId is invalid.");

		// TODO: Save into database

		var userId = 1;
		return Task.FromResult(userId);
	}
}