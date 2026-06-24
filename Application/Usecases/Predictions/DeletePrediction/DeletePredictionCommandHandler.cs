using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.DeletePrediction;

public sealed class DeletePredictionCommandHandler
    : IRequestHandler<DeletePredictionCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeletePredictionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeletePredictionCommand request,
        CancellationToken cancellationToken)
    {
        var prediction = await _context.Predictions
            .FirstOrDefaultAsync(x => x.PredictionId == request.PredictionId, cancellationToken);

        if (prediction is null)
            return false;

        _context.Predictions.Remove(prediction);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}