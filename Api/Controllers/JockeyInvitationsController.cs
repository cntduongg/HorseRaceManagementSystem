using Application.Usecases.JockeyInvitations.CreateJockeyInvitation;
using Application.Usecases.JockeyInvitations.DeleteJockeyInvitation;
using Application.Usecases.JockeyInvitations.GetJockeyInvitationDetail;
using Application.Usecases.JockeyInvitations.GetJockeyInvitationList;
using Application.Usecases.JockeyInvitations.UpdateJockeyInvitation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/jockey-invitations")]
[Authorize(Roles = "HORSE_OWNER,JOCKEY,ADMIN")]
public sealed class JockeyInvitationsController : ControllerBase
{
    private readonly ISender _sender;

    public JockeyInvitationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateJockeyInvitationCommand command,
        CancellationToken cancellationToken)
    {
        var invitationId =
            await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { invitationId },
            new { invitationId });
    }

    [HttpGet("{invitationId:int}")]
    public async Task<ActionResult<JockeyInvitationDetailResponse>>
        GetById(
            [FromRoute] int invitationId,
            CancellationToken cancellationToken)
    {
        var invitation = await _sender.Send(
            new GetJockeyInvitationDetailQuery(invitationId),
            cancellationToken);

        if (invitation is null)
        {
            return NotFound(new
            {
                message = "Invitation not found"
            });
        }

        return Ok(invitation);
    }

    [HttpGet]
    public async Task<ActionResult<List<JockeyInvitationListItemResponse>>>
        GetAll(
            CancellationToken cancellationToken)
    {
        var invitations =
            await _sender.Send(
                new GetJockeyInvitationListQuery(),
                cancellationToken);

        return Ok(invitations);
    }

    [HttpPut("{invitationId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int invitationId,
        [FromBody] UpdateJockeyInvitationCommand command,
        CancellationToken cancellationToken)
    {
        if (invitationId != command.InvitationId)
        {
            return BadRequest(new
            {
                message = "InvitationId mismatch"
            });
        }

        var result =
            await _sender.Send(command, cancellationToken);

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
}