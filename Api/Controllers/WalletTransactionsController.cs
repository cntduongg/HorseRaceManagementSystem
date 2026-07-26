using Application.Usecases.WalletTransactions.CreateWalletTransaction;
using Application.Usecases.WalletTransactions.DeleteWalletTransaction;
using Application.Usecases.WalletTransactions.GetWalletTransactionDetail;
using Application.Usecases.WalletTransactions.GetWalletTransactionList;
using Application.Usecases.WalletTransactions.UpdateWalletTransaction;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/wallet-transactions")]
[Authorize]
public sealed class WalletTransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public WalletTransactionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
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
            new GetWalletTransactionDetailQuery(transactionId, GetViewerScope()),
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

    // Giao dịch ví **của chính mình** (ADMIN thấy tất cả) — xem GetViewerScope().
    [HttpGet]
    public async Task<ActionResult<List<WalletTransactionListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var transactions = await _sender.Send(
            new GetWalletTransactionListQuery(GetViewerScope()),
            cancellationToken);

        return Ok(transactions);
    }

    [HttpPut("{transactionId:int}")]
    [Authorize(Roles = "ADMIN")]
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
    [Authorize(Roles = "ADMIN")]
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

    // Phạm vi dữ liệu được đọc: ADMIN → null (không lọc); role khác → chỉ giao dịch của chính mình.
    // (`ICurrentUser` vẫn đang bị comment — T-07 — nên resolve thủ công như các controller khác.)
    private int? GetViewerScope()
    {
        if (User.IsInRole("ADMIN"))
        {
            return null;
        }

        var claim =
            User.FindFirst("userId")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing userId claim");
        }

        return userId;
    }
}