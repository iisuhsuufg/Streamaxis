using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Middleware;
using StreamAxis.Api.Scraping;
using StreamAxis.Api.Services;
using Npgsql;
using System;

var builder = WebApplication.CreateBuilder(args);

// ✅ Convert DATABASE_URL to Npgsql connection string safely
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    try
    {
        var databaseUri = new Uri(databaseUrl);
        var userInfo = databaseUri.UserInfo.Split(':');

        var builderNpgsql = new NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.Port,
            Database = databaseUri.AbsolutePath.TrimStart('/'),
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : string.Empty,
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        connectionString = builderNpgsql.ConnectionString;
    }
    catch
    {
        throw new Exception("DATABASE_URL is invalid. Check format: postgres://user:pass@host:port/dbname");
    }
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));  // Changed from UseSqlite to UseNpgsql

builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDlhdScraper, DlhdScraper>();
builder.Services.AddScoped<IMyFlixerScraper, MyFlixerScraper>();
builder.Services.AddScoped<IContentImportService, ContentImportService>();
builder.Services.AddControllers();
builder.Services.AddRazorPages();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Disable HTTPS redirection in development
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

app.Run();
