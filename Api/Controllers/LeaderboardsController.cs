using Application.Usecases.Leaderboards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// Bảng xếp hạng Prize Points (Flow 8) — đọc cho mọi user đã đăng nhập.
[ApiController]
[Route("api/leaderboards")]
[Authorize]
public sealed class LeaderboardsController : ControllerBase
{
    private readonly ISender _sender;

    public LeaderboardsController(ISender sender)
    {
        _sender = sender;
    }

    // Career Leaderboard (toàn giải). ?role=JOCKEY|HORSE_OWNER để lọc.
    [HttpGet("career")]
    public async Task<IActionResult> Career([FromQuery] string? role, CancellationToken ct)
        => Ok(await _sender.Send(new GetLeaderboardQuery(null, role), ct));

    // Tournament Leaderboard.
    [HttpGet("tournament/{tournamentId:int}")]
    public async Task<IActionResult> Tournament(
        int tournamentId, [FromQuery] string? role, CancellationToken ct)
        => Ok(await _sender.Send(new GetLeaderboardQuery(tournamentId, role), ct));
}
