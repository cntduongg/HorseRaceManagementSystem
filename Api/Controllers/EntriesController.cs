using Application.Usecases.Entries.CreateEntry;
using Application.Usecases.Entries.DeleteEntry;
using Application.Usecases.Entries.GetEntryDetail;
using Application.Usecases.Entries.GetEntryList;
using Application.Usecases.Entries.UpdateEntry;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/entries")]
public sealed class EntriesController : ControllerBase
{
	private readonly ISender _sender;

	public EntriesController(ISender sender)
	{
		_sender = sender;
	}

	[HttpPost]
	public async Task<ActionResult<int>> Create(
		[FromBody] CreateEntryCommand command,
		CancellationToken cancellationToken)
	{
		var entryId = await _sender.Send(command, cancellationToken);

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

	[HttpGet]
	public async Task<ActionResult<List<EntryListItemResponse>>> GetAll(
		CancellationToken cancellationToken)
	{
		var entries = await _sender.Send(
			new GetEntryListQuery(),
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