using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Entities;
using StreamAxis.Shared;

namespace StreamAxis.Api.Pages.Admin;

[IgnoreAntiforgeryToken]
public class ContentEditModel : PageModel
{
    private readonly AppDbContext _db;

    public ContentEditModel(AppDbContext db)
    {
        _db = db;
    }

    public int? Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? StreamUrl { get; set; }
    public int Category { get; set; }
    public bool IsActive { get; set; } = true;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Id = id;
        if (!id.HasValue) return Page();
        var c = await _db.Contents.FindAsync(id.Value);
        if (c == null) return NotFound();
        Title = c.Title;
        Description = c.Description;
        PosterUrl = c.PosterUrl;
        StreamUrl = c.StreamUrl;
        Category = (int)c.Category;
        IsActive = c.IsActive;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id, string title, string? description, string? posterUrl, string? streamUrl, int category, bool? isActive)
    {
        Entities.Content? c;
        if (id.HasValue)
        {
            c = await _db.Contents.FindAsync(id.Value);
            if (c == null) return NotFound();
        }
        else
        {
            c = new Entities.Content { CreatedAt = DateTime.UtcNow };
            _db.Contents.Add(c);
        }
        c.Title = title?.Trim() ?? "";
        c.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        c.PosterUrl = string.IsNullOrWhiteSpace(posterUrl) ? null : posterUrl.Trim();
        c.StreamUrl = string.IsNullOrWhiteSpace(streamUrl) ? null : streamUrl.Trim();
        c.Category = (ContentCategory)Math.Clamp(category, 0, 2);
        c.IsActive = isActive == true;
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToPage("/Admin/Content");
    }
}
