using Application.Common.Interfaces;
using Application.Usecases.Entries.CreateEntry;
using Application.Usecases.Entries.DeleteEntry;
using Application.Usecases.Entries.GetEntryDetail;
using Application.Usecases.Entries.GetEntryList;
using Application.Usecases.Entries.UpdateEntry;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/entries")]
[Authorize]
public sealed class EntriesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public EntriesController(
        ISender sender,
        ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    // Owner nộp Entry — HorseOwnerId lấy từ JWT (không tin body).
    [HttpPost]
    [Authorize(Roles = "HORSE_OWNER")]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateEntryCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            throw new UnauthorizedAccessException("Invalid or missing user.");

        var entryId = await _sender.Send(
            command with { HorseOwnerId = _currentUser.UserId.Value },
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { entryId },
            new { entryId });
    }

    [HttpGet("{entryId:int}")]
    public async Task<ActionResult<EntryDetailResponse>> GetById(
        int entryId,
        CancellationToken cancellationToken)
    {
        var entry = await _sender.Send(
            new GetEntryDetailQuery(entryId),
            cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        return Ok(entry);
    }

    // HORSE_OWNER chỉ thấy entry của mình; referee/spectator/admin thấy tất cả.
    [HttpGet]
    public async Task<ActionResult<List<EntryListItemResponse>>> GetAll(
        [FromQuery] int? raceId,
        CancellationToken cancellationToken)
    {
        int? ownerScope = null;

        if (_currentUser.IsInRole("HORSE_OWNER"))
        {
            ownerScope = _currentUser.UserId;
        }

        var entries = await _sender.Send(
            new GetEntryListQuery(raceId, ownerScope),
            cancellationToken);

        return Ok(entries);
    }

    [HttpPut("{entryId:int}")]
    public async Task<ActionResult> Update(
        int entryId,
        UpdateEntryCommand command,
        CancellationToken cancellationToken)
    {
        if (entryId != command.EntryId)
        {
            return BadRequest();
        }

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{entryId:int}")]
    public async Task<ActionResult> Delete(
        int entryId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteEntryCommand(entryId),
            cancellationToken);

        return Ok(result);
    }
}