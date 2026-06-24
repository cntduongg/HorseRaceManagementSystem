using Application.Usecases.SettlementRuns.CreateSettlementRun;
using Application.Usecases.SettlementRuns.DeleteSettlementRun;
using Application.Usecases.SettlementRuns.GetSettlementRunDetail;
using Application.Usecases.SettlementRuns.GetSettlementRunList;
using Application.Usecases.SettlementRuns.UpdateSettlementRun;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/settlement-runs")]
[Authorize(Roles = "ADMIN")]
public sealed class SettlementRunsController : ControllerBase
{
    private readonly ISender _sender;

    public SettlementRunsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateSettlementRunCommand command,
        CancellationToken cancellationToken)
    {
        var settlementRunId = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { settlementRunId },
            new { settlementRunId });
    }

    [HttpGet("{settlementRunId:int}")]
    public async Task<ActionResult<SettlementRunDetailResponse>> GetById(
        [FromRoute] int settlementRunId,
        CancellationToken cancellationToken)
    {
        var settlementRun = await _sender.Send(
            new GetSettlementRunDetailQuery(settlementRunId),
            cancellationToken);

        if (settlementRun is null)
        {
            return NotFound(new
            {
                message = "Settlement run not found"
            });
        }

        return Ok(settlementRun);
    }

    [HttpGet]
    public async Task<ActionResult<List<SettlementRunListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var settlementRuns = await _sender.Send(
            new GetSettlementRunListQuery(),
            cancellationToken);

        return Ok(settlementRuns);
    }

    [HttpPut("{settlementRunId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int settlementRunId,
        [FromBody] UpdateSettlementRunCommand command,
        CancellationToken cancellationToken)
    {
        if (settlementRunId != command.SettlementRunId)
        {
            return BadRequest(new
            {
                message = "SettlementRunId mismatch"
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

    [HttpDelete("{settlementRunId:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int settlementRunId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteSettlementRunCommand(settlementRunId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}