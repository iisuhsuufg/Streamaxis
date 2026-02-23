using StreamAxis.Shared;

namespace StreamAxis.Api.Models;

public class LoginResponse
{
    public string SessionToken { get; set; } = "";
    public UserProfileDto UserProfile { get; set; } = null!;
}
