using Application.Usecases.LegRefereeEntries.CreateLegRefereeEntry;
using Application.Usecases.LegRefereeEntries.DeleteLegRefereeEntry;
using Application.Usecases.LegRefereeEntries.GetLegRefereeEntryDetail;
using Application.Usecases.LegRefereeEntries.GetLegRefereeEntryList;
using Application.Usecases.LegRefereeEntries.UpdateLegRefereeEntry;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/leg-referee-entries")]
public sealed class LegRefereeEntriesController : ControllerBase
{
    private readonly ISender _sender;

    public LegRefereeEntriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<long>> Create(
        [FromBody] CreateLegRefereeEntryCommand command,
        CancellationToken cancellationToken)
    {
        var legRefereeEntryId = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { legRefereeEntryId },
            new { legRefereeEntryId });
    }

    [HttpGet("{legRefereeEntryId:long}")]
    public async Task<ActionResult<LegRefereeEntryDetailResponse>> GetById(
        [FromRoute] long legRefereeEntryId,
        CancellationToken cancellationToken)
    {
        var item = await _sender.Send(
            new GetLegRefereeEntryDetailQuery(legRefereeEntryId),
            cancellationToken);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Leg referee entry not found"
            });
        }

        return Ok(item);
    }

    [HttpGet]
    public async Task<ActionResult<List<LegRefereeEntryListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var items = await _sender.Send(
            new GetLegRefereeEntryListQuery(),
            cancellationToken);

        return Ok(items);
    }

    [HttpPut("{legRefereeEntryId:long}")]
    public async Task<ActionResult> Update(
        [FromRoute] long legRefereeEntryId,
        [FromBody] UpdateLegRefereeEntryCommand command,
        CancellationToken cancellationToken)
    {
        if (legRefereeEntryId != command.LegRefereeEntryId)
        {
            return BadRequest(new
            {
                message = "LegRefereeEntryId mismatch"
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

    [HttpDelete("{legRefereeEntryId:long}")]
    public async Task<ActionResult> Delete(
        [FromRoute] long legRefereeEntryId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteLegRefereeEntryCommand(
                legRefereeEntryId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}