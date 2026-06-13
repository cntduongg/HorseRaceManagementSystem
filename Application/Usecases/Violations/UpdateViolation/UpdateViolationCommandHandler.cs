using MediatR;

namespace Application.Usecases.Violations.UpdateViolation;

public sealed class UpdateViolationCommandHandler
	: IRequestHandler<UpdateViolationCommand, bool>
{
	public Task<bool> Handle(
		UpdateViolationCommand request,
		CancellationToken cancellationToken)
	{
		// TODO: Update database

		return Task.FromResult(true);
	}
}