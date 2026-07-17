using Application.Usecases.Predictions.CreatePrediction;
using Application.Usecases.Predictions.DeletePrediction;
using Application.Usecases.Predictions.GetPredictionDetail;
using Application.Usecases.Predictions.GetPredictionList;
using Application.Usecases.Predictions.GetRacePredictionOdds;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.DTOs;

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

    // Flow 7.35
    // Spectator xem locked odds của từng Entry trong Race đang Scheduled.
    [HttpGet("races/{raceId:int}/legs/{legNumber:int}/odds")]
    [Authorize(Roles = "SPECTATOR,ADMIN,REFEREE,HORSE_OWNER")]
    public async Task<ActionResult<RacePredictionOddsResponse>> GetLegOdds(
        [FromRoute] int raceId,
        [FromRoute] int legNumber,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetLegPredictionOddsQuery(raceId, legNumber),
            cancellationToken);
        return Ok(result);
    }

    // Flow 7.36 - 7.38
    // Spectator đặt prediction: chọn 1 Entry + BetAmount.
    // RaceId lấy từ route, SpectatorId lấy từ JWT, không tin body.
    [HttpPost("races/{raceId:int}/legs/{legNumber:int}")]
    [Authorize(Roles = "SPECTATOR")]
    public async Task<ActionResult> Create(
        [FromRoute] int raceId,
        [FromRoute] int legNumber,
        [FromBody] PredictionRequest request,
        CancellationToken cancellationToken)
    {
        var predictionId = await _sender.Send(
            new CreatePredictionCommand(
                RaceId: raceId,
                LegNumber: legNumber,
                SpectatorId: GetUserId(),
                FirstEntryId: request.EntryId,
                BetAmount: request.BetAmount),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { predictionId }, new { predictionId });
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


    [HttpDelete("{predictionId:int}/cancel")]
    [Authorize(Roles = "SPECTATOR")]
    public async Task<ActionResult> Cancel(
        [FromRoute] int predictionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeletePredictionCommand(predictionId, GetUserId()),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }

    private int GetUserId()
    {
        var claim =
            User.FindFirst("userId")?.Value ??
            User.FindFirst("UserId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value;

        if (!int.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing userId claim");
        }

        return userId;
    }
}