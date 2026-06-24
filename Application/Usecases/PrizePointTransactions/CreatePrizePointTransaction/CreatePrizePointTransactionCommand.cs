using MediatR;
using Domain.Aggregates.Enums;

namespace Application.Usecases.PrizePointTransactions.CreatePrizePointTransaction;

public sealed record CreatePrizePointTransactionCommand(
    int TournamentId,
    int RaceId,
    int EntryId,
    int UserId,
    string SourceType,
    int FinalPosition,
    int Points,
    PrizePointTransactionType TransactionType,
    int? RollbackOfId
) : IRequest<int>;