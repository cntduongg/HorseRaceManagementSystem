using Application.Usecases.Races.CreateRace;
using Application.Usecases.Races.GetRaceDetail;
using Application.Usecases.Races.DeleteRace;
using Application.Usecases.Races.GetRaceList;
using Application.Usecases.Races.UpdateRace;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace Api.Controllers;


[ApiController]
[Route("api/races")]
[Authorize]
public sealed class RacesController : ControllerBase
{
    private readonly ISender _sender;

    public RacesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateRaceCommand command,
        CancellationToken cancellationToken)
    {
        var raceId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { raceId },
            new { raceId });
    }

    [HttpGet("{raceId:int}")]
    public async Task<ActionResult<RaceDetailResponse>> GetById(
        [FromRoute] int raceId,
        CancellationToken cancellationToken)
    {
        var race = await _sender.Send(
            new GetRaceDetailQuery(raceId),
            cancellationToken);

        if (race is null)
        {
            return NotFound(new
            {
                message = "Race not found",
                raceId
            });
        }

        return Ok(race);
    }

    [HttpGet]
    public async Task<ActionResult<List<RaceListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var races = await _sender.Send(
            new GetRaceListQuery(),
            cancellationToken);

        return Ok(races);
    }

    [HttpPut("{raceId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Update(
        [FromRoute] int raceId,
        [FromBody] UpdateRaceCommand command,
        CancellationToken cancellationToken)
    {
        if (raceId != command.RaceId)
        {
            return BadRequest(new
            {
                message = "RaceId mismatch"
            });
        }

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (!result)
        {
            return NotFound(new
            {
                message = "Race not found"
            });
        }

        return Ok(new
        {
            success = true
        });
    }

    [HttpDelete("{raceId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Delete(
        [FromRoute] int raceId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteRaceCommand(raceId),
            cancellationToken);

        if (!result)
        {
            return NotFound(new
            {
                message = "Race not found"
            });
        }

        return Ok(new
        {
            success = true
        });
    }
}