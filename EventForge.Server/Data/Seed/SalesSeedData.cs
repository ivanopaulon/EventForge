using EventForge.Server.Data.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data.Seed;

/// <summary>
/// Seed data for Sales module.
/// </summary>
public static class SalesSeedData
{
    /// <summary>
    /// Seeds default NoteFlags for a tenant.
    /// </summary>
    public static async Task SeedNoteFlagsAsync(EventForgeDbContext context, Guid tenantId)
    {
        // Check if already seeded
        if (await context.NoteFlags.AnyAsync(nf => nf.TenantId == tenantId))
        {
            return;
        }

        var noteFlags = new List<NoteFlag>
        {
            new NoteFlag
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = "GENERAL",
                Name = "Nota Generica",
                Description = "Nota generica senza categoria specifica",
                Color = "#607D8B",
                Icon = "note",
                IsActive = true,
                DisplayOrder = 1,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "System",
                ModifiedAt = DateTime.UtcNow
            },
            new NoteFlag
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = "URGENT",
                Name = "Urgente",
                Description = "Richiede attenzione immediata",
                Color = "#F44336",
                Icon = "priority_high",
                IsActive = true,
                DisplayOrder = 2,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "System",
                ModifiedAt = DateTime.UtcNow
            },
            new NoteFlag
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = "CUSTOMER_REQUEST",
                Name = "Richiesta Cliente",
                Description = "Richiesta particolare del cliente",
                Color = "#2196F3",
                Icon = "person_outline",
                IsActive = true,
                DisplayOrder = 3,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "System",
                ModifiedAt = DateTime.UtcNow
            },
            new NoteFlag
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = "DISCOUNT_REASON",
                Name = "Motivo Sconto",
                Description = "Motivazione per sconto applicato",
                Color = "#FF9800",
                Icon = "discount",
                IsActive = true,
                DisplayOrder = 4,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "System",
                ModifiedAt = DateTime.UtcNow
            },
            new NoteFlag
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = "KITCHEN_NOTE",
                Name = "Nota Cucina",
                Description = "Istruzioni per la cucina/preparazione",
                Color = "#4CAF50",
                Icon = "restaurant",
                IsActive = true,
                DisplayOrder = 5,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                ModifiedBy = "System",
                ModifiedAt = DateTime.UtcNow
            }
        };

        context.NoteFlags.AddRange(noteFlags);
        await context.SaveChangesAsync();
    }
}
