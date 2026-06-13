namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationDetail;

public sealed record JockeyInvitationDetailResponse(
    int InvitationId,
    int HorseOwnerId,
    int JockeyId,
    int HorseId,
    int RaceId,
    string Status,
    string? Message,
    string? ResponseReason,
    DateTime SentAt
);