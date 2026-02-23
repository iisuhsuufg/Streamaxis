using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Entities;
using StreamAxis.Shared;

namespace StreamAxis.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Content> Contents => Set<Content>();
    public DbSet<UserPlaybackState> UserPlaybackStates => Set<UserPlaybackState>();
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<Episode> Episodes => Set<Episode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(256);
            e.Property(x => x.Password).HasMaxLength(256);
        });

        modelBuilder.Entity<Device>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();
            e.Property(x => x.DeviceId).HasMaxLength(256);
            e.Property(x => x.DeviceName).HasMaxLength(256);
            e.HasOne(x => x.User).WithMany(u => u.Devices).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Token).HasMaxLength(64);
            e.Property(x => x.DeviceId).HasMaxLength(256);
            e.HasOne(x => x.User).WithMany(u => u.Sessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Content>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(512);
            e.Property(x => x.Description).HasMaxLength(2048);
            e.Property(x => x.PosterUrl).HasMaxLength(1024);
            e.Property(x => x.StreamUrl).HasMaxLength(2048);
        });

        modelBuilder.Entity<UserPlaybackState>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.ContentId }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.PlaybackStates).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Content).WithMany(c => c.UserPlaybackStates).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Episode>(e =>
        {
            e.HasOne(x => x.Content).WithMany(c => c.Episodes).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.PosterUrl).HasMaxLength(500);
            e.Property(x => x.StreamUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<AppConfig>(e =>
        {
            e.Property(x => x.CurrentVersion).HasMaxLength(32);
            e.Property(x => x.LatestApkUrl).HasMaxLength(2048);
            e.Property(x => x.UpdateMessage).HasMaxLength(1024);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            Password = "admin",
            ExpirationDate = now.AddYears(10),
            IsActive = true,
            MaxDevices = 10,
            CreatedAt = now,
            UpdatedAt = now
        });
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 2,
            Username = "demo",
            Password = "demo",
            ExpirationDate = now.AddMonths(1),
            IsActive = true,
            MaxDevices = 1,
            CreatedAt = now,
            UpdatedAt = now
        });

        modelBuilder.Entity<AppConfig>().HasData(new AppConfig
        {
            Id = 1,
            CurrentVersion = "1.0",
            LatestApkUrl = null,
            IsUpdateRequired = false,
            UpdateMessage = null,
            UpdatedAt = now
        });

        modelBuilder.Entity<Content>().HasData(
            new Content
            {
                Id = 1,
                Title = "Big Buck Bunny",
                Description = "Demo stream",
                PosterUrl = "https://peach.blender.org/wp-content/uploads/title_anouncement.jpg",
                StreamUrl = "https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8",
                Category = ContentCategory.Movie,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Content
            {
                Id = 2,
                Title = "Tears of Steel",
                Description = "Demo stream",
                PosterUrl = "https://mango.blender.org/wp-content/uploads/tearsofsteel_thumbnail.jpg",
                StreamUrl = "https://demo.unified-streaming.com/k8s/features/stable/video/tears-of-steel/tears-of-steel.ism/.m3u8",
                Category = ContentCategory.Movie,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Content
            {
                Id = 3,
                Title = "Live TV Demo",
                Description = "24/7 demo channel",
                PosterUrl = null,
                StreamUrl = "https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8",
                Category = ContentCategory.LiveTv,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        );
    }
}