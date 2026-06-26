using MediatR;

namespace Application.Usecases.Entries.GetEntryList;

public sealed record GetEntryListQuery(
	int? RaceId = null,
	int? OwnerId = null)
	: IRequest<List<EntryListItemResponse>>;