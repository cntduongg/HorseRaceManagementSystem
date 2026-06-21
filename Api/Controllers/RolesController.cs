using Application.Usecases.Roles.CreateRole;
using Application.Usecases.Roles.DeleteRole;
using Application.Usecases.Roles.GetRoleDetail;
using Application.Usecases.Roles.GetRoleList;
using Application.Usecases.Roles.UpdateRole;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly ISender _sender;

    public RolesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var roleId = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { roleId },
            new { roleId });
    }

    [HttpGet("{roleId:int}")]
    public async Task<ActionResult<RoleDetailResponse>> GetById(
        [FromRoute] int roleId,
        CancellationToken cancellationToken)
    {
        var role = await _sender.Send(
            new GetRoleDetailQuery(roleId),
            cancellationToken);

        if (role is null)
        {
            return NotFound(new
            {
                message = "Role not found"
            });
        }

        return Ok(role);
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var roles = await _sender.Send(
            new GetRoleListQuery(),
            cancellationToken);

        return Ok(roles);
    }

    [HttpPut("{roleId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int roleId,
        [FromBody] UpdateRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (roleId != command.RoleId)
        {
            return BadRequest(new
            {
                message = "RoleId mismatch"
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

    [HttpDelete("{roleId:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int roleId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteRoleCommand(roleId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}