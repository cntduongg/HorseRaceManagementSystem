namespace Application.Usecases.JockeyInvitations.GetJockeyInvitationList;

public sealed record JockeyInvitationListItemResponse(
    int InvitationId,
    int HorseOwnerId,
    int JockeyId,
    string Status,
    DateTime SentAt
);