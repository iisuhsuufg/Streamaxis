using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StreamAxis.Api.Services;

namespace StreamAxis.Api.Middleware;

public class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SessionAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            if (!string.IsNullOrEmpty(token))
            {
                var session = await authService.GetSessionAsync(token);
                if (session != null)
                {
                    var identity = new ClaimsIdentity("Session");
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, session.User.Id.ToString()));
                    identity.AddClaim(new Claim(ClaimTypes.Name, session.User.Username));
                    identity.AddClaim(new Claim("DeviceId", session.DeviceId));
                    context.User = new ClaimsPrincipal(identity);
                }
            }
        }

        await _next(context);
    }
}
