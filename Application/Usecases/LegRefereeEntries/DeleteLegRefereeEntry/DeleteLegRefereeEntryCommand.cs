using MediatR;

namespace Application.Usecases.LegRefereeEntries.DeleteLegRefereeEntry;

public sealed record DeleteLegRefereeEntryCommand(
	long LegRefereeEntryId
) : IRequest<bool>;