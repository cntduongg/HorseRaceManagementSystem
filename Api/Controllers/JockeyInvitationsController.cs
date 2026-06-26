using Application.Usecases.JockeyInvitations.CreateJockeyInvitation;
using Application.Usecases.JockeyInvitations.DeleteJockeyInvitation;
using Application.Usecases.JockeyInvitations.GetJockeyInvitationDetail;
using Application.Usecases.JockeyInvitations.GetJockeyInvitationList;
using Application.Usecases.JockeyInvitations.UpdateJockeyInvitation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/jockey-invitations")]
[Authorize]
public sealed class JockeyInvitationsController : ControllerBase
{
    private readonly ISender _sender;

    public JockeyInvitationsController(ISender sender)
    {
        _sender = sender;
    }

    // Owner mời nài — HorseOwnerId lấy từ JWT (không tin body).
    [HttpPost]
    [Authorize(Roles = "HORSE_OWNER")]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateJockeyInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var invitationId = await _sender.Send(
            command with { HorseOwnerId = GetUserId() }, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { invitationId },
            new { invitationId });
    }

    [HttpGet("{invitationId:int}")]
    public async Task<ActionResult<JockeyInvitationDetailResponse>> GetById(
        [FromRoute] int invitationId,
        CancellationToken cancellationToken)
    {
        var invitation = await _sender.Send(
            new GetJockeyInvitationDetailQuery(invitationId),
            cancellationToken);

        if (invitation is null)
            return NotFound(new { message = "Invitation not found" });

        return Ok(invitation);
    }

    // Scope theo role: jockey thấy nhận, owner thấy gửi, admin thấy tất cả.
    [HttpGet]
    public async Task<ActionResult<List<JockeyInvitationListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var invitations = await _sender.Send(
            new GetJockeyInvitationListQuery(GetUserId(), GetRole()),
            cancellationToken);

        return Ok(invitations);
    }

    // Accept/Decline (jockey) hoặc Confirm/Cancel (owner). Định danh lấy từ JWT.
    [HttpPut("{invitationId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int invitationId,
        [FromBody] UpdateInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateJockeyInvitationCommand(
                invitationId, request.Status, request.ResponseReason, GetUserId()),
            cancellationToken);

        return Ok(new { success = result });
    }

    [HttpDelete("{invitationId:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int invitationId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteJockeyInvitationCommand(invitationId),
            cancellationToken);

        return Ok(new { success = result });
    }

    private int GetUserId()
    {
        var claim =
            User.FindFirst("userId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing userId claim");
        return userId;
    }

    private string? GetRole()
    {
        if (User.IsInRole("ADMIN")) return "ADMIN";
        if (User.IsInRole("JOCKEY")) return "JOCKEY";
        if (User.IsInRole("HORSE_OWNER")) return "HORSE_OWNER";
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
}

public sealed record UpdateInvitationRequest(string Status, string? ResponseReason);
