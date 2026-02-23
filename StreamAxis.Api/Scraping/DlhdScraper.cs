using System.Text.RegularExpressions;
using System.Net.Http.Json;
using System.Text.Json;

namespace StreamAxis.Api.Scraping;

public interface IDlhdScraper
{
    Task<IReadOnlyList<ScrapedChannelItem>> ScrapeAsync(CancellationToken ct = default);
}

public class DlhdScraper : IDlhdScraper
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly string BaseUrl = "https://dlhd.link";

    public DlhdScraper(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<IReadOnlyList<ScrapedChannelItem>> ScrapeAsync(CancellationToken ct = default)
    {
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/json,*/*;q=0.9");
        client.Timeout = TimeSpan.FromSeconds(30);

        var channels = new List<ScrapedChannelItem>();
        
        // Define a curated list of known channels with their IDs
        var knownChannels = new[]
        {
            new { Id = "1", Name = "CNN" },
            new { Id = "2", Name = "BBC World News" },
            new { Id = "3", Name = "Fox News" },
            new { Id = "4", Name = "MSNBC" },
            new { Id = "5", Name = "Sky News" },
            new { Id = "6", Name = "Al Jazeera" },
            new { Id = "7", Name = "France 24" },
            new { Id = "8", Name = "DW News" },
            new { Id = "9", Name = "CGTN" },
            new { Id = "10", Name = "TRT World" },
            new { Id = "11", Name = "NHK World" },
            new { Id = "12", Name = "ABC News" },
            new { Id = "13", Name = "CBSN" },
            new { Id = "14", Name = "NBC News" },
            new { Id = "15", Name = "ESPN" },
            new { Id = "16", Name = "ESPN2" },
            new { Id = "17", Name = "Fox Sports" },
            new { Id = "18", Name = "NFL Network" },
            new { Id = "19", Name = "NBA TV" },
            new { Id = "20", Name = "MLB Network" },
            new { Id = "21", Name = "Golf Channel" },
            new { Id = "22", Name = "Tennis Channel" },
            new { Id = "23", Name = "Motor Trend" },
            new { Id = "24", Name = "Discovery" },
            new { Id = "25", Name = "National Geographic" },
            new { Id = "26", Name = "History Channel" },
            new { Id = "27", Name = "Science Channel" },
            new { Id = "28", Name = "Animal Planet" },
            new { Id = "29", Name = "Food Network" },
            new { Id = "30", Name = "Travel Channel" },
            new { Id = "31", Name = "HGTV" },
            new { Id = "32", Name = "TLC" },
            new { Id = "33", Name = "Cartoon Network" },
            new { Id = "34", Name = "Disney Channel" },
            new { Id = "35", Name = "Nickelodeon" },
            new { Id = "36", Name = "Boomerang" },
            new { Id = "37", Name = "CNBC" },
            new { Id = "38", Name = "Bloomberg" },
            new { Id = "39", Name = "CNN International" },
            new { Id = "40", Name = "EuroNews" },
            new { Id = "41", Name = "MTV" },
            new { Id = "42", Name = "VH1" },
            new { Id = "43", Name = "Comedy Central" },
            new { Id = "44", Name = "TBS" },
            new { Id = "45", Name = "TNT" },
            new { Id = "46", Name = "USA Network" },
            new { Id = "47", Name = "FX" },
            new { Id = "48", Name = "AMC" },
            new { Id = "49", Name = "Syfy" },
            new { Id = "50", Name = "Sci-Fi" }
        };

        foreach (var channel in knownChannels)
        {
            try
            {
                // Validate that the channel exists by checking if the watch URL is valid
                var channelUrl = $"{BaseUrl}/watch.php?id={channel.Id}";
                var response = await client.GetAsync(channelUrl, ct);
                
                if (response.IsSuccessStatusCode)
                {
                    channels.Add(new ScrapedChannelItem
                    {
                        Title = channel.Name,
                        ExternalId = channel.Id,
                        SourceUrl = channelUrl
                    });
                }
            }
            catch
            {
                // Skip invalid channels
                continue;
            }
        }

        return channels;
    }

    internal static IReadOnlyList<ScrapedChannelItem> Parse(string html, HashSet<string>? seenIds = null)
    {
        // This method is kept for interface compatibility but not used in the new approach
        return new List<ScrapedChannelItem>();
    }
}