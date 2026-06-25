using Application.Usecases.Legs.CreateLeg;
using Application.Usecases.Legs.DeleteLeg;
using Application.Usecases.Legs.GetLegDetail;
using Application.Usecases.Legs.GetLegList;
using Application.Usecases.Legs.UpdateLeg;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers;

[ApiController]
[Route("api/legs")]
[Authorize(Roles = "REFEREE,ADMIN")]
public sealed class LegsController : ControllerBase
{
    private readonly ISender _sender;

    public LegsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateLegCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            command,
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }

    [HttpGet("{raceId:int}/{legNumber:int}")]
    public async Task<ActionResult<LegDetailResponse>> GetById(
        [FromRoute] int raceId,
        [FromRoute] int legNumber,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetLegDetailQuery(
                raceId,
                legNumber),
            cancellationToken);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Leg not found"
            });
        }

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<LegListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetLegListQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{raceId:int}/{legNumber:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int raceId,
        [FromRoute] int legNumber,
        [FromBody] UpdateLegCommand command,
        CancellationToken cancellationToken)
    {
        if (raceId != command.RaceId ||
            legNumber != command.LegNumber)
        {
            return BadRequest(new
            {
                message = "RaceId or LegNumber mismatch"
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

    [HttpDelete("{raceId:int}/{legNumber:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int raceId,
        [FromRoute] int legNumber,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteLegCommand(
                raceId,
                legNumber),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}