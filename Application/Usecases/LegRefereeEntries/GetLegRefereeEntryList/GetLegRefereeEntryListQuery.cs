using MediatR;

namespace Application.Usecases.LegRefereeEntries.GetLegRefereeEntryList;

public sealed record GetLegRefereeEntryListQuery()
    : IRequest<List<LegRefereeEntryListItemResponse>>;