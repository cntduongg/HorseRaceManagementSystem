namespace Application.Common;

public interface IRegistrationService
{
	Task CloseRegistrationAsync(
		int raceId,
		CancellationToken cancellationToken = default);
}