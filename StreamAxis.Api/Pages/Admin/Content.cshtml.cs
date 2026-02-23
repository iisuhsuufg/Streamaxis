using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Services;
using StreamAxis.Shared;

namespace StreamAxis.Api.Pages.Admin;

[IgnoreAntiforgeryToken]
public class ContentModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IContentImportService _import;

    public ContentModel(AppDbContext db, IContentImportService import)
    {
        _db = db;
        _import = import;
    }

    public int? CategoryFilter { get; set; }
    public List<ContentRow> Contents { get; set; } = new();
    public ImportResult? ImportResult { get; set; }

    public class ContentRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public ContentCategory Category { get; set; }
        public bool IsActive { get; set; }
    }

    public async Task OnGetAsync(int? category)
    {
        CategoryFilter = category;
        var query = _db.Contents.AsQueryable();
        if (category.HasValue)
            query = query.Where(c => (int)c.Category == category.Value);
        Contents = await query
            .OrderBy(c => c.Category).ThenBy(c => c.Title)
            .Select(c => new ContentRow { Id = c.Id, Title = c.Title, Category = c.Category, IsActive = c.IsActive })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostScrapeAsync()
    {
        ImportResult = await _import.ImportFromScrapersAsync(1000, 1000, 1000);
        await OnGetAsync(CategoryFilter);
        return Page();
    }
}
