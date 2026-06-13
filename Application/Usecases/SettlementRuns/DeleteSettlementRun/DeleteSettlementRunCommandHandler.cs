using MediatR;

namespace Application.Usecases.SettlementRuns.DeleteSettlementRun;

public sealed class DeleteSettlementRunCommandHandler
    : IRequestHandler<DeleteSettlementRunCommand, bool>
{
    public Task<bool> Handle(
        DeleteSettlementRunCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete settlement run

        return Task.FromResult(true);
    }
}