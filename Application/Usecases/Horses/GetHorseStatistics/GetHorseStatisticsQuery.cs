using MediatR;

namespace Application.Usecases.Horses.GetHorseStatistics;

public sealed record GetHorseStatisticsQuery(int HorseId) : IRequest<HorseStatisticsResponse?>;