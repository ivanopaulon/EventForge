using Microsoft.EntityFrameworkCore;

public class EventForgeDbContext : DbContext
{
    public EventForgeDbContext(DbContextOptions<EventForgeDbContext> options)
        : base(options)
    {
    }

    // Aggiungi qui i DbSet<T> per le tue entità
}