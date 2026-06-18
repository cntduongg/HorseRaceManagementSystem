using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceResults.DeleteRaceResult;

public sealed class DeleteRaceResultCommandHandler
	: IRequestHandler<DeleteRaceResultCommand, bool>
{
	private readonly IApplicationDbContext _context;

	public DeleteRaceResultCommandHandler(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task<bool> Handle(DeleteRaceResultCommand request, CancellationToken cancellationToken)
	{
		var entity = await _context.RaceResults
			.FirstOrDefaultAsync(x =>
				x.RaceId == request.RaceId &&
				x.EntryId == request.EntryId,
				cancellationToken);

		if (entity is null)
			return false;

		_context.RaceResults.Remove(entity);
		await _context.SaveChangesAsync(cancellationToken);

		return true;
	}
}