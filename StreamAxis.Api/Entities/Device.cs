namespace StreamAxis.Api.Entities;

public class Device
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public DateTime LastLoginDate { get; set; }

    public User User { get; set; } = null!;
}
