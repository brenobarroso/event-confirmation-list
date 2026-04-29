using event_confirmation_list.Data;
using event_confirmation_list.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/evento", async (AppDbContext db) =>
{
    var eventInfo = await db.EventInfos.AsNoTracking().FirstOrDefaultAsync();
    return eventInfo is null ? Results.NotFound() : Results.Ok(eventInfo);
});

app.MapPost("/api/evento", async (EventInfo request, AppDbContext db) =>
{
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

app.MapGet("/api/admin/dados", async (AppDbContext db) =>
{
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
