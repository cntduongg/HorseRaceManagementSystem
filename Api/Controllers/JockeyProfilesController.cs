using Application.Usecases.JockeyProfiles.CreateJockeyProfile;
using Application.Usecases.JockeyProfiles.DeleteJockeyProfile;
using Application.Usecases.JockeyProfiles.GetJockeyProfileDetail;
using Application.Usecases.JockeyProfiles.GetJockeyProfileList;
using Application.Usecases.JockeyProfiles.UpdateJockeyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/jockey-profiles")]
[Authorize]
public sealed class JockeyProfilesController : ControllerBase
{
    private readonly ISender _sender;

    public JockeyProfilesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateJockeyProfileCommand command,
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
    public async Task<ActionResult<JockeyProfileDetailResponse>> GetById(
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        var profile = await _sender.Send(
            new GetJockeyProfileDetailQuery(userId),
            cancellationToken);

        if (profile is null)
        {
            return NotFound(new
            {
                message = "Jockey profile not found"
            });
        }

        return Ok(profile);
    }

    [HttpGet]
    public async Task<ActionResult<List<JockeyProfileListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var profiles = await _sender.Send(
            new GetJockeyProfileListQuery(),
            cancellationToken);

        return Ok(profiles);
    }

    [HttpPut("{userId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int userId,
        [FromBody] UpdateJockeyProfileCommand command,
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
            new DeleteJockeyProfileCommand(userId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}