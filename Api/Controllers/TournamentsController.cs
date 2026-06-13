using Application.Usecases.Tournaments.CreateTournament;
using Application.Usecases.Tournaments.DeleteTournament;
using Application.Usecases.Tournaments.GetTournamentDetail;
using Application.Usecases.Tournaments.GetTournamentList;
using Application.Usecases.Tournaments.UpdateTournament;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/tournaments")]
public sealed class TournamentsController : ControllerBase
{
    private readonly ISender _sender;

    public TournamentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateTournamentCommand command,
        CancellationToken cancellationToken)
    {
        var tournamentId = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { tournamentId },
            new { tournamentId });
    }

    [HttpGet]
    public async Task<ActionResult<List<TournamentListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetTournamentListQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{tournamentId:int}")]
    public async Task<ActionResult<TournamentDetailResponse>> GetById(
        [FromRoute] int tournamentId,
        CancellationToken cancellationToken)
    {
        var tournament = await _sender.Send(
            new GetTournamentDetailQuery(tournamentId),
            cancellationToken);

        if (tournament is null)
        {
            return NotFound(new
            {
                message = "Tournament not found",
                tournamentId
            });
        }

        return Ok(tournament);
    }

    [HttpPut("{tournamentId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int tournamentId,
        [FromBody] UpdateTournamentCommand command,
        CancellationToken cancellationToken)
    {
        if (tournamentId != command.TournamentId)
        {
            return BadRequest(new
            {
                message = "Route id and request id do not match."
            });
        }

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (!result)
        {
            return NotFound(new
            {
                message = "Tournament not found",
                tournamentId
            });
        }

        return NoContent();
    }

    [HttpDelete("{tournamentId:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int tournamentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteTournamentCommand(tournamentId),
            cancellationToken);

        if (!result)
        {
            return NotFound(new
            {
                message = "Tournament not found",
                tournamentId
            });
        }

        return NoContent();
    }
}