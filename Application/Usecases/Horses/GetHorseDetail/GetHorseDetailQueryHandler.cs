using Application.Common;
using MediatR;

namespace Application.Usecases.Horses.GetHorseDetail;

public sealed class GetHorseDetailQueryHandler
    : IRequestHandler<GetHorseDetailQuery, HorseDetailResponse?>
{
    private readonly IHorseRepository _horseRepository;

    public GetHorseDetailQueryHandler(IHorseRepository horseRepository)
    {
        _horseRepository = horseRepository;
    }

    public async Task<HorseDetailResponse?> Handle(
        GetHorseDetailQuery request,
        CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken);
        if (horse is null)
        {
            return null;
        }

        return new HorseDetailResponse(
            horse.HorseId,
            horse.OwnerId,
            horse.Name,
            horse.Breed,
            horse.BirthYear,
            horse.Color,
            horse.Status,
            horse.ImageUrl);
    }
}
