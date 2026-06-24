namespace Domain.Aggregates.Enums;

public enum PrizePointTransactionType
{
    Awarded = 1,
    Deducted = 2,
    Rollback = 3,
    ManualAdjust = 4
}