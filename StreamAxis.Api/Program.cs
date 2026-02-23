using Microsoft.EntityFrameworkCore;
using StreamAxis.Api.Data;
using StreamAxis.Api.Middleware;
using StreamAxis.Api.Scraping;
using StreamAxis.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Use PostgreSQL if DATABASE_URL is available, otherwise fall back to SQLite
var connectionString = builder.Configuration.GetConnectionString("DATABASE_URL") ?? 
                      builder.Configuration.GetConnectionString("DefaultConnection");

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();