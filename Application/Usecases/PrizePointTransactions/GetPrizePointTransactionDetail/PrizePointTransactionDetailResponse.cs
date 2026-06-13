namespace Application.Usecases.PrizePointTransactions.GetPrizePointTransactionDetail;

public sealed record PrizePointTransactionDetailResponse(
    int PrizePointTransactionId,
    int RaceResultId,
    int TournamentId,
    int RaceId,
    int EntryId,
    int UserId,
    string EntityType,
    int FinalPosition,
    int Points,
    string Type
);