using MediatR;

namespace Application.Usecases.LegOfficialResults.CreateLegOfficialResult;

public sealed class CreateLegOfficialResultCommandHandler
    : IRequestHandler<CreateLegOfficialResultCommand, bool>
{
    public Task<bool> Handle(
        CreateLegOfficialResultCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Save to database

        return Task.FromResult(true);
    }
}