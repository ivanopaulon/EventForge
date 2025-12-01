using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace EventForge.Server.Data;

/// <summary>
/// Design-time factory per EF Core CLI tools.
/// Logga la connection string (con password mascherata) usata da dotnet ef.
/// </summary>
public class EventForgeDbContextFactory : IDesignTimeDbContextFactory<EventForgeDbContext>
{
    public EventForgeDbContext CreateDbContext(string[] args)
    {
        // Base path = cartella del progetto EventForge.Server
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Prova nomi comuni (usa lo stesso nome che usi in AddConfiguredDbContext)
        var csNames = new[] { "SqlServer", "DefaultConnection", "ConnectionString", "Database" };
        string? connectionString = null;
        string? usedName = null;

        foreach (var name in csNames)
        {
            var cs = configuration.GetConnectionString(name);
            if (!string.IsNullOrEmpty(cs))
            {
                connectionString = cs;
                usedName = name;
                break;
            }
        }

        // Se ancora nulla, prendi la prima definita
        if (connectionString == null)
        {
            var section = configuration.GetSection("ConnectionStrings");
            var first = section.GetChildren().FirstOrDefault();
            if (first != null)
            {
                usedName = first.Key;
                connectionString = first.Value;
            }
        }

        // Log su console (dotnet ef cattura stdout). Maschera password se presente.
        if (!string.IsNullOrEmpty(connectionString))
        {
            var masked = MaskPassword(connectionString);
            Console.WriteLine($"[EventForgeDbContextFactory] USING ConnectionString name='{usedName}' -> {masked}");
        }
        else
        {
            Console.WriteLine("[EventForgeDbContextFactory] No connection string found in appsettings.* or environment variables.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<EventForgeDbContext>();

        if (!string.IsNullOrEmpty(connectionString))
        {
            optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
            });
        }
        else
        {
            // fallback: connection dummy per evitare crash; dotnet ef comunque mostrerà il messaggio sopra
            optionsBuilder.UseSqlServer("Server=localhost;Database=EventForgeDummy;Trusted_Connection=True;TrustServerCertificate=True;");
        }

        return new EventForgeDbContext(optionsBuilder.Options);
    }

    private static string MaskPassword(string cs)
    {
        try
        {
            // cerca password= o pwd=
            var parts = cs.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                var idx = p.IndexOf('=', StringComparison.Ordinal);
                if (idx > 0)
                {
                    var key = p.Substring(0, idx).Trim().ToLowerInvariant();
                    if (key.Contains("password") || key.Contains("pwd"))
                    {
                        parts[i] = key + "=****";
                    }
                }
            }
            return string.Join(";", parts);
        }
        catch
        {
            return "**** (error masking)";
        }
    }
}
