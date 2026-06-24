namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationList;

public sealed record JockeyInvitationListItemResponse(
    int InvitationId,

    int HorseOwnerId,
    string HorseOwnerName,

    int JockeyId,
    string JockeyName,

    int RaceId,
    string RaceName,

    int HorseId,
    string HorseName,

    string Status,
    DateTime SentAt
);