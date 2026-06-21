using MediatR;

namespace Application.Usecases.PrizePointTransactions.CreatePrizePointTransaction;

public sealed record CreatePrizePointTransactionCommand(
    int RaceResultId,
    int TournamentId,
    int RaceId,
    int EntryId,
    int UserId,
    string EntityType,
    int FinalPosition,
    int Points,
    string Type
) : IRequest<int>;