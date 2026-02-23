namespace StreamAxis.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public DateTime ExpirationDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxDevices { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<UserPlaybackState> PlaybackStates { get; set; } = new List<UserPlaybackState>();
}
