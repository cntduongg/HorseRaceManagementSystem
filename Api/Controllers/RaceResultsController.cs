using Application.Usecases.RaceResults.CreateRaceResult;
using Application.Usecases.RaceResults.DeleteRaceResult;
using Application.Usecases.RaceResults.GetRaceResultDetail;
using Application.Usecases.RaceResults.GetRaceResultList;
using Application.Usecases.RaceResults.UpdateRaceResult;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/race-results")]
public sealed class RaceResultsController : ControllerBase
{
    private readonly ISender _sender;

    public RaceResultsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult> Create(
        [FromBody] CreateRaceResultCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null)
            return BadRequest(new { message = "Invalid request body" });

        var result = await _sender.Send(command, cancellationToken);

        return Ok(new { success = result });
    }

    [HttpGet("{raceId:int}/{entryId:int}")]
    public async Task<ActionResult<RaceResultDetailResponse>> GetById(
        int raceId,
        int entryId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetRaceResultDetailQuery(raceId, entryId),
            cancellationToken);

        if (result is null)
            return NotFound(new { message = "Race result not found" });

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<RaceResultListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRaceResultListQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{raceId:int}/{entryId:int}")]
    public async Task<ActionResult> Update(
        int raceId,
        int entryId,
        [FromBody] UpdateRaceResultCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null)
            return BadRequest(new { message = "Invalid request body" });

        if (raceId != command.RaceId || entryId != command.EntryId)
            return BadRequest(new { message = "RaceId or EntryId mismatch" });

        var result = await _sender.Send(command, cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{raceId:int}/{entryId:int}")]
    public async Task<ActionResult> Delete(
        int raceId,
        int entryId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteRaceResultCommand(raceId, entryId),
            cancellationToken);

        if (!result)
            return NotFound();

        return NoContent();
    }
}