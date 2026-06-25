using Application.Usecases.RaceExecution;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

// Các endpoint vận hành đua (Flow 3-5, 8). Cùng prefix api/races với RacesController (CRUD),
// route con không trùng nhau.
[ApiController]
[Route("api/races")]
[Authorize]
public sealed class RaceExecutionController : ControllerBase
{
    private readonly ISender _sender;

    public RaceExecutionController(ISender sender)
    {
        _sender = sender;
    }

    // ── Race lifecycle ───────────────────────────────────────────────

    [HttpPost("{raceId:int}/start")]
    [Authorize(Roles = "REFEREE,ADMIN")]
    public async Task<IActionResult> Start(int raceId, CancellationToken ct)
        => Ok(await _sender.Send(new StartRaceCommand(raceId, GetUserId()), ct));

    [HttpPost("{raceId:int}/resume")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Resume(int raceId, CancellationToken ct)
        => Ok(await _sender.Send(new ResumeRaceCommand(raceId), ct));

    [HttpGet("{raceId:int}/execution")]
    public async Task<IActionResult> Execution(int raceId, CancellationToken ct)
        => Ok(await _sender.Send(new GetRaceExecutionQuery(raceId, GetUserIdOrZero()), ct));

    [HttpGet("{raceId:int}/standings")]
    public async Task<IActionResult> Standings(int raceId, CancellationToken ct)
        => Ok(await _sender.Send(new GetRaceStandingsQuery(raceId), ct));

    [HttpGet("{raceId:int}/pause")]
    [Authorize(Roles = "REFEREE,ADMIN")]
    public async Task<IActionResult> Pause(int raceId, CancellationToken ct)
        => Ok(await _sender.Send(new GetRacePauseQuery(raceId), ct));

    // ── Blind double-entry (Flow 4) ──────────────────────────────────

    [HttpGet("{raceId:int}/legs/{legIndex:int}/referee-view")]
    [Authorize(Roles = "REFEREE")]
    public async Task<IActionResult> RefereeView(int raceId, int legIndex, CancellationToken ct)
        => Ok(await _sender.Send(new GetRefereeLegViewQuery(raceId, legIndex, GetUserId()), ct));

    [HttpPut("{raceId:int}/legs/{legIndex:int}/draft")]
    [Authorize(Roles = "REFEREE")]
    public async Task<IActionResult> Draft(
        int raceId, int legIndex, [FromBody] LegEntriesRequest body, CancellationToken ct)
        => Ok(await _sender.Send(
            new SaveLegDraftCommand(raceId, legIndex, GetUserId(), body.Entries ?? new()), ct));

    [HttpPost("{raceId:int}/legs/{legIndex:int}/submit")]
    [Authorize(Roles = "REFEREE")]
    public async Task<IActionResult> Submit(
        int raceId, int legIndex, [FromBody] LegEntriesRequest body, CancellationToken ct)
        => Ok(await _sender.Send(
            new SubmitLegResultCommand(raceId, legIndex, GetUserId(), body.Entries ?? new()), ct));

    // ── Resolve conflict (Flow 5) ────────────────────────────────────

    [HttpPost("{raceId:int}/legs/{legIndex:int}/override")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Override(
        int raceId, int legIndex, [FromBody] OverrideRequest body, CancellationToken ct)
        => Ok(await _sender.Send(
            new OverrideLegResultCommand(
                raceId, legIndex, GetUserId(), body.OverrideReason, body.Decisions ?? new()), ct));

    // ── Publish / Unpublish (Flow 8) ─────────────────────────────────

    [HttpPost("{raceId:int}/publish")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Publish(int raceId, CancellationToken ct)
        => Ok(await _sender.Send(new PublishRaceResultCommand(raceId, GetUserId()), ct));

    [HttpPost("{raceId:int}/unpublish")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Unpublish(int raceId, CancellationToken ct)
        => Ok(await _sender.Send(new UnpublishRaceResultCommand(raceId, GetUserId()), ct));

    // ── Helpers ──────────────────────────────────────────────────────

    private int GetUserId()
    {
        var claim =
            User.FindFirst("userId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing userId claim");

        return userId;
    }

    private int GetUserIdOrZero()
    {
        var claim =
            User.FindFirst("userId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(claim, out var userId) ? userId : 0;
    }
}

// Request DTOs
public sealed record LegEntriesRequest(List<SubmitPositionItem>? Entries);
public sealed record OverrideRequest(string? OverrideReason, List<OverrideDecisionItem>? Decisions);
