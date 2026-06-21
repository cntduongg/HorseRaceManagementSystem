using Application.Usecases.Users.CreateUser;
using Application.Usecases.Users.DeleteUser;
using Application.Usecases.Users.GetUserDetail;
using Application.Usecases.Users.GetUserList;
using Application.Usecases.Users.UpdateUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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