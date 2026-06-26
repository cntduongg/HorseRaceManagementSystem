using Application.Usecases.Entries.CreateEntry;
using Application.Usecases.Entries.DeleteEntry;
using Application.Usecases.Entries.GetEntryDetail;
using Application.Usecases.Entries.GetEntryList;
using Application.Usecases.Entries.UpdateEntry;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/entries")]
[Authorize]
public sealed class EntriesController : ControllerBase
{
	private readonly ISender _sender;

	public EntriesController(ISender sender)
	{
		_sender = sender;
	}

	// Owner nộp Entry — HorseOwnerId lấy từ JWT (không tin body).
	[HttpPost]
	[Authorize(Roles = "HORSE_OWNER")]
	public async Task<ActionResult<int>> Create(
		[FromBody] CreateEntryCommand command,
		CancellationToken cancellationToken)
	{
		var claim =
			User.FindFirst("userId")?.Value ??
			User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (!int.TryParse(claim, out var ownerId))
			throw new UnauthorizedAccessException("Invalid or missing userId claim");

		var entryId = await _sender.Send(
			command with { HorseOwnerId = ownerId }, cancellationToken);

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

	// HORSE_OWNER chỉ thấy entry của mình; referee/spectator/admin thấy tất cả. Lọc theo raceId nếu có.
	[HttpGet]
	public async Task<ActionResult<List<EntryListItemResponse>>> GetAll(
		[FromQuery] int? raceId,
		CancellationToken cancellationToken)
	{
		int? ownerScope = null;
		if (User.IsInRole("HORSE_OWNER"))
		{
			var claim =
				User.FindFirst("userId")?.Value ??
				User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (int.TryParse(claim, out var ownerId))
				ownerScope = ownerId;
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