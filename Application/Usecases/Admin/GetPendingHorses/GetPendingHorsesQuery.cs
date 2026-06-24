using Application.Common;
using Domain.Aggregates.Entities;

namespace Application.Usecases.Admin.GetPendingHorses;

public sealed record GetPendingHorsesQuery()
    : IQuery<List<GetPendingHorsesResponse>>;