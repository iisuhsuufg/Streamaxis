using StreamAxis.Shared;

namespace StreamAxis.Api.Entities;

public class Content
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? StreamUrl { get; set; }
    public ContentCategory Category { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserPlaybackState> UserPlaybackStates { get; set; } = new List<UserPlaybackState>();
    public virtual ICollection<Episode> Episodes { get; set; } = new List<Episode>();
}
