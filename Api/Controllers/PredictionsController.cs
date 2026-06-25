using Application.Usecases.Predictions.CreatePrediction;
using Application.Usecases.Predictions.DeletePrediction;
using Application.Usecases.Predictions.GetPredictionDetail;
using Application.Usecases.Predictions.GetPredictionList;
using Application.Usecases.Predictions.UpdatePrediction;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers;

[ApiController]
[Route("api/predictions")]
[Authorize]
public sealed class PredictionsController : ControllerBase
{
    private readonly ISender _sender;

    public PredictionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "SPECTATOR")]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreatePredictionCommand command,
        CancellationToken cancellationToken)
    {
        var predictionId = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { predictionId },
            new { predictionId });
    }

    [HttpGet("{predictionId:int}")]
    public async Task<ActionResult<PredictionDetailResponse>> GetById(
        [FromRoute] int predictionId,
        CancellationToken cancellationToken)
    {
        var prediction = await _sender.Send(
            new GetPredictionDetailQuery(predictionId),
            cancellationToken);

        if (prediction is null)
        {
            return NotFound(new
            {
                message = "Prediction not found"
            });
        }

        return Ok(prediction);
    }

    [HttpGet]
    public async Task<ActionResult<List<PredictionListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var predictions = await _sender.Send(
            new GetPredictionListQuery(),
            cancellationToken);

        return Ok(predictions);
    }

    [HttpPut("{predictionId:int}")]
    [Authorize(Roles = "SPECTATOR")]
    public async Task<ActionResult> Update(
        [FromRoute] int predictionId,
        [FromBody] UpdatePredictionCommand command,
        CancellationToken cancellationToken)
    {
        if (predictionId != command.PredictionId)
        {
            return BadRequest(new
            {
                message = "PredictionId mismatch"
            });
        }

        var result = await _sender.Send(
            command,
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }

    [HttpDelete("{predictionId:int}")]
    [Authorize(Roles = "SPECTATOR")]
    public async Task<ActionResult> Delete(
        [FromRoute] int predictionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeletePredictionCommand(predictionId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}