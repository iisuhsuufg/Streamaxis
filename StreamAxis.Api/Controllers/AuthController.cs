using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StreamAxis.Api.Models;
using StreamAxis.Api.Services;
using StreamAxis.Shared;

namespace StreamAxis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { error = "Username and DeviceId are required." });

        var result = await _auth.LoginAsync(
            request.Username.Trim(),
            request.Password ?? "",
            request.DeviceId.Trim(),
            string.IsNullOrWhiteSpace(request.DeviceName) ? "Fire TV" : request.DeviceName.Trim());

        if (!result.Success)
            return Unauthorized(new { error = result.ErrorMessage });

        return Ok(new LoginResponse
        {
            SessionToken = result.Token!,
            UserProfile = new UserProfileDto
            {
                UserId = result.User!.Id,
                Username = result.User.Username,
                MaxDevices = result.User.MaxDevices,
                ExpirationDate = result.User.ExpirationDate
            }
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var token = GetBearerToken();
        if (!string.IsNullOrEmpty(token))
            await _auth.InvalidateSessionAsync(token);
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var token = GetBearerToken();
        if (string.IsNullOrEmpty(token))
            return Unauthorized();
        var session = await _auth.GetSessionAsync(token);
        if (session == null)
            return Unauthorized();
        return Ok(new UserProfileDto
        {
            UserId = session.User.Id,
            Username = session.User.Username,
            MaxDevices = session.User.MaxDevices,
            ExpirationDate = session.User.ExpirationDate
        });
    }

    private string? GetBearerToken()
    {
        var auth = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        return auth["Bearer ".Length..].Trim();
    }
}
