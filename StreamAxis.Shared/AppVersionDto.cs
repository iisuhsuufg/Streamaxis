namespace StreamAxis.Shared;

public class AppVersionDto
{
    public string CurrentVersion { get; set; } = "";
    public string? LatestApkUrl { get; set; }
    public bool IsUpdateRequired { get; set; }
    public string? UpdateMessage { get; set; }
}
