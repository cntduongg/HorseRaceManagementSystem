using Application.Usecases.Jockeys.SearchJockeys;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/jockeys")]
[Authorize]
public sealed class JockeysController : ControllerBase
{
    private readonly ISender _sender;

    public JockeysController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<SearchJockeyResponse>>> Search(
        [FromQuery] string? keyword,
        [FromQuery] int? minTotalRaces,
        [FromQuery] int? minWins,
        [FromQuery] int? minTop3,
        [FromQuery] int? minPrizePoints,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SearchJockeysQuery(
                keyword,
                minTotalRaces,
                minWins,
                minTop3,
                minPrizePoints),
            cancellationToken);

        return Ok(result);
    }
}