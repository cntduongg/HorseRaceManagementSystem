using Application.Usecases.Admin.ApproveUser;
using Application.Usecases.Admin.GetPendingUsers;
using Application.Usecases.Admin.RejectUser;

using Application.Usecases.Admin.GetPendingHorses;
using Application.Usecases.Admin.ApproveHorse;
using Application.Usecases.Admin.RejectHorse;
using Application.Usecases.Admin.RevokeHorse;

using Application.Usecases.Admin.GetPendingEntries;
using Application.Usecases.Admin.ApproveEntry;
using Application.Usecases.Admin.RejectEntry;

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
        => Ok(await _sender.Send(new GetPendingUsersQuery(), ct));

    [HttpPost("users/{id:int}/approve")]
    public async Task<IActionResult> ApproveUser(int id, CancellationToken ct)
        => Ok(await _sender.Send(new ApproveUserCommand(id), ct));

    [HttpPost("users/{id:int}/reject")]
    public async Task<IActionResult> RejectUser(int id, [FromBody] RejectUserRequest req, CancellationToken ct)
        => Ok(await _sender.Send(new RejectUserCommand(id, req.Reason), ct));

    // =========================
    // HORSES
    // =========================

    [HttpGet("horses/pending")]
    public async Task<IActionResult> GetPendingHorses(CancellationToken ct)
        => Ok(await _sender.Send(new GetPendingHorsesQuery(), ct));

    [HttpPost("horses/{id:int}/approve")]
    public async Task<IActionResult> ApproveHorse(int id, CancellationToken ct)
    {
        var adminId = GetUserId();
        return Ok(await _sender.Send(new ApproveHorseCommand(id, adminId), ct));
    }

    [HttpPost("horses/{id:int}/reject")]
    public async Task<IActionResult> RejectHorse(int id, [FromBody] RejectHorseRequest req, CancellationToken ct)
        => Ok(await _sender.Send(new RejectHorseCommand(id, req.Reason), ct));

    [HttpPost("horses/{id:int}/revoke")]
    public async Task<IActionResult> RevokeHorse(int id, CancellationToken ct)
        => Ok(await _sender.Send(new RevokeHorseCommand(id), ct));

    // =========================
    // ENTRIES
    // =========================

    [HttpGet("entries/pending")]
    public async Task<IActionResult> GetPendingEntries(CancellationToken ct)
        => Ok(await _sender.Send(new GetPendingEntriesQuery(), ct));

    [HttpPost("entries/{id:int}/approve")]
    public async Task<IActionResult> ApproveEntry(int id, CancellationToken ct)
    {
        var adminId = GetUserId();
        return Ok(await _sender.Send(new ApproveEntryCommand(id, adminId), ct));
    }

    [HttpPost("entries/{id:int}/reject")]
    public async Task<IActionResult> RejectEntry(int id, [FromBody] RejectEntryRequest req, CancellationToken ct)
        => Ok(await _sender.Send(new RejectEntryCommand(id, req.Reason), ct));

    // =========================
    // COMMON
    // =========================

    private int GetUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        return int.Parse(claim!);
    }
}

// =========================
// REQUEST DTOs
// =========================

public sealed record RejectUserRequest(string? Reason);
public sealed record RejectHorseRequest(string? Reason);
public sealed record RejectEntryRequest(string? Reason);