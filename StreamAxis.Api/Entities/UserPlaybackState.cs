namespace StreamAxis.Api.Entities;

public class UserPlaybackState
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ContentId { get; set; }
    public long LastPositionTicks { get; set; }
    public DateTime LastUpdated { get; set; }

    public User User { get; set; } = null!;
    public Content Content { get; set; } = null!;
}
