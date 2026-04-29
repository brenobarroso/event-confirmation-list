using event_confirmation_list.Models;
using Microsoft.EntityFrameworkCore;

namespace event_confirmation_list.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<EventInfo> EventInfos => Set<EventInfo>();
    public DbSet<Guest> Guests => Set<Guest>();
}
