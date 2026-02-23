namespace StreamAxis.Shared;

public class UserProfileDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public int MaxDevices { get; set; }
    public DateTime ExpirationDate { get; set; }
}
