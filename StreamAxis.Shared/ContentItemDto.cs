namespace StreamAxis.Shared;

public class ContentItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? StreamUrl { get; set; }
    public ContentCategory Category { get; set; }
    public long? ResumePositionTicks { get; set; }
}
