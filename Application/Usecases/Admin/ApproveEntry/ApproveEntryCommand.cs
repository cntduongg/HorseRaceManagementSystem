using MediatR;

namespace Application.Usecases.Admin.ApproveEntry;

public sealed record ApproveEntryCommand(
	int EntryId,
	int AdminId
) : IRequest<ApproveEntryResponse>;