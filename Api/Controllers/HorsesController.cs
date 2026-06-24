using Application.Common;
using Application.Usecases.Horses.ApproveHorse;
using Application.Usecases.Horses.CreateHorse;
using Application.Usecases.Horses.DeleteHorse;
using Application.Usecases.Horses.GetHorseDetail;
using Application.Usecases.Horses.GetHorseList;
using Application.Usecases.Horses.RejectHorse;
using Application.Usecases.Horses.RevokeHorse;
using Application.Usecases.Horses.UpdateHorse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/horses")]
[Authorize]
public sealed class HorsesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public HorsesController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>Horse Owner registers a new horse (starts as Pending). Owner is taken from the JWT.</summary>
    [Authorize(Roles = "HORSE_OWNER")]
    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateHorseCommand command,
        CancellationToken cancellationToken)
    {
        command = command with { OwnerId = _currentUser.UserId ?? 0 };

        var horseId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { horseId },
            new { horseId });
    }

    /// <summary>
    /// Lists horses. Horse Owners see only their own; admins see all and may filter by status.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<HorseListItemResponse>>> GetAll(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        int? ownerId = _currentUser.Role == "HORSE_OWNER" ? _currentUser.UserId : null;

        var result = await _sender.Send(
            new GetHorseListQuery(ownerId, status),
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

    [Authorize(Roles = "HORSE_OWNER")]
    [HttpPut("{horseId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int horseId,
        [FromBody] UpdateHorseCommand command,
        CancellationToken cancellationToken)
    {
        command = command with
        {
            HorseId = horseId,
            RequesterId = _currentUser.UserId ?? 0
        };

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

    [Authorize(Roles = "HORSE_OWNER")]
    [HttpDelete("{horseId:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int horseId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteHorseCommand(horseId, _currentUser.UserId ?? 0),
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

    /// <summary>Admin approves a Pending horse (Flow 1).</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPost("{horseId:int}/approve")]
    public async Task<ActionResult<HorseActionResponse>> Approve(
        [FromRoute] int horseId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ApproveHorseCommand(horseId, _currentUser.UserId ?? 0),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Admin rejects a Pending horse with a mandatory reason (Flow 1).</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPost("{horseId:int}/reject")]
    public async Task<ActionResult<RejectHorseResponse>> Reject(
        [FromRoute] int horseId,
        [FromBody] RejectHorseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RejectHorseCommand(horseId, _currentUser.UserId ?? 0, request.Reason),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Admin revokes an Approved horse; Pending entries using it are auto-cancelled (Flow 1).</summary>
    [Authorize(Roles = "ADMIN")]
    [HttpPost("{horseId:int}/revoke")]
    public async Task<ActionResult<RevokeHorseResponse>> Revoke(
        [FromRoute] int horseId,
        [FromBody] RevokeHorseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RevokeHorseCommand(horseId, _currentUser.UserId ?? 0, request.Reason),
            cancellationToken);

        return Ok(result);
    }
}

public sealed record RejectHorseRequest(string Reason);
public sealed record RevokeHorseRequest(string? Reason);
