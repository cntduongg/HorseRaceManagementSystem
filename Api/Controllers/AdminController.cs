using Application.Usecases.Admin.ApproveUser;
using Application.Usecases.Admin.GetPendingUsers;
using Application.Usecases.Admin.RejectUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public sealed class AdminController : ControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// List all user accounts that are pending admin approval (Horse Owner and Jockey registrations).
    /// </summary>
    [HttpGet("users/pending")]
    [ProducesResponseType(typeof(List<PendingUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<PendingUserResponse>>> GetPendingUsers(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPendingUsersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Approve a pending user registration. Sets their status to Active so they can log in.
    /// </summary>
    [HttpPost("users/{id:int}/approve")]
    [ProducesResponseType(typeof(ApproveUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApproveUserResponse>> ApproveUser(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ApproveUserCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Reject a pending user registration. Optionally provide a reason.
    /// </summary>
    [HttpPost("users/{id:int}/reject")]
    [ProducesResponseType(typeof(RejectUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RejectUserResponse>> RejectUser(
        int id,
        [FromBody] RejectUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RejectUserCommand(id, request.Reason), cancellationToken);
        return Ok(result);
    }
}

public sealed record RejectUserRequest(string? Reason);
