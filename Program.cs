using event_confirmation_list.Data;
using event_confirmation_list.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<AdminSessionStore>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/evento", async (AppDbContext db) =>
{
    var eventInfo = await db.EventInfos.AsNoTracking().FirstOrDefaultAsync();
    return eventInfo is null ? Results.NotFound() : Results.Ok(eventInfo);
});

app.MapPost("/api/admin/login", (AdminLoginRequest request, HttpContext http, IConfiguration config, AdminSessionStore sessions) =>
{
    var configuredPassword = config["Admin:Password"] ?? "123456";

    if (!string.Equals(request.Password, configuredPassword, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    var token = sessions.CreateSession();
    http.Response.Cookies.Append("admin_auth", token, new CookieOptions
    {
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Strict,
        Secure = false,
        Expires = DateTimeOffset.UtcNow.AddHours(8)
    });

    return Results.Ok(new { message = "Login realizado com sucesso." });
});

app.MapGet("/api/admin/session", (HttpContext http, AdminSessionStore sessions) =>
{
    var token = http.Request.Cookies["admin_auth"];
    if (string.IsNullOrWhiteSpace(token) || !sessions.IsValid(token))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new { authenticated = true });
});

app.MapPost("/api/evento", async (EventInfo request, AppDbContext db, HttpContext http, AdminSessionStore sessions) =>
{
    var token = http.Request.Cookies["admin_auth"];
    if (string.IsNullOrWhiteSpace(token) || !sessions.IsValid(token))
    {
        return Results.Unauthorized();
    }

    var existing = await db.EventInfos.FirstOrDefaultAsync();

    if (existing is null)
    {
        request.Id = Guid.NewGuid();
        db.EventInfos.Add(request);
    }
    else
    {
        existing.Title = request.Title;
        existing.Date = request.Date;
        existing.Time = request.Time;
        existing.Location = request.Location;
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Evento salvo com sucesso." });
});

app.MapPost("/api/confirmar", async (ConfirmGuestRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "Nome do convidado é obrigatório." });
    }

    var guest = new Guest
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        ConfirmedAt = DateTime.UtcNow
    };

    db.Guests.Add(guest);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Presença confirmada com sucesso." });
});

app.MapGet("/api/admin/dados", async (AppDbContext db, HttpContext http, AdminSessionStore sessions) =>
{
    var token = http.Request.Cookies["admin_auth"];
    if (string.IsNullOrWhiteSpace(token) || !sessions.IsValid(token))
    {
        return Results.Unauthorized();
    }

    var eventInfo = await db.EventInfos.AsNoTracking().FirstOrDefaultAsync();
    var guests = await db.Guests
        .AsNoTracking()
        .OrderBy(g => g.Name)
        .Select(g => g.Name)
        .ToListAsync();

    return Results.Ok(new
    {
        eventInfo,
        totalGuests = guests.Count,
        confirmedGuests = guests
    });
});

app.Run();

public record ConfirmGuestRequest(string Name);
public record AdminLoginRequest(string Password);

public class AdminSessionStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _sessions = new();

    public string CreateSession()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _sessions[token] = DateTimeOffset.UtcNow.AddHours(8);
        return token;
    }

    public bool IsValid(string token)
    {
        if (!_sessions.TryGetValue(token, out var expiresAt))
        {
            return false;
        }

        if (expiresAt < DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return false;
        }

        return true;
    }
}
