using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Entities;

namespace StreamAxis.Api.Pages.Admin;

[IgnoreAntiforgeryToken]
public class UserEditModel : PageModel
{
    private readonly AppDbContext _db;

    public UserEditModel(AppDbContext db)
    {
        _db = db;
    }

    public User? User { get; set; }
    public List<DeviceRow> Devices { get; set; } = new();

    public class DeviceRow
    {
        public int Id { get; set; }
        public string DeviceName { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public DateTime LastLoginDate { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        User = await _db.Users.FindAsync(id);
        if (User == null) return NotFound();
        Devices = await _db.Devices
            .Where(d => d.UserId == id)
            .Select(d => new DeviceRow { Id = d.Id, DeviceName = d.DeviceName, DeviceId = d.DeviceId, LastLoginDate = d.LastLoginDate })
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int id, bool? isActive, DateTime expirationDate, int maxDevices)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = isActive == true;
        user.ExpirationDate = expirationDate.ToUniversalTime();
        user.MaxDevices = Math.Clamp(maxDevices, 1, 99);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveDeviceAsync(int userId, int deviceId)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);
        if (device != null)
        {
            _db.Devices.Remove(device);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { id = userId });
    }

    public async Task<IActionResult> OnPostResetDevicesAsync(int userId)
    {
        var devices = await _db.Devices.Where(d => d.UserId == userId).ToListAsync();
        _db.Devices.RemoveRange(devices);
        await _db.SaveChangesAsync();
        return RedirectToPage(new { id = userId });
    }
}
