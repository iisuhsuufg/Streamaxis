using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Services;

namespace StreamAxis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlayController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthService _auth;

    public PlayController(AppDbContext db, IAuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpPost("{contentId:int}")]
    public async Task<ActionResult<PlayResponse>> Play(int contentId)
    {
        var token = Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "").Trim();
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var session = await _auth.GetSessionAsync(token);
        if (session == null) return Unauthorized();

        var content = await _db.Contents.FirstOrDefaultAsync(c => c.Id == contentId && c.IsActive);
        if (content == null) return NotFound();

        return Ok(new PlayResponse
        {
            StreamUrl = content.StreamUrl ?? "",
            Title = content.Title
        });
    }
}

public class PlayResponse
{
    public string StreamUrl { get; set; } = "";
    public string Title { get; set; } = "";
}
