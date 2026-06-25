using Application.Usecases.PrizePointTransactions.CreatePrizePointTransaction;
using Application.Usecases.PrizePointTransactions.DeletePrizePointTransaction;
using Application.Usecases.PrizePointTransactions.GetPrizePointTransactionDetail;
using Application.Usecases.PrizePointTransactions.GetPrizePointTransactionList;
using Application.Usecases.PrizePointTransactions.UpdatePrizePointTransaction;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers;

[ApiController]
[Route("api/prize-point-transactions")]
[Authorize(Roles = "ADMIN")]
public sealed class PrizePointTransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public PrizePointTransactionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreatePrizePointTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { prizePointTransactionId = id },
            new { prizePointTransactionId = id });
    }

    [HttpGet("{prizePointTransactionId:int}")]
    public async Task<ActionResult<PrizePointTransactionDetailResponse>> GetById(
        [FromRoute] int prizePointTransactionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetPrizePointTransactionDetailQuery(prizePointTransactionId),
            cancellationToken);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Prize point transaction not found"
            });
        }

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<PrizePointTransactionListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetPrizePointTransactionListQuery(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{prizePointTransactionId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int prizePointTransactionId,
        [FromBody] UpdatePrizePointTransactionCommand command,
        CancellationToken cancellationToken)
    {
        if (prizePointTransactionId != command.PrizePointTransactionId)
        {
            return BadRequest(new
            {
                message = "PrizePointTransactionId mismatch"
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

    [HttpDelete("{prizePointTransactionId:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int prizePointTransactionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeletePrizePointTransactionCommand(
                prizePointTransactionId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}