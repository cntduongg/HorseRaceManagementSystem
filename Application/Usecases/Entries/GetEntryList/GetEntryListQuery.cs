using MediatR;

namespace Application.Usecases.Entries.GetEntryList;

public sealed record GetEntryListQuery()
	: IRequest<List<EntryListItemResponse>>;