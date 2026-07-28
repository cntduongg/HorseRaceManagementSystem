using Domain.Aggregates.Entities;

namespace Application.Common.Wallet;

public static class WalletTransactionReasonBuilder
{
    public static string BetPlaced(
        Race race,
        Horse horse,
        User jockey)
    {
        return $"Bet placed | Race: {race.Name} | Horse: {horse.Name} | Jockey: {jockey.FullName}";
    }

    public static string BetCancelled(
        Race race,
        Horse horse,
        User jockey)
    {
        return $"Bet cancelled | Race: {race.Name} | Horse: {horse.Name} | Jockey: {jockey.FullName}";
    }

    public static string BetRefundHorseRevoked(
        Race race,
        Horse horse,
        User jockey)
    {
        return $"Bet refunded | Race: {race.Name} | Horse: {horse.Name} | Jockey: {jockey.FullName} | Reason: Horse revoked";
    }

    public static string BetRefundRaceCancelled(
        Race race,
        Horse horse,
        User jockey)
    {
        return $"Bet refunded | Race: {race.Name} | Horse: {horse.Name} | Jockey: {jockey.FullName} | Reason: Race cancelled";
    }

    public static string BetRefundAccountLocked(
        Race race,
        Horse horse,
        User jockey)
    {
        return $"Bet refunded | Race: {race.Name} | Horse: {horse.Name} | Jockey: {jockey.FullName} | Reason: Account locked";
    }

    public static string Payout(
        Race race,
        Horse horse,
        User jockey,
        decimal odds)
    {
        return $"Won bet | Race: {race.Name} | Horse: {horse.Name} | Jockey: {jockey.FullName} | Odds: {odds}";
    }

    public static string PayoutRollback(
        Race race,
        Horse horse,
        User jockey)
    {
        return $"Payout rollback | Race: {race.Name} | Horse: {horse.Name} | Jockey: {jockey.FullName}";
    }
}