using Application.Common;
using MediatR;

namespace Application.Usecases.Horses.GetHorseList;

public sealed class GetHorseListQueryHandler
    : IRequestHandler<GetHorseListQuery, List<HorseListItemResponse>>
{
    private readonly IHorseRepository _horseRepository;

    public GetHorseListQueryHandler(IHorseRepository horseRepository)
    {
        _horseRepository = horseRepository;
    }

    public async Task<List<HorseListItemResponse>> Handle(
        GetHorseListQuery request,
        CancellationToken cancellationToken)
    {
        var horses = await _horseRepository.GetAsync(
            request.OwnerId,
            request.Status,
            cancellationToken);

        return horses
            .Select(h => new HorseListItemResponse(
                h.HorseId,
                h.Name,
                h.Breed,
                h.BirthYear,
                h.Color,
                h.ImageUrl,
                h.Status,
                h.RejectionReason))
            .ToList();
    }
}
