using Application.Usecases.Spectators.CreateSpectator;
using Application.Usecases.Spectators.DeleteSpectator;
using Application.Usecases.Spectators.GetSpectatorDetail;
using Application.Usecases.Spectators.GetSpectatorList;
using Application.Usecases.Spectators.UpdateSpectator;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/spectators")]
public sealed class SpectatorsController : ControllerBase
{
    private readonly ISender _sender;

    public SpectatorsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateSpectatorCommand command,
        CancellationToken cancellationToken)
    {
        var userId = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { userId },
            new { userId });
    }

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<SpectatorDetailResponse>> GetById(
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        var spectator = await _sender.Send(
            new GetSpectatorDetailQuery(userId),
            cancellationToken);

        if (spectator is null)
        {
            return NotFound(new
            {
                message = "Spectator not found"
            });
        }

        return Ok(spectator);
    }

    [HttpGet]
    public async Task<ActionResult<List<SpectatorListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var spectators = await _sender.Send(
            new GetSpectatorListQuery(),
            cancellationToken);

        return Ok(spectators);
    }

    [HttpPut("{userId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int userId,
        [FromBody] UpdateSpectatorCommand command,
        CancellationToken cancellationToken)
    {
        if (userId != command.UserId)
        {
            return BadRequest(new
            {
                message = "UserId mismatch"
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

    [HttpDelete("{userId:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteSpectatorCommand(userId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}