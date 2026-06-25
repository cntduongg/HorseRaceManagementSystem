using Application.Usecases.Horses.CreateHorse;
using Application.Usecases.Horses.DeleteHorse;
using Application.Usecases.Horses.GetHorseDetail;
using Application.Usecases.Horses.GetHorseList;
using Application.Usecases.Horses.UpdateHorse;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers;

[ApiController]
[Route("api/horses")]
[Authorize]
public sealed class HorsesController : ControllerBase
{
    private readonly ISender _sender;

    public HorsesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "HORSE_OWNER")]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateHorseCommand command,
        CancellationToken cancellationToken)
    {
        var horseId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { horseId },
            new { horseId });
    }

    [HttpGet]
    public async Task<ActionResult<List<HorseListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetHorseListQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{horseId:int}")]
    public async Task<ActionResult<HorseDetailResponse>> GetById(
        [FromRoute] int horseId,
        CancellationToken cancellationToken)
    {
        var horse = await _sender.Send(
            new GetHorseDetailQuery(horseId),
            cancellationToken);

        if (horse is null)
        {
            return NotFound(new
            {
                message = "Horse not found",
                horseId
            });
        }

        return Ok(horse);
    }

    [HttpPut("{horseId:int}")]
    [Authorize(Roles = "HORSE_OWNER")]
    public async Task<ActionResult> Update(
        [FromRoute] int horseId,
        [FromBody] UpdateHorseCommand command,
        CancellationToken cancellationToken)
    {
        if (horseId != command.HorseId)
        {
            return BadRequest(new
            {
                message = "Route id and request id do not match."
            });
        }

        var result = await _sender.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound(new
            {
                message = "Horse not found",
                horseId
            });
        }

        return NoContent();
    }

    [HttpDelete("{horseId:int}")]
    [Authorize(Roles = "HORSE_OWNER")]
    public async Task<ActionResult> Delete(
        [FromRoute] int horseId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteHorseCommand(horseId),
            cancellationToken);

        if (!result)
        {
            return NotFound(new
            {
                message = "Horse not found",
                horseId
            });
        }

        return NoContent();
    }
}