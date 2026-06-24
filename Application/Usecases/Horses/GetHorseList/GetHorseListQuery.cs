using MediatR;

namespace Application.Usecases.Horses.GetHorseList;

/// <summary>
/// Lists horses. Horse Owners pass their own <paramref name="OwnerId"/> to see only
/// their horses; admins pass null to see all and may filter by <paramref name="Status"/>.
/// </summary>
public sealed record GetHorseListQuery(
    int? OwnerId = null,
    string? Status = null
) : IRequest<List<HorseListItemResponse>>;
