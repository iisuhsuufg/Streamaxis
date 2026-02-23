using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Shared;

namespace StreamAxis.Api.Controllers;

[ApiController]
[Route("api/app-version")]
public class AppVersionController : ControllerBase
{
    private readonly AppDbContext _db;

    public AppVersionController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<AppVersionDto>> GetVersion()
    {
        var c = await _db.AppConfigs.FirstOrDefaultAsync();
        if (c == null)
            return Ok(new AppVersionDto { CurrentVersion = "1.0", IsUpdateRequired = false });
        return Ok(new AppVersionDto
        {
            CurrentVersion = c.CurrentVersion,
            LatestApkUrl = c.LatestApkUrl,
            IsUpdateRequired = c.IsUpdateRequired,
            UpdateMessage = c.UpdateMessage
        });
    }
}
