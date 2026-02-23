using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Entities;

namespace StreamAxis.Api.Pages.Admin;

[IgnoreAntiforgeryToken]
public class UsersModel : PageModel
{
    private readonly AppDbContext _db;

    public UsersModel(AppDbContext db)
    {
        _db = db;
    }

    public string? Query { get; set; }
    public string? Message { get; set; }
    public List<UserRow> Users { get; set; } = new();

    public class UserRow
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public bool IsActive { get; set; }
        public int MaxDevices { get; set; }
        public DateTime ExpirationDate { get; set; }
    }

    public async Task OnGetAsync(string? q, string? message)
    {
        Query = q;
        Message = message;
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u => u.Username.Contains(q));
        Users = await query
            .OrderBy(u => u.Username)
            .Select(u => new UserRow
            {
                Id = u.Id,
                Username = u.Username,
                IsActive = u.IsActive,
                MaxDevices = u.MaxDevices,
                ExpirationDate = u.ExpirationDate
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string username, string password, int maxDevices, DateTime expirationDate)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return RedirectToPage(new { message = "Username and password are required." });
        }

        var exists = await _db.Users.AnyAsync(u => u.Username == username);
        if (exists)
        {
            return RedirectToPage(new { message = "Username already exists." });
        }

        var user = new User
        {
            Username = username.Trim(),
            Password = password,
            MaxDevices = Math.Clamp(maxDevices, 1, 99),
            ExpirationDate = expirationDate.ToUniversalTime(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return RedirectToPage(new { message = $"User '{username}' created successfully!" });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null && user.Username.ToLower() != "admin")
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return RedirectToPage(new { message = $"User deleted." });
        }
        return RedirectToPage(new { message = "Cannot delete admin user." });
    }
}
