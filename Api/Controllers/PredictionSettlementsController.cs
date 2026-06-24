using Application.Usecases.PredictionSettlements.CreatePredictionSettlement;
using Application.Usecases.PredictionSettlements.DeletePredictionSettlement;
using Application.Usecases.PredictionSettlements.GetPredictionSettlementDetail;
using Application.Usecases.PredictionSettlements.GetPredictionSettlementList;
using Application.Usecases.PredictionSettlements.UpdatePredictionSettlement;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/prediction-settlements")]
[Authorize(Roles = "ADMIN")]
public sealed class PredictionSettlementsController : ControllerBase
{
    private readonly ISender _sender;

    public PredictionSettlementsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreatePredictionSettlementCommand command,
        CancellationToken cancellationToken)
    {
        var predictionSettlementId = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { predictionSettlementId },
            new { predictionSettlementId });
    }

    [HttpGet("{predictionSettlementId:int}")]
    public async Task<ActionResult<PredictionSettlementDetailResponse>> GetById(
        [FromRoute] int predictionSettlementId,
        CancellationToken cancellationToken)
    {
        var predictionSettlement = await _sender.Send(
            new GetPredictionSettlementDetailQuery(predictionSettlementId),
            cancellationToken);

        if (predictionSettlement is null)
        {
            return NotFound(new
            {
                message = "Prediction settlement not found"
            });
        }

        return Ok(predictionSettlement);
    }

    [HttpGet]
    public async Task<ActionResult<List<PredictionSettlementListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var predictionSettlements = await _sender.Send(
            new GetPredictionSettlementListQuery(),
            cancellationToken);

        return Ok(predictionSettlements);
    }

    [HttpPut("{predictionSettlementId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int predictionSettlementId,
        [FromBody] UpdatePredictionSettlementCommand command,
        CancellationToken cancellationToken)
    {
        if (predictionSettlementId != command.PredictionSettlementId)
        {
            return BadRequest(new
            {
                message = "PredictionSettlementId mismatch"
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

    [HttpDelete("{predictionSettlementId:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int predictionSettlementId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeletePredictionSettlementCommand(
                predictionSettlementId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}