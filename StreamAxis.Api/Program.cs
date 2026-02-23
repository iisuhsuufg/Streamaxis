using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Middleware;
using StreamAxis.Api.Scraping;
using StreamAxis.Api.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// ✅ Get Railway DATABASE_URL environment variable
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(databaseUrl))
{
    throw new Exception("DATABASE_URL is not set. Make sure Postgres is attached in Railway.");
}

// ✅ Convert Railway URL → Npgsql connection string
var databaseUri = new Uri(databaseUrl);
var userInfo = databaseUri.UserInfo.Split(':');

var connectionString =
    $"Host={databaseUri.Host};" +
    $"Port={databaseUri.Port};" +
    $"Database={databaseUri.AbsolutePath.TrimStart('/')};" +
    $"Username={userInfo[0]};" +
    $"Password={userInfo[1]};" +
    "SSL Mode=Require;Trust Server Certificate=true;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDlhdScraper, DlhdScraper>();
builder.Services.AddScoped<IMyFlixerScraper, MyFlixerScraper>();
builder.Services.AddScoped<IContentImportService, ContentImportService>();

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only force HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseMiddleware<SessionAuthMiddleware>();
app.UseMiddleware<AdminAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

// ✅ Run EF migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

// Make the app listen on all interfaces and the Railway-assigned port
app.Urls.Add($"http://*:{port}");
app.Run();

