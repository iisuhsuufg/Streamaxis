using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Entities;
using StreamAxis.Shared;

namespace StreamAxis.Api.Services;

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }
    public string? Token { get; set; }
}

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password, string deviceId, string deviceName);
    Task<User?> ValidateSessionAsync(string token);
    Task<Session?> GetSessionAsync(string token);
    Task InvalidateSessionAsync(string token);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AuthResult> LoginAsync(string username, string password, string deviceId, string deviceName)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || user.Password != password)
            return new AuthResult { Success = false, ErrorMessage = "Invalid username or password." };

        if (!user.IsActive)
            return new AuthResult { Success = false, ErrorMessage = "Account is disabled." };

        if (user.ExpirationDate <= DateTime.UtcNow)
            return new AuthResult { Success = false, ErrorMessage = "Subscription has expired." };

        var existingDevice = await _db.Devices.FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeviceId == deviceId);
        if (existingDevice != null)
        {
            existingDevice.LastLoginDate = DateTime.UtcNow;
            existingDevice.DeviceName = deviceName;
        }
        else
        {
            var count = await _db.Devices.CountAsync(d => d.UserId == user.Id);
            if (count >= user.MaxDevices)
                return new AuthResult { Success = false, ErrorMessage = "Device limit reached. Contact admin." };

            _db.Devices.Add(new Device
            {
                UserId = user.Id,
                DeviceId = deviceId,
                DeviceName = deviceName,
                LastLoginDate = DateTime.UtcNow
            });
        }

        var token = Guid.NewGuid().ToString("N");
        _db.Sessions.Add(new Session
        {
            UserId = user.Id,
            DeviceId = deviceId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.Add(SessionLifetime),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return new AuthResult { Success = true, User = user, Token = token };
    }

    public async Task<User?> ValidateSessionAsync(string token)
    {
        var session = await _db.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);
        if (session == null) return null;
        if (!session.User.IsActive || session.User.ExpirationDate <= DateTime.UtcNow) return null;
        return session.User;
    }

    public async Task<Session?> GetSessionAsync(string token)
    {
        var session = await _db.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);
        if (session == null || !session.User.IsActive || session.User.ExpirationDate <= DateTime.UtcNow)
            return null;
        return session;
    }

    public async Task InvalidateSessionAsync(string token)
    {
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.Token == token);
        if (session != null)
        {
            _db.Sessions.Remove(session);
            await _db.SaveChangesAsync();
        }
    }
}
