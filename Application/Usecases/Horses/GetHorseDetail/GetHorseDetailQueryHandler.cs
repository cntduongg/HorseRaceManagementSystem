using MediatR;

namespace Application.Usecases.Horses.GetHorseDetail;

public sealed class GetHorseDetailQueryHandler
    : IRequestHandler<GetHorseDetailQuery, HorseDetailResponse?>
{
    public Task<HorseDetailResponse?> Handle(
        GetHorseDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (request.HorseId <= 0)
        {
            return Task.FromResult<HorseDetailResponse?>(null);
        }

        var response = new HorseDetailResponse(
            HorseId: request.HorseId,
            OwnerId: 1,
            Name: "Demo Horse",
            Breed: "Arabian",
            BirthYear: 2020,
            Color: "Brown",
            Status: "Pending",
            ImageUrl: null
        );

        return Task.FromResult<HorseDetailResponse?>(response);
    }
}