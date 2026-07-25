using Application.Usecases.PointWallets.CreatePointWallet;
using Application.Usecases.PointWallets.DeletePointWallet;
using Application.Usecases.PointWallets.GetPointWalletDetail;
using Application.Usecases.PointWallets.GetPointWalletList;
using Application.Usecases.PointWallets.UpdatePointWallet;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/point-wallets")]
[Authorize]
public sealed class PointWalletsController : ControllerBase
{
    private readonly ISender _sender;

    public PointWalletsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<int>> Create(
        [FromBody] CreatePointWalletCommand command,
        CancellationToken cancellationToken)
    {
        var walletId = await _sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { walletId },
            new { walletId });
    }

    [HttpGet("{walletId:int}")]
    public async Task<ActionResult<PointWalletDetailResponse>> GetById(
        [FromRoute] int walletId,
        CancellationToken cancellationToken)
    {
        var wallet = await _sender.Send(
            new GetPointWalletDetailQuery(walletId, GetViewerScope()),
            cancellationToken);

        if (wallet is null)
        {
            return NotFound(new
            {
                message = "Point wallet not found"
            });
        }

        return Ok(wallet);
    }

    // Ví **của chính mình** (ADMIN thấy tất cả) — xem GetViewerScope().
    [HttpGet]
    public async Task<ActionResult<List<PointWalletListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var wallets = await _sender.Send(
            new GetPointWalletListQuery(GetViewerScope()),
            cancellationToken);

        return Ok(wallets);
    }

    [HttpPut("{walletId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Update(
        [FromRoute] int walletId,
        [FromBody] UpdatePointWalletCommand command,
        CancellationToken cancellationToken)
    {
        if (walletId != command.WalletId)
        {
            return BadRequest(new
            {
                message = "WalletId mismatch"
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

    [HttpDelete("{walletId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Delete(
        [FromRoute] int walletId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeletePointWalletCommand(walletId),
            cancellationToken);

        return Ok(new
        {
            success = result
        });
    }

    // Phạm vi dữ liệu được đọc: ADMIN → null (không lọc); role khác → chỉ ví của chính mình.
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