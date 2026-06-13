using Application.Usecases.WalletTransactions.CreateWalletTransaction;
using Application.Usecases.WalletTransactions.DeleteWalletTransaction;
using Application.Usecases.WalletTransactions.GetWalletTransactionDetail;
using Application.Usecases.WalletTransactions.GetWalletTransactionList;
using Application.Usecases.WalletTransactions.UpdateWalletTransaction;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/wallet-transactions")]
public sealed class WalletTransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public WalletTransactionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreateWalletTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var transactionId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { transactionId },
            new { transactionId });
    }

    [HttpGet("{transactionId:int}")]
    public async Task<ActionResult<WalletTransactionDetailResponse>> GetById(
        [FromRoute] int transactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await _sender.Send(
            new GetWalletTransactionDetailQuery(transactionId),
            cancellationToken);

        if (transaction is null)
        {
            return NotFound(new
            {
                message = "Wallet transaction not found"
            });
        }

        return Ok(transaction);
    }

    [HttpGet]
    public async Task<ActionResult<List<WalletTransactionListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var transactions = await _sender.Send(
            new GetWalletTransactionListQuery(),
            cancellationToken);

        return Ok(transactions);
    }

    [HttpPut("{transactionId:int}")]
    public async Task<ActionResult> Update(
        [FromRoute] int transactionId,
        [FromBody] UpdateWalletTransactionCommand command,
        CancellationToken cancellationToken)
    {
        if (transactionId != command.WalletTransactionId)
        {
            return BadRequest(new
            {
                message = "WalletTransactionId mismatch"
            });
        }

        var result = await _sender.Send(command, cancellationToken);

        return Ok(new
        {
            success = result
        });
    }

    [HttpDelete("{transactionId:int}")]
    public async Task<ActionResult> Delete(
        [FromRoute] int transactionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteWalletTransactionCommand(transactionId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }
}