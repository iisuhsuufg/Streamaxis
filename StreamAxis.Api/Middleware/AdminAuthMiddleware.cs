namespace StreamAxis.Api.Middleware;

public class AdminAuthMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly PathString LoginPath = new("/admin/login");
    private static readonly PathString LogoutPath = new("/admin/logout");

    public AdminAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        
        // Skip non-admin paths
        if (!path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }
        
        // Allow login and logout pages without authentication
        if (path.StartsWithSegments(LoginPath, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments(LogoutPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }
        
        // Check if user is authenticated
        if (context.Request.Cookies["AdminAuth"] != "1")
        {
            context.Response.Redirect("/Admin/Login");
            return;
        }
        
        // User is authenticated, continue to the requested page
        await _next(context);
    }
}
