using System.Text.RegularExpressions;
using HtmlAgilityPack;
using StreamAxis.Shared;

namespace StreamAxis.Api.Scraping;

public interface IMyFlixerScraper
{
    Task<IReadOnlyList<ScrapedContentItem>> ScrapeMoviesAsync(int maxItems = 1000, CancellationToken ct = default);
    Task<IReadOnlyList<ScrapedContentItem>> ScrapeTvShowsAsync(int maxItems = 1000, CancellationToken ct = default);
}

public class MyFlixerScraper : IMyFlixerScraper
{
    private readonly IHttpClientFactory _httpFactory;
    private const string BaseUrl = "https://myflixerz.to";

    public MyFlixerScraper(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<IReadOnlyList<ScrapedContentItem>> ScrapeMoviesAsync(int maxItems = 1000, CancellationToken ct = default)
    {
        return await ScrapeWithPaginationAsync(BaseUrl + "/movie", ContentCategory.Movie, maxItems, ct);
    }

    public async Task<IReadOnlyList<ScrapedContentItem>> ScrapeTvShowsAsync(int maxItems = 1000, CancellationToken ct = default)
    {
        return await ScrapeWithPaginationAsync(BaseUrl + "/tv-show", ContentCategory.TvShow, maxItems, ct);
    }

    private async Task<IReadOnlyList<ScrapedContentItem>> ScrapeWithPaginationAsync(string baseListUrl, ContentCategory category, int maxItems, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
        client.Timeout = TimeSpan.FromSeconds(30);

        var allItems = new List<ScrapedContentItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var maxPages = Math.Min(20, (maxItems / 24) + 1); // Scan up to 20 pages

        for (int page = 1; page <= maxPages && allItems.Count < maxItems; page++)
        {
            try
            {
                var url = page == 1 ? baseListUrl : $"{baseListUrl}?page={page}";
                var html = await client.GetStringAsync(url, ct);
                var items = ParseListPage(html, category, seen);
                
                if (items.Count == 0) break; // No more items found
                
                allItems.AddRange(items);
                
                // Small delay between pages to be respectful
                if (page < maxPages && allItems.Count < maxItems)
                    await Task.Delay(1000, ct); // Wait 1 second between pages
            }
            catch (Exception ex)
            {
                // Log error but continue to next page
                Console.WriteLine($"Error scraping page {page}: {ex.Message}");
                continue;
            }
        }

        return allItems.Take(maxItems).ToList();
    }

    private List<ScrapedContentItem> ParseListPage(string html, ContentCategory category, HashSet<string> seen)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var list = new List<ScrapedContentItem>();

        // Try multiple selectors to find content items
        var selectors = new[]
        {
            "//div[contains(@class,'film_list-wrap')]//div[contains(@class,'flw-item')]",
            "//div[contains(@class,'film-poster')]",
            "//a[contains(@class,'film-poster-ahref')]",
            "//div[contains(@class,'item')]//a[contains(@href,'/movie/') or contains(@href,'/tv/')]",
            "//a[contains(@href,'/movie/') or contains(@href,'/tv/') or contains(@href,'/watch-')]"
        };

        HtmlNodeCollection? nodes = null;
        foreach (var selector in selectors)
        {
            nodes = doc.DocumentNode.SelectNodes(selector);
            if (nodes != null && nodes.Count > 0) break;
        }

        if (nodes == null) return list;

        foreach (var node in nodes)
        {
            try
            {
                var item = ExtractContentItem(node, category);
                if (item != null && !string.IsNullOrWhiteSpace(item.Title) && seen.Add(item.Title))
                {
                    list.Add(item);
                }
            }
            catch
            {
                continue;
            }
        }

        return list;
    }

    private ScrapedContentItem? ExtractContentItem(HtmlNode node, ContentCategory category)
    {
        string? href = null;
        string? title = null;
        string? poster = null;

        // Try to get href
        var linkNode = node.Name == "a" ? node : node.SelectSingleNode(".//a[@href]");
        if (linkNode != null)
        {
            href = linkNode.GetAttributeValue("href", "");
        }

        // Try multiple ways to get title
        title = node.GetAttributeValue("title", "")?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            var titleNode = node.SelectSingleNode(".//h2") ?? 
                           node.SelectSingleNode(".//h3") ?? 
                           node.SelectSingleNode(".//*[contains(@class,'film-name')]") ??
                           node.SelectSingleNode(".//*[contains(@class,'title')]");
            title = titleNode?.InnerText?.Trim();
        }
        if (string.IsNullOrEmpty(title))
        {
            var imgNode = node.SelectSingleNode(".//img");
            title = imgNode?.GetAttributeValue("alt", "")?.Trim() ?? imgNode?.GetAttributeValue("title", "")?.Trim();
        }

        // Clean up title
        if (!string.IsNullOrEmpty(title))
        {
            title = System.Net.WebUtility.HtmlDecode(title).Trim();
            // Remove year suffix like "(2023)" for cleaner titles
            title = Regex.Replace(title, @"\s*\(\d{4}\)\s*$", "").Trim();
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrEmpty(href)) return null;
        if (href.StartsWith("#") || href.Length < 5) return null;

        // Normalize href
        if (!href.StartsWith("http"))
        {
            href = new Uri(new Uri(BaseUrl), href).ToString();
        }

        // Try to get poster
        var img = node.SelectSingleNode(".//img");
        poster = img?.GetAttributeValue("data-src", "") ?? img?.GetAttributeValue("src", "");
        if (!string.IsNullOrEmpty(poster))
        {
            if (poster.StartsWith("data:")) poster = null;
            else if (!poster.StartsWith("http"))
                poster = new Uri(new Uri(BaseUrl), poster).ToString();
        }

        var item = new ScrapedContentItem
        {
            Title = title,
            PosterUrl = string.IsNullOrEmpty(poster) ? null : poster,
            DetailUrl = href,
            Category = category
        };

        // If this is a TV show, try to extract episodes
        if (category == ContentCategory.TvShow)
        {
            item.Episodes = ExtractEpisodesForTvShow(href, title).Result;
        }

        return item;
    }

    private async Task<List<ScrapedEpisodeItem>> ExtractEpisodesForTvShow(string detailUrl, string showTitle)
    {
        var episodes = new List<ScrapedEpisodeItem>();
        
        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            client.Timeout = TimeSpan.FromSeconds(30);

            var html = await client.GetStringAsync(detailUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Look for episode lists in the TV show page
            var episodeSelectors = new[]
            {
                "//div[contains(@class,'season-block')]//div[contains(@class,'ep-item')]",
                "//div[contains(@class,'ep-list')]//a",
                "//div[contains(@class,'episode')]//a",
                "//a[contains(@href,'/episode') or contains(@href,'/watch-')]"
            };

            HtmlNodeCollection? episodeNodes = null;
            foreach (var selector in episodeSelectors)
            {
                episodeNodes = doc.DocumentNode.SelectNodes(selector);
                if (episodeNodes != null && episodeNodes.Count > 0) break;
            }

            if (episodeNodes != null)
            {
                int episodeNumber = 1;
                foreach (var node in episodeNodes.Take(20)) // Limit to first 20 episodes
                {
                    try
                    {
                        var episodeLink = node.Name == "a" ? node : node.SelectSingleNode(".//a");
                        if (episodeLink != null)
                        {
                            var episodeHref = episodeLink.GetAttributeValue("href", "");
                            var episodeTitle = episodeLink.InnerText.Trim();
                            
                            if (!string.IsNullOrEmpty(episodeHref))
                            {
                                // Try to extract season and episode number from the title or URL
                                var (seasonNum, epNum) = ExtractSeasonEpisodeNumbers(episodeTitle, episodeHref);
                                
                                // If not found in title, use the counter
                                if (epNum == 0)
                                    epNum = episodeNumber++;

                                episodes.Add(new ScrapedEpisodeItem
                                {
                                    Title = string.IsNullOrEmpty(episodeTitle) ? $"Episode {epNum}" : episodeTitle,
                                    Description = $"Episode {epNum} of {showTitle}",
                                    StreamUrl = episodeHref.StartsWith("http") ? episodeHref : new Uri(new Uri(BaseUrl), episodeHref).ToString(),
                                    SeasonNumber = seasonNum,
                                    EpisodeNumber = epNum
                                });
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }
        catch
        {
            // If episode extraction fails, return an empty list
            return new List<ScrapedEpisodeItem>();
        }

        return episodes;
    }

    private (int season, int episode) ExtractSeasonEpisodeNumbers(string title, string url)
    {
        // Try to extract from title
        var seasonEpPattern = @"(?:s|season|se)\s*(\d+)\s*[ex]\s*(\d+)|(\d+)\s*x\s*(\d+)";
        var match = Regex.Match(title + " " + url, seasonEpPattern, RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            if (!string.IsNullOrEmpty(match.Groups[1].Value))
            {
                return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
            }
            else if (!string.IsNullOrEmpty(match.Groups[3].Value))
            {
                return (int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value));
            }
        }
        
        return (1, 0); // Default to season 1, unknown episode number
    }
}