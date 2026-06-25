using Application.Usecases.Users.CreateUser;
using Application.Usecases.Users.DeleteUser;
using Application.Usecases.Users.GetUserDetail;
using Application.Usecases.Users.GetUserList;
using Application.Usecases.Users.UpdateUser;
using Application.Usecases.Users.ChangePassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Đổi mật khẩu của chính user đang đăng nhập (resolve UserId từ JWT — bỏ qua route id).
    /// </summary>
    [HttpPut("{userId:int}/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromRoute] int userId,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var claim =
            User.FindFirst("userId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claim, out var currentUserId))
            throw new UnauthorizedAccessException("Invalid or missing userId claim");

        var result = await _sender.Send(
            new ChangePasswordCommand(currentUserId, request.CurrentPassword, request.NewPassword),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var userId = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { userId },
            new { userId });
    }

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<UserDetailResponse>> GetById(
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        var user = await _sender.Send(
            new GetUserDetailQuery(userId),
            cancellationToken);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found"
            });
        }

        return Ok(user);
    }

    [HttpGet]
    public async Task<ActionResult<List<UserListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var users = await _sender.Send(
            new GetUserListQuery(),
            cancellationToken);

        return Ok(users);
    }

    [HttpPut("{userId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int userId,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (userId != command.UserId)
        {
            return BadRequest(new
            {
                message = "UserId mismatch"
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

    [HttpDelete("{userId:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteUserCommand(userId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);