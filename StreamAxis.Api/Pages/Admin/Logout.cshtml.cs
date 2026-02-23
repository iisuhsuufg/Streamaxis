using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StreamAxis.Api.Pages.Admin;

public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        Response.Cookies.Delete("AdminAuth", new CookieOptions { Path = "/" });
        return RedirectToPage("/Admin/Login");
    }
}
