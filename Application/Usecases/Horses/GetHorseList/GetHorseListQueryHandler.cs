using MediatR;

namespace Application.Usecases.Horses.GetHorseList;

public sealed class GetHorseListQueryHandler
    : IRequestHandler<GetHorseListQuery, List<HorseListItemResponse>>
{
    public Task<List<HorseListItemResponse>> Handle(
        GetHorseListQuery request,
        CancellationToken cancellationToken)
    {
        var data = new List<HorseListItemResponse>
        {
            new(
                HorseId: 1,
                Name: "Thunder",
                Status: "Approved",
                Breed: "Arabian"
            ),
            new(
                HorseId: 2,
                Name: "Lightning",
                Status: "Pending",
                Breed: "Mongolian"
            )
        };

        return Task.FromResult(data);
    }
}