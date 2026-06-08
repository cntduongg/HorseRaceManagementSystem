
using Application.Usecases.Races.CreateRace;
using Application.Usecases.Races.GetRaceDetail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/races")]
public sealed class RacesController : ControllerBase
{
    private readonly ISender _sender;

    public RacesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateRaceCommand command,
        CancellationToken cancellationToken)
    {
        var raceId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { raceId },
            new { raceId });
    }

    [HttpGet("{raceId:guid}")]
    public async Task<ActionResult<RaceDetailResponse>> GetById(
        [FromRoute] Guid raceId,
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
}