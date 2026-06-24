namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationDetail;

public sealed record JockeyInvitationDetailResponse(
    int InvitationId,

    int HorseOwnerId,
    string HorseOwnerName,

    int JockeyId,
    string JockeyName,

    int HorseId,
    string HorseName,

    int RaceId,
    string RaceName,

    string Status,
    string? Message,
    string? ResponseReason,
    DateTime SentAt
);