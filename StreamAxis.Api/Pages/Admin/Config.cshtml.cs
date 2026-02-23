using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Entities;

namespace StreamAxis.Api.Pages.Admin;

[IgnoreAntiforgeryToken]
public class ConfigModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ConfigModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public string CurrentVersion { get; set; } = "";
    public string? LatestApkUrl { get; set; }
    public bool IsUpdateRequired { get; set; }
    public string? UpdateMessage { get; set; }
    public string? Message { get; set; }

    public async Task OnGetAsync()
    {
        var c = await _db.AppConfigs.FirstOrDefaultAsync();
        if (c != null)
        {
            CurrentVersion = c.CurrentVersion;
            LatestApkUrl = c.LatestApkUrl;
            IsUpdateRequired = c.IsUpdateRequired;
            UpdateMessage = c.UpdateMessage;
        }
        else
            CurrentVersion = "1.0";
    }

    public async Task<IActionResult> OnPostSaveAsync(string? currentVersion, string? latestApkUrl, bool? isUpdateRequired, string? updateMessage)
    {
        var c = await _db.AppConfigs.FirstOrDefaultAsync();
        if (c == null)
        {
            c = new AppConfig { Id = 1, UpdatedAt = DateTime.UtcNow };
            _db.AppConfigs.Add(c);
        }
        c.CurrentVersion = currentVersion ?? "1.0";
        c.LatestApkUrl = string.IsNullOrWhiteSpace(latestApkUrl) ? null : latestApkUrl.Trim();
        c.IsUpdateRequired = isUpdateRequired == true;
        c.UpdateMessage = string.IsNullOrWhiteSpace(updateMessage) ? null : updateMessage.Trim();
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        Message = "Saved.";
        CurrentVersion = c.CurrentVersion;
        LatestApkUrl = c.LatestApkUrl;
        IsUpdateRequired = c.IsUpdateRequired;
        UpdateMessage = c.UpdateMessage;
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) { Message = "No file selected."; await OnGetAsync(); return Page(); }
        var apksDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "apks");
        Directory.CreateDirectory(apksDir);
        var name = Path.GetFileName(file.FileName);
        if (string.IsNullOrEmpty(name) || !name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            name = "app-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".apk";
        var path = Path.Combine(apksDir, name);
        await using (var stream = System.IO.File.Create(path))
            await file.CopyToAsync(stream);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/apks/{name}";
        var cfg = await _db.AppConfigs.FirstOrDefaultAsync();
        if (cfg != null)
        {
            cfg.LatestApkUrl = url;
            cfg.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        Message = "Uploaded. Latest APK URL set to " + url;
        await OnGetAsync();
        return Page();
    }
}
