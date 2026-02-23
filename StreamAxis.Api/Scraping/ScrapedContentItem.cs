using StreamAxis.Shared;

namespace StreamAxis.Api.Scraping;

public class ScrapedContentItem
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? DetailUrl { get; set; }
    public ContentCategory Category { get; set; }
    
    // For TV Shows - list of episodes
    public List<ScrapedEpisodeItem> Episodes { get; set; } = new List<ScrapedEpisodeItem>();
}

public class ScrapedEpisodeItem
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? StreamUrl { get; set; }
    public int SeasonNumber { get; set; } = 1;
    public int EpisodeNumber { get; set; } = 0;
}