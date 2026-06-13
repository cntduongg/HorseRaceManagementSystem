using MediatR;

namespace Application.Usecases.RaceResults.DeleteRaceResult;

public sealed class DeleteRaceResultCommandHandler
	: IRequestHandler<DeleteRaceResultCommand, bool>
{
	public Task<bool> Handle(
		DeleteRaceResultCommand request,
		CancellationToken cancellationToken)
	{
		// TODO: Delete race result from database

		return Task.FromResult(true);
	}
}