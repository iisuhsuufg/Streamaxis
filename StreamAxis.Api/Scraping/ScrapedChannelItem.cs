namespace StreamAxis.Api.Scraping;

public class ScrapedChannelItem
{
    public string Title { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string? SourceUrl { get; set; }
}
