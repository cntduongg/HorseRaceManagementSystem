using Application.Usecases.Violations.CreateViolation;
using Application.Usecases.Violations.DeleteViolation;
using Application.Usecases.Violations.GetViolationDetail;
using Application.Usecases.Violations.GetViolationList;
using Application.Usecases.Violations.UpdateViolation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers;

[ApiController]
[Route("api/violations")]
[Authorize(Roles = "REFEREE,ADMIN")]
public sealed class ViolationsController : ControllerBase
{
	private readonly ISender _sender;

	public ViolationsController(ISender sender)
	{
		_sender = sender;
	}

	[HttpPost]
	public async Task<ActionResult<int>> Create(
		[FromBody] CreateViolationCommand command,
		CancellationToken cancellationToken)
	{
		var violationId = await _sender.Send(
			command,
			cancellationToken);

		return CreatedAtAction(
			nameof(GetById),
			new { violationId },
			new { violationId });
	}

	[HttpGet("{violationId:int}")]
	public async Task<ActionResult<ViolationDetailResponse>> GetById(
		[FromRoute] int violationId,
		CancellationToken cancellationToken)
	{
		var violation = await _sender.Send(
			new GetViolationDetailQuery(
				violationId),
			cancellationToken);

		if (violation is null)
		{
			return NotFound(new
			{
				message = "Violation not found"
			});
		}

		return Ok(violation);
	}

	[HttpGet]
	public async Task<ActionResult<List<ViolationListItemResponse>>> GetAll(
		CancellationToken cancellationToken)
	{
		var violations = await _sender.Send(
			new GetViolationListQuery(),
			cancellationToken);

		return Ok(violations);
	}

	[HttpPut("{violationId:int}")]
	public async Task<ActionResult> Update(
		[FromRoute] int violationId,
		[FromBody] UpdateViolationCommand command,
		CancellationToken cancellationToken)
	{
		if (violationId != command.ViolationId)
		{
			return BadRequest(new
			{
				message = "ViolationId mismatch"
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

	[HttpDelete("{violationId:int}")]
	public async Task<ActionResult> Delete(
		[FromRoute] int violationId,
		CancellationToken cancellationToken)
	{
		var result = await _sender.Send(
			new DeleteViolationCommand(
				violationId),
			cancellationToken);

		return Ok(new
		{
			success = result
		});
	}
}