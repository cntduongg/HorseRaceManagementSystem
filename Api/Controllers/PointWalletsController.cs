using Application.Usecases.PointWallets.CreatePointWallet;
using Application.Usecases.PointWallets.DeletePointWallet;
using Application.Usecases.PointWallets.GetPointWalletDetail;
using Application.Usecases.PointWallets.GetPointWalletList;
using Application.Usecases.PointWallets.UpdatePointWallet;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/point-wallets")]
public sealed class PointWalletsController : ControllerBase
{
    private readonly ISender _sender;

    public PointWalletsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
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
            new GetPointWalletDetailQuery(walletId),
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

    [HttpGet]
    public async Task<ActionResult<List<PointWalletListItemResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var wallets = await _sender.Send(
            new GetPointWalletListQuery(),
            cancellationToken);

        return Ok(wallets);
    }

    [HttpPut("{walletId:int}")]
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
}