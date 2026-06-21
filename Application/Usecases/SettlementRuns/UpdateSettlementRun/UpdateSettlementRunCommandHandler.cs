using MediatR;

namespace Application.Usecases.SettlementRuns.UpdateSettlementRun;

public sealed class UpdateSettlementRunCommandHandler
    : IRequestHandler<UpdateSettlementRunCommand, bool>
{
    public Task<bool> Handle(
        UpdateSettlementRunCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update settlement run

        return Task.FromResult(true);
    }
}