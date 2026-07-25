using Application.Usecases.Horses.CreateHorse;
using Application.Usecases.Horses.DeleteHorse;
using Application.Usecases.Horses.GetHorseDetail;
using Application.Usecases.Horses.GetHorseList;
using Application.Usecases.Horses.UpdateHorse;
using Application.Usecases.Horses.ResubmitHorse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        int? ownerScope = null;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        var userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (roleClaim == "HORSE_OWNER" && int.TryParse(userIdClaim, out var ownerId))
        {
            ownerScope = ownerId;
        }

        var result = await _sender.Send(
            new GetHorseListQuery(ownerScope),
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
            return NotFound(new { message = "Horse not found", horseId });
        }

        return Ok(horse);
    }

    [HttpPut("{horseId:int}")]
    [Authorize(Roles = "HORSE_OWNER")]
    public async Task<ActionResult> Update(
        [FromRoute] int horseId,
        [FromBody] UpdateHorseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(
            new UpdateHorseCommand(
                horseId,
                ownerId,
                request.Name,
                request.Breed,
                request.BirthYear,
                request.Color,
                request.ImageUrl),
            cancellationToken);

        return result.Error switch
        {
            UpdateHorseError.NotFound => NotFound(new { message = "Horse not found", horseId }),
            UpdateHorseError.Forbidden => Forbid(),
            UpdateHorseError.InvalidStatus => BadRequest(new { message = result.Message }),
            _ => NoContent()
        };
    }

    [HttpDelete("{horseId:int}")]
    [Authorize(Roles = "HORSE_OWNER")]
    public async Task<ActionResult> Delete(
        [FromRoute] int horseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(
            new DeleteHorseCommand(horseId, ownerId),
            cancellationToken);

        return result.Error switch
        {
            DeleteHorseError.NotFound => NotFound(new { message = "Horse not found", horseId }),
            DeleteHorseError.Forbidden => Forbid(),
            _ => NoContent()
        };
    }

    [HttpPost("{horseId:int}/resubmit")]
    [Authorize(Roles = "HORSE_OWNER")]
    public async Task<ActionResult> Resubmit(
        [FromRoute] int horseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var ownerId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(
            new ResubmitHorseCommand(horseId, ownerId),
            cancellationToken);

        return result.Error switch
        {
            ResubmitHorseError.NotFound => NotFound(new { message = "Horse not found", horseId }),
            ResubmitHorseError.Forbidden => Forbid(),
            ResubmitHorseError.InvalidStatus => BadRequest(new
            {
                message = "Only rejected horses can be resubmitted."
            }),
            _ => Ok(new { horseId = result.HorseId, status = result.Status })
        };
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirst("userId")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(claim, out userId);
    }
}

public sealed record UpdateHorseRequest(
    string Name,
    string? Breed,
    int? BirthYear,
    string? Color,
    string? ImageUrl);