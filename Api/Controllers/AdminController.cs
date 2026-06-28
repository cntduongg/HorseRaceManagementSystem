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

using Application.Usecases.Admin.GetAdminViolations;
using Application.Usecases.Admin.PointsManagement;
using Application.Usecases.Admin.Discrepancies;
using Application.Usecases.Violations.ApproveViolation;
using Application.Usecases.Violations.RejectViolation;
using Application.Usecases.Admin.LockUser;
using Application.Usecases.Admin.UnlockUser;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Application.Usecases.Admin.ResultPublication;
using Application.Usecases.RaceExecution;

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

    
    [HttpGet("races/{raceId:int}/publication-review")]
    public async Task<IActionResult> ReviewRacePublication(
        [FromRoute] int raceId,
        CancellationToken ct)
    {
        return Ok(await _sender.Send(
            new ReviewRacePublicationQuery(raceId),
            ct));
    }

// Flow 8.43–47 — Publish race result + prize points + leaderboards/career + settle bets + payouts.
    [HttpPost("races/{raceId:int}/publish")]
    public async Task<IActionResult> PublishRaceResult(
        [FromRoute] int raceId,
        CancellationToken ct)
    {
        return Ok(await _sender.Send(
            new PublishRaceResultCommand(raceId, GetUserId()),
            ct));
    }
    
    [HttpPost("races/{raceId:int}/unpublish")]
    public async Task<IActionResult> UnpublishRaceResult(
        [FromRoute] int raceId,
        CancellationToken ct)
    {
        return Ok(await _sender.Send(
            new UnpublishRaceResultCommand(raceId, GetUserId()),
            ct));
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

    // Khóa/Mở khóa tài khoản (Flow 7: khóa Spectator → đóng băng ví + hoàn cược Pending).
    [HttpPost("users/{id:int}/lock")]
    public async Task<IActionResult> LockUser(int id, [FromBody] LockUserRequest? req, CancellationToken ct)
        => Ok(await _sender.Send(new LockUserCommand(id, GetUserId(), req?.Reason), ct));

    [HttpPost("users/{id:int}/unlock")]
    public async Task<IActionResult> UnlockUser(int id, CancellationToken ct)
        => Ok(await _sender.Send(new UnlockUserCommand(id, GetUserId()), ct));

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
    // VIOLATIONS (review)
    // =========================

    [HttpGet("violations")]
    public async Task<IActionResult> GetViolations(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 15,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new GetAdminViolationsQuery(status, page, pageSize), ct));

    [HttpPost("violations/{id:int}/approve")]
    public async Task<IActionResult> ApproveViolation(
        int id, [FromBody] ApproveViolationRequest req, CancellationToken ct)
        => Ok(await _sender.Send(
            new ApproveViolationCommand(id, GetUserId(), req?.Penalty, req?.AdminNote), ct));

    [HttpPost("violations/{id:int}/reject")]
    public async Task<IActionResult> RejectViolation(
        int id, [FromBody] RejectViolationRequest req, CancellationToken ct)
        => Ok(await _sender.Send(
            new RejectViolationCommand(id, GetUserId(), req?.Reason ?? ""), ct));

    // =========================
    // POINTS MANAGEMENT
    // =========================

    [HttpGet("points/balances")]
    public async Task<IActionResult> GetPointBalances(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new GetPointBalancesQuery(search, page, pageSize), ct));

    [HttpGet("points/transactions")]
    public async Task<IActionResult> GetPointTransactions(
        [FromQuery] string? search, [FromQuery] string? type,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new GetPointTransactionsQuery(search, type, page, pageSize), ct));

    [HttpPost("points/adjust")]
    public async Task<IActionResult> AdjustPoints(
        [FromBody] AdjustPointsRequest req, CancellationToken ct)
        => Ok(await _sender.Send(
            new AdjustPointsCommand(req.UserId, req.Amount, req.Type, req.Reason, GetUserId()), ct));

    // Kích hoạt thủ công top-up tuần (tiện test; thực tế chạy tự động qua background service).
    [HttpPost("points/weekly-topup")]
    public async Task<IActionResult> RunWeeklyTopUp(CancellationToken ct)
        => Ok(await _sender.Send(new RunWeeklyTopUpCommand(), ct));

    // =========================
    // DISCREPANCIES
    // =========================

    [HttpGet("discrepancies")]
    public async Task<IActionResult> GetDiscrepancies(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 15,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new GetDiscrepanciesQuery(status, page, pageSize), ct));

    [HttpPost("discrepancies/{id:int}/resolve")]
    public async Task<IActionResult> ResolveDiscrepancy(
        int id, [FromBody] ResolveDiscrepancyRequest req, CancellationToken ct)
        => Ok(await _sender.Send(new ResolveDiscrepancyCommand(
            id, req.Resolution, req.Action, req.AdjustedPointsAwarded, GetUserId()), ct));

    // =========================
    // COMMON
    // =========================
    private int GetUserId()
    {
        var claim =
            User.FindFirst("userId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing userId claim");

        return userId;
    }
}

// =========================
// REQUEST DTOs
// =========================

public sealed record RejectUserRequest(string? Reason);
public sealed record RejectHorseRequest(string? Reason);
public sealed record RejectEntryRequest(string? Reason);
public sealed record AdjustPointsRequest(int UserId, decimal Amount, string Type, string? Reason);
public sealed record ResolveDiscrepancyRequest(string Resolution, string Action, int AdjustedPointsAwarded);
public sealed record ApproveViolationRequest(string? Penalty, string? AdminNote);
public sealed record RejectViolationRequest(string? Reason);
public sealed record LockUserRequest(string? Reason);