using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Services;
using StreamAxis.Shared;

namespace StreamAxis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _auth;

    public ContentController(AppDbContext db, IAuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContentItemDto>>> GetContent([FromQuery] ContentCategory? category)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var query = _db.Contents.Where(c => c.IsActive).AsQueryable();
        if (category.HasValue)
            query = query.Where(c => c.Category == category.Value);

        var list = await query
            .Select(c => new ContentItemDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                PosterUrl = c.PosterUrl,
                StreamUrl = c.StreamUrl,
                Category = c.Category,
                ResumePositionTicks = _db.UserPlaybackStates
                    .Where(s => s.UserId == userId && s.ContentId == c.Id)
                    .Select(s => (long?)s.LastPositionTicks)
                    .FirstOrDefault()
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("continue-watching")]
    public async Task<ActionResult<IEnumerable<ContentItemDto>>> GetContinueWatching()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var list = await _db.UserPlaybackStates
            .Where(s => s.UserId == userId && s.LastPositionTicks > 0)
            .OrderByDescending(s => s.LastUpdated)
            .Take(20)
            .Select(s => new ContentItemDto
            {
                Id = s.Content.Id,
                Title = s.Content.Title,
                Description = s.Content.Description,
                PosterUrl = s.Content.PosterUrl,
                StreamUrl = s.Content.StreamUrl,
                Category = s.Content.Category,
                ResumePositionTicks = s.LastPositionTicks
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("{id:int}/resume")]
    public async Task<ActionResult<long>> GetResumePosition(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var state = await _db.UserPlaybackStates
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ContentId == id);
        return Ok(state?.LastPositionTicks ?? 0L);
    }

    [HttpPost("{id:int}/progress")]
    public async Task<IActionResult> UpdateProgress(int id, [FromBody] ProgressRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var state = await _db.UserPlaybackStates
            .FirstOrDefaultAsync(s => s.UserId == userId && s.ContentId == id);
        if (state == null)
        {
            _db.UserPlaybackStates.Add(new Entities.UserPlaybackState
            {
                UserId = userId.Value,
                ContentId = id,
                LastPositionTicks = request.PositionTicks,
                LastUpdated = DateTime.UtcNow
            });
        }
        else
        {
            state.LastPositionTicks = request.PositionTicks;
            state.LastUpdated = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return Ok();
    }

    private int? GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var uid) ? uid : null;
    }
}

public class ProgressRequest
{
    public long PositionTicks { get; set; }
}
