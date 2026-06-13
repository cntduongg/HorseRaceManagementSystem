using MediatR;

namespace Application.Usecases.SettlementRuns.CreateSettlementRun;

public sealed class CreateSettlementRunCommandHandler
    : IRequestHandler<CreateSettlementRunCommand, int>
{
    public Task<int> Handle(
        CreateSettlementRunCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Save settlement run to database

        var settlementRunId = 1;

        return Task.FromResult(settlementRunId);
    }
}