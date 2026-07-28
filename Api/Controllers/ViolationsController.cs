using Application.Usecases.Violations.CreateViolation;
using Application.Usecases.Violations.DeleteViolation;
using Application.Usecases.Violations.GetViolationDetail;
using Application.Usecases.Violations.GetViolationList;
using Application.Usecases.Violations.UpdateViolation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

	// Referee báo cáo — ReportedByRefereeId lấy từ JWT, Status luôn Pending.
	[HttpPost]
	public async Task<ActionResult<int>> Create(
		[FromBody] CreateViolationCommand command,
		CancellationToken cancellationToken)
	{
		var claim =
			User.FindFirst("userId")?.Value ??
			User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		if (!int.TryParse(claim, out var refereeId))
			throw new UnauthorizedAccessException("Invalid or missing userId claim");

		var violationId = await _sender.Send(
			command with { ReportedByRefereeId = refereeId, Status = "Pending" },
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
				violationId,
				GetViewerScope()),
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

	// Danh sách báo cáo **của chính mình** (ADMIN thấy tất cả) — xem GetViewerScope().
	[HttpGet]
	public async Task<ActionResult<List<ViolationListItemResponse>>> GetAll(
		CancellationToken cancellationToken)
	{
		var violations = await _sender.Send(
			new GetViolationListQuery(GetViewerScope()),
			cancellationToken);

		return Ok(violations);
	}

	// Admin-only: chỉnh sửa vi phạm đã xử lý. ActorAdminId lấy từ JWT, không tin body.
	[HttpPut("{violationId:int}")]
	[Authorize(Roles = "ADMIN")]
	public async Task<ActionResult> Update(
		[FromRoute] int violationId,
		[FromBody] UpdateViolationRequest body,
		CancellationToken cancellationToken)
	{
		if (violationId != body.ViolationId)
		{
			return BadRequest(new
			{
				message = "ViolationId mismatch"
			});
		}

		var result = await _sender.Send(
			new UpdateViolationCommand(
				body.ViolationId,
				body.RaceId,
				body.LegNumber,
				body.EntryId,
				body.ReportedByRefereeId,
				body.ViolationType,
				body.Description,
				body.Penalty,
				body.Status,
				body.AdminNote,
				GetUserId()),
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

	// Phạm vi dữ liệu được đọc: ADMIN → null (không lọc); REFEREE → chỉ báo cáo của chính mình.
	// Lấy từ JWT, không tin body/query. (`ICurrentUser` vẫn đang bị comment — T-07.)
	private int? GetViewerScope()
		=> User.IsInRole("ADMIN") ? null : GetUserId();

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

public sealed record UpdateViolationRequest(
	int ViolationId,
	int RaceId,
	int LegNumber,
	int EntryId,
	int ReportedByRefereeId,
	string ViolationType,
	string? Description,
	string Penalty,
	string Status,
	string? AdminNote);
