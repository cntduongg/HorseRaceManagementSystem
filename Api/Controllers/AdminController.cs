using Application.Usecases.Admin.ApproveUser;
using Application.Usecases.Admin.GetPendingUsers;
using Application.Usecases.Admin.RejectUser;
using Application.Usecases.Admin.GetReviewHistory;
using Application.Usecases.Admin.GetInvalidUsers;
using Application.Usecases.Admin.GetUserHistory;
using Application.Usecases.Admin.RaceModeration;
using Application.Usecases.Users.GetUserDetail;
using Domain.Aggregates.Enums;
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
        [FromBody] UnpublishRaceRequest req,
        CancellationToken ct)
    {
        return Ok(await _sender.Send(
            new UnpublishRaceResultCommand(raceId, GetUserId(), req?.Reason ?? ""),
            ct));
    }

    // ── Race moderation (tùy chọn, không thuộc 8 flow) ──
    // Approve = xác nhận Race Scheduled; Reject = Cancel Race; Finish = PendingResult→Finished.
    [HttpPost("races/{raceId:int}/approve")]
    public async Task<IActionResult> ApproveRace(int raceId, CancellationToken ct)
        => Ok(await _sender.Send(new ApproveRaceCommand(raceId, GetUserId()), ct));

    [HttpPost("races/{raceId:int}/reject")]
    public async Task<IActionResult> RejectRace(
        int raceId, [FromBody] RejectRaceRequest? req, CancellationToken ct)
        => Ok(await _sender.Send(new RejectRaceCommand(raceId, GetUserId(), req?.Reason), ct));

    [HttpPost("races/{raceId:int}/finish")]
    public async Task<IActionResult> FinishRace(int raceId, CancellationToken ct)
        => Ok(await _sender.Send(new FinishRaceCommand(raceId, GetUserId()), ct));
    // =========================
    // USERS
    // =========================

    [HttpGet("users/pending")]
    public async Task<IActionResult> GetPendingUsers(CancellationToken ct)
        => Ok(await _sender.Send(new GetPendingUsersQuery(), ct));

    [HttpPost("users/{id:int}/approve")]
    public async Task<IActionResult> ApproveUser(
    int id,
    [FromBody] ApproveUserRequest? req,
    CancellationToken ct)
    => Ok(await _sender.Send(
        new ApproveUserCommand(
            id,
            GetUserId(),
            req?.Reason),
        ct));
    [HttpPost("users/{id:int}/reject")]
    public async Task<IActionResult> RejectUser(
        int id,
        [FromBody] RejectUserRequest req,
        CancellationToken ct)
        => Ok(await _sender.Send(
            new RejectUserCommand(
                id,
                GetUserId(),
                req.Reason),
            ct));

    // ── Invalid users (tùy chọn) — tài khoản không hoạt động (bị từ chối/khóa) ──
    [HttpGet("users/invalid")]
    public async Task<IActionResult> GetInvalidUsers(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
        => Ok(await _sender.Send(new GetInvalidUsersQuery(search, page, pageSize), ct));

    [HttpGet("users/invalid/{id:int}")]
    public async Task<IActionResult> GetInvalidUserById(int id, CancellationToken ct)
    {
        var user = await _sender.Send(new GetUserDetailQuery(id), ct);
        return user is null ? NotFound(new { message = "User not found" }) : Ok(user);
    }

    [HttpPost("users/invalid/{id:int}/approve")]
    public async Task<IActionResult> ApproveInvalidUser(
        int id, [FromBody] ApproveUserRequest? req, CancellationToken ct)
        => Ok(await _sender.Send(new ApproveUserCommand(id, GetUserId(), req?.Reason), ct));

    [HttpPost("users/invalid/{id:int}/reject")]
    public async Task<IActionResult> RejectInvalidUser(
        int id, [FromBody] RejectUserRequest req, CancellationToken ct)
        => Ok(await _sender.Send(new RejectUserCommand(id, GetUserId(), req.Reason), ct));

    // Lịch sử hoạt động của 1 user (ví + prize + duyệt hồ sơ).
    [HttpGet("users/{id:int}/history")]
    public async Task<IActionResult> GetUserHistory(
        int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetUserHistoryQuery(id, page, pageSize), ct));

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
    public async Task<IActionResult> ApproveHorse(
    int id,
    [FromBody] ApproveHorseRequest? req,
    CancellationToken ct)
    => Ok(await _sender.Send(
        new ApproveHorseCommand(
            id,
            GetUserId(),
            req?.Reason),
        ct));

    [HttpPost("horses/{id:int}/reject")]
    public async Task<IActionResult> RejectHorse(int id, [FromBody] RejectHorseRequest req, CancellationToken ct)
        => Ok(await _sender.Send(new RejectHorseCommand(
    id,
    GetUserId(),
    req.Reason), ct));

    // Body là tùy chọn: FE gọi revoke không kèm reason (không có ô nhập lý do),
    // nên request đến không có Content-Type → phải cho phép empty body, nếu không sẽ 415.
    [HttpPost("horses/{id:int}/revoke")]
    public async Task<IActionResult> RevokeHorse(
    int id, [FromBody] RevokeHorseRequest? req, CancellationToken ct)
    => Ok(await _sender.Send(new RevokeHorseCommand(id, GetUserId(), req?.Reason), ct));

    // =========================
    // ENTRIES
    // =========================

    [HttpGet("entries/pending")]
    public async Task<IActionResult> GetPendingEntries(CancellationToken ct)
        => Ok(await _sender.Send(new GetPendingEntriesQuery(), ct));

    [HttpPost("entries/{id:int}/approve")]
    public async Task<IActionResult> ApproveEntry(
       int id,
       [FromBody] ApproveEntryRequest? req,
       CancellationToken ct)
       => Ok(await _sender.Send(
           new ApproveEntryCommand(
               id,
               GetUserId(),
               req?.Reason),
           ct));

    [HttpPost("entries/{id:int}/reject")]
    public async Task<IActionResult> RejectEntry(int id, [FromBody] RejectEntryRequest req, CancellationToken ct)
        => Ok(await _sender.Send(new RejectEntryCommand(
    id,
    GetUserId(),
    req.Reason), ct));

    // =========================
    // VIOLATIONS (review)
    // =========================

    [HttpGet("violations")]
    public async Task<IActionResult> GetViolations(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 15,
        [FromQuery] string? search = null, [FromQuery] string? sort = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken ct = default)
        => Ok(await _sender.Send(
            new GetAdminViolationsQuery(status, page, pageSize, search, sort, sortDirection), ct));

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

    // Xem ví + giao dịch gần nhất của 1 khán giả.
    [HttpGet("points/{userId:int}")]
    public async Task<IActionResult> GetUserWallet(int userId, CancellationToken ct)
    {
        var wallet = await _sender.Send(new GetUserWalletQuery(userId), ct);
        return wallet is null ? NotFound() : Ok(wallet);
    }

    // Đặt thẳng số dư ví về một giá trị chính xác (buff điểm phục vụ test).
    [HttpPut("points/{userId:int}")]
    public async Task<IActionResult> SetPointBalance(
        int userId, [FromBody] SetPointBalanceRequest req, CancellationToken ct)
        => Ok(await _sender.Send(
            new SetPointBalanceCommand(userId, req.Balance, req.Reason, GetUserId()), ct));

    // Kích hoạt thủ công top-up tuần (tiện test; thực tế chạy tự động qua background service).
    [HttpPost("points/weekly-topup")]
    public async Task<IActionResult> RunWeeklyTopUp(CancellationToken ct)
        => Ok(await _sender.Send(new RunWeeklyTopUpCommand(), ct));
    [HttpPost("points/daily-topup")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RunDailyTopUp(CancellationToken ct)
        => Ok(await _sender.Send(new RunDailyTopUpCommand(), ct));

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
    // REVIEW HISTORY
    // =========================
    [HttpGet("review-history")]
    public async Task<IActionResult> GetReviewHistory(
    [FromQuery] ReviewEntity? entity,
    [FromQuery] int? entityId,
    CancellationToken ct)
    {
        return Ok(await _sender.Send(
            new GetReviewHistoryQuery(entity, entityId),
            ct));
    }

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
public sealed record RevokeHorseRequest(string? Reason);
public sealed record ApproveEntryRequest(string? Reason);
public sealed record ApproveHorseRequest(string? Reason);
public sealed record ApproveUserRequest(string? Reason);
public sealed record RejectUserRequest(string? Reason);
public sealed record RejectHorseRequest(string? Reason);
public sealed record RejectEntryRequest(string? Reason);
public sealed record AdjustPointsRequest(int UserId, decimal Amount, string Type, string? Reason);
public sealed record SetPointBalanceRequest(decimal Balance, string? Reason);
public sealed record ResolveDiscrepancyRequest(string Resolution, string Action, int AdjustedPointsAwarded);
public sealed record ApproveViolationRequest(string? Penalty, string? AdminNote);
public sealed record RejectViolationRequest(string? Reason);
public sealed record LockUserRequest(string? Reason);
public sealed record RejectRaceRequest(string? Reason);
public sealed record UnpublishRaceRequest(string Reason);