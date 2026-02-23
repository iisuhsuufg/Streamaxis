using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;

namespace StreamAxis.Api.Pages.Admin;

[IgnoreAntiforgeryToken]
public class LoginModel : PageModel
{
    private readonly AppDbContext _db;

    public LoginModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty] public string Username { get; set; } = "";
    public string? Error { get; set; }

    public IActionResult OnGet()
    {
        if (Request.Cookies["AdminAuth"] == "1")
            return RedirectToPage("/Admin/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
        if (user == null || !user.IsActive || !string.Equals(user.Username, "admin", StringComparison.OrdinalIgnoreCase))
        {
            Error = "Invalid credentials.";
            return Page();
        }
        Response.Cookies.Append("AdminAuth", "1", new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Set to false for HTTP connections
            SameSite = SameSiteMode.Lax, // Changed from Strict to Lax
            Expires = DateTimeOffset.UtcNow.AddHours(2),
            Path = "/"
        });
        return RedirectToPage("/Admin/Index");
    }
}
