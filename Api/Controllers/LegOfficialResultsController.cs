using Application.Usecases.LegOfficialResults.CreateLegOfficialResult;
using Application.Usecases.LegOfficialResults.DeleteLegOfficialResult;
using Application.Usecases.LegOfficialResults.GetLegOfficialResultDetail;
using Application.Usecases.LegOfficialResults.GetLegOfficialResultList;
using Application.Usecases.LegOfficialResults.UpdateLegOfficialResult;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/leg-official-results")]
[Authorize(Roles = "REFEREE,ADMIN")]
public sealed class LegOfficialResultsController : ControllerBase
{
	private readonly ISender _sender;

	public LegOfficialResultsController(ISender sender)
	{
		_sender = sender;
	}

	[HttpPost]
	public async Task<ActionResult> Create(
		[FromBody] CreateLegOfficialResultCommand command,
		CancellationToken cancellationToken)
	{
		var result = await _sender.Send(
			command,
			cancellationToken);

		return Ok(new
		{
			success = result
		});
	}

	[HttpGet("{raceId:int}/{legNumber:int}/{entryId:int}")]
	public async Task<ActionResult<LegOfficialResultDetailResponse>> GetById(
		[FromRoute] int raceId,
		[FromRoute] int legNumber,
		[FromRoute] int entryId,
		CancellationToken cancellationToken)
	{
		var item = await _sender.Send(
			new GetLegOfficialResultDetailQuery(
				raceId,
				legNumber,
				entryId),
			cancellationToken);

		if (item is null)
		{
			return NotFound(new
			{
				message = "Leg official result not found"
			});
		}

		return Ok(item);
	}

	[HttpGet]
	public async Task<ActionResult<List<LegOfficialResultListItemResponse>>> GetAll(
		CancellationToken cancellationToken)
	{
		var items = await _sender.Send(
			new GetLegOfficialResultListQuery(),
			cancellationToken);

		return Ok(items);
	}

	[HttpPut("{raceId:int}/{legNumber:int}/{entryId:int}")]
	public async Task<ActionResult> Update(
		[FromRoute] int raceId,
		[FromRoute] int legNumber,
		[FromRoute] int entryId,
		[FromBody] UpdateLegOfficialResultCommand command,
		CancellationToken cancellationToken)
	{
		if (raceId != command.RaceId ||
			legNumber != command.LegNumber ||
			entryId != command.EntryId)
		{
			return BadRequest(new
			{
				message = "Composite key mismatch"
			});
		}

		var result = await _sender.Send(
			command,
			cancellationToken);

		return Ok(new
		{
			success = result
		});
	}

	[HttpDelete("{raceId:int}/{legNumber:int}/{entryId:int}")]
	public async Task<ActionResult> Delete(
		[FromRoute] int raceId,
		[FromRoute] int legNumber,
		[FromRoute] int entryId,
		CancellationToken cancellationToken)
	{
		var result = await _sender.Send(
			new DeleteLegOfficialResultCommand(
				raceId,
				legNumber,
				entryId),
			cancellationToken);

		return Ok(new
		{
			success = result
		});
	}
}