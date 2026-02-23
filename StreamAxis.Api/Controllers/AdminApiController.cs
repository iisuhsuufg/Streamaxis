using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamAxis.Api.Services;
using System.Security.Claims;

namespace StreamAxis.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminApiController : ControllerBase
{
    private readonly IContentImportService _import;

    public AdminApiController(IContentImportService import)
    {
        _import = import;
    }

    private bool IsAdmin => string.Equals(User.FindFirstValue(ClaimTypes.Name), "admin", StringComparison.OrdinalIgnoreCase);

    [HttpPost("scrape")]
    public async Task<IActionResult> TriggerScrape([FromQuery] int maxLiveTv = 100, [FromQuery] int maxMovies = 50, [FromQuery] int maxTvShows = 50)
    {
        if (!IsAdmin) return Forbid();
        var result = await _import.ImportFromScrapersAsync(maxLiveTv, maxMovies, maxTvShows);
        if (result.Error != null)
            return StatusCode(500, new { error = result.Error, result });
        return Ok(result);
    }
}
