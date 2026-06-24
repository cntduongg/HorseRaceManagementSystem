namespace Application.Usecases.PrizePointTransactions.GetPrizePointTransactionDetail;

public sealed record PrizePointTransactionDetailResponse(
    int PrizePointTransactionId,
    int TournamentId,
    int RaceId,
    int EntryId,
    int UserId,
    string SourceType,
    int FinalPosition,
    int Points,
    string TransactionType,
    int? RollbackOfId
);