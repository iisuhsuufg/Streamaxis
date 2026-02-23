using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Middleware;
using StreamAxis.Api.Scraping;
using StreamAxis.Api.Services;
using Npgsql;
using System;

var builder = WebApplication.CreateBuilder(args);

// Convert DATABASE_URL to Npgsql connection string format
var databaseUrl = builder.Configuration.GetConnectionString("DATABASE_URL");

string connectionString;
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Parse the PostgreSQL URL format
    var databaseUri = new Uri(databaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':');
    
    connectionString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
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

//using (var scope = app.Services.CreateScope())
//{
 //   var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
 //   db.Database.Migrate();
//}


app.Run();
