using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Entities;
using StreamAxis.Api.Scraping;
using StreamAxis.Shared;

namespace StreamAxis.Api.Services;

public class ImportResult
{
    public int LiveTvAdded { get; set; }
    public int MoviesAdded { get; set; }
    public int TvShowsAdded { get; set; }
    public string? Error { get; set; }
}

public interface IContentImportService
{
    Task<ImportResult> ImportFromScrapersAsync(int maxLiveTv = 1000, int maxMovies = 1000, int maxTvShows = 1000, CancellationToken ct = default);
}

public class ContentImportService : IContentImportService
{
    private readonly AppDbContext _db;
    private readonly IDlhdScraper _dlhd;
    private readonly IMyFlixerScraper _myFlixer;
    private const string DemoHlsUrl = "https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8";

    public ContentImportService(AppDbContext db, IDlhdScraper dlhd, IMyFlixerScraper myFlixer)
    {
        _db = db;
        _dlhd = dlhd;
        _myFlixer = myFlixer;
    }

    public async Task<ImportResult> ImportFromScrapersAsync(int maxLiveTv = 1000, int maxMovies = 1000, int maxTvShows = 1000, CancellationToken ct = default)
    {
        var result = new ImportResult();
        var now = DateTime.UtcNow;
        try
        {
            var liveChannels = await _dlhd.ScrapeAsync(ct);
            foreach (var ch in liveChannels.Take(maxLiveTv))
            {
                var exists = await _db.Contents.AnyAsync(c => c.Category == ContentCategory.LiveTv && c.Title == ch.Title, ct);
                if (exists) continue;
                _db.Contents.Add(new Content
                {
                    Title = ch.Title,
                    Description = $"24/7 channel: {ch.Title}",
                    PosterUrl = null,
                    StreamUrl = DemoHlsUrl,
                    Category = ContentCategory.LiveTv,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                result.LiveTvAdded++;
            }

            var movies = await _myFlixer.ScrapeMoviesAsync(maxMovies, ct);
            foreach (var m in movies)
            {
                if (await _db.Contents.AnyAsync(c => c.Category == ContentCategory.Movie && c.Title == m.Title, ct)) continue;
                _db.Contents.Add(new Content
                {
                    Title = m.Title,
                    Description = m.Description,
                    PosterUrl = m.PosterUrl,
                    StreamUrl = DemoHlsUrl,
                    Category = ContentCategory.Movie,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                result.MoviesAdded++;
            }

            var tvShows = await _myFlixer.ScrapeTvShowsAsync(maxTvShows, ct);
            foreach (var t in tvShows)
            {
                var existingContent = await _db.Contents.FirstOrDefaultAsync(c => c.Category == ContentCategory.TvShow && c.Title == t.Title, ct);
                
                Content content;
                if (existingContent != null)
                {
                    // Update existing TV show
                    content = existingContent;
                    content.Description = t.Description;
                    content.PosterUrl = t.PosterUrl;
                    content.UpdatedAt = now;
                }
                else
                {
                    // Create new TV show
                    content = new Content
                    {
                        Title = t.Title,
                        Description = t.Description,
                        PosterUrl = t.PosterUrl,
                        StreamUrl = DemoHlsUrl, // Default stream URL
                        Category = ContentCategory.TvShow,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _db.Contents.Add(content);
                    result.TvShowsAdded++;
                }

                // Add episodes for this TV show if they don't already exist
                foreach (var episode in t.Episodes)
                {
                    var episodeExists = await _db.Episodes.AnyAsync(e => 
                        e.ContentId == content.Id && 
                        e.SeasonNumber == episode.SeasonNumber && 
                        e.EpisodeNumber == episode.EpisodeNumber, ct);
                    
                    if (!episodeExists)
                    {
                        _db.Episodes.Add(new Episode
                        {
                            ContentId = content.Id,
                            Title = episode.Title,
                            Description = episode.Description,
                            PosterUrl = episode.PosterUrl,
                            StreamUrl = episode.StreamUrl ?? DemoHlsUrl,
                            SeasonNumber = episode.SeasonNumber,
                            EpisodeNumber = episode.EpisodeNumber,
                            IsActive = true,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }
        return result;
    }
}
