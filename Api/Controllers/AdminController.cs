using Application.Usecases.Admin.ApproveUser;
using Application.Usecases.Admin.GetPendingUsers;
using Application.Usecases.Admin.RejectUser;

using Application.Usecases.Admin.GetPendingHorses;
using Application.Usecases.Admin.ApproveHorse;
using Application.Usecases.Admin.RejectHorse;
using Application.Usecases.Admin.RevokeHorse;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public sealed class AdminController : ControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    // =========================
    // USERS
    // =========================

    [HttpGet("users/pending")]
    public async Task<IActionResult> GetPendingUsers(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPendingUsersQuery(), ct);
        return Ok(result);
    }

    [HttpPost("users/{id:int}/approve")]
    public async Task<IActionResult> ApproveUser(int id, CancellationToken ct)
    {
        var result = await _sender.Send(new ApproveUserCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("users/{id:int}/reject")]
    public async Task<IActionResult> RejectUser(int id, [FromBody] RejectUserRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RejectUserCommand(id, request.Reason), ct);
        return Ok(result);
    }

    // =========================
    // HORSES
    // =========================

    /// <summary>
    /// Get all pending horses for admin review
    /// </summary>
    [HttpGet("horses/pending")]
    public async Task<IActionResult> GetPendingHorses(CancellationToken ct)
    {
        var result = await _sender.Send(new GetPendingHorsesQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Approve horse registration
    /// </summary>
    [HttpPost("horses/{id:int}/approve")]
    public async Task<IActionResult> ApproveHorse(int id, CancellationToken ct)
    {
        // TODO: get AdminId from JWT
        var adminId = GetUserId();

        var result = await _sender.Send(new ApproveHorseCommand(id, adminId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Reject horse registration
    /// </summary>
    [HttpPost("horses/{id:int}/reject")]
    public async Task<IActionResult> RejectHorse(int id, [FromBody] RejectHorseRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RejectHorseCommand(id, request.Reason), ct);
        return Ok(result);
    }

    /// <summary>
    /// Revoke approved horse + cancel all entries
    /// </summary>
    [HttpPost("horses/{id:int}/revoke")]
    public async Task<IActionResult> RevokeHorse(int id, CancellationToken ct)
    {
        var result = await _sender.Send(new RevokeHorseCommand(id), ct);
        return Ok(result);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return int.Parse(claim!);
    }
}

public sealed record RejectUserRequest(string? Reason);
public sealed record RejectHorseRequest(string? Reason);