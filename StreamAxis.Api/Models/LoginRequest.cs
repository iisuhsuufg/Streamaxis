namespace StreamAxis.Api.Models;

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
}
