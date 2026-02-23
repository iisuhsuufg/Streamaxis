namespace StreamAxis.Api.Entities;

public class Session
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceId { get; set; } = "";
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
