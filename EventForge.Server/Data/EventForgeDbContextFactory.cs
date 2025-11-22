using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventForge.Server.Data;

/// <summary>
/// Design-time factory for EventForgeDbContext to support EF Core migrations.
/// </summary>
public class EventForgeDbContextFactory : IDesignTimeDbContextFactory<EventForgeDbContext>
{
    public EventForgeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EventForgeDbContext>();
        
        // Use a dummy connection string for design-time operations
        // The actual connection string will be provided at runtime
        optionsBuilder.UseSqlServer("Server=localhost;Database=EventForge;Integrated Security=true;TrustServerCertificate=true;");
        
        return new EventForgeDbContext(optionsBuilder.Options);
    }
}
