namespace StreamAxis.Api.Entities;

public class AppConfig
{
    public int Id { get; set; }
    public string CurrentVersion { get; set; } = "1.0";
    public string? LatestApkUrl { get; set; }
    public bool IsUpdateRequired { get; set; }
    public string? UpdateMessage { get; set; }
    public DateTime UpdatedAt { get; set; }
}
