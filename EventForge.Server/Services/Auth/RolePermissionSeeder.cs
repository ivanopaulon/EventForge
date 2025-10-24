using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Auth;

/// <summary>
/// Static helper to seed roles, permissions and role-permission assignments.
/// Kept as a separate unit to allow easier testing and reuse.
/// </summary>
public static class RolePermissionSeeder
{
 public static async Task<bool> SeedAsync(EventForgeDbContext _dbContext, ILogger logger, CancellationToken cancellationToken = default)
 {
 try
 {
 // Define default permissions
 Permission[] defaultPermissions = new Permission[]
 {
 new Permission { Name = "Users.Users.Create", DisplayName = "Create Users", Category = "Users", Resource = "Users", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Users.Users.Read", DisplayName = "View Users", Category = "Users", Resource = "Users", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Users.Users.Update", DisplayName = "Update Users", Category = "Users", Resource = "Users", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Users.Users.Delete", DisplayName = "Delete Users", Category = "Users", Resource = "Users", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Users.Roles.Create", DisplayName = "Create Roles", Category = "Users", Resource = "Roles", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Users.Roles.Read", DisplayName = "View Roles", Category = "Users", Resource = "Roles", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Users.Roles.Update", DisplayName = "Update Roles", Category = "Users", Resource = "Roles", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Users.Roles.Delete", DisplayName = "Delete Roles", Category = "Users", Resource = "Roles", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Events.Events.Create", DisplayName = "Create Events", Category = "Events", Resource = "Events", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Events.Events.Read", DisplayName = "View Events", Category = "Events", Resource = "Events", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Events.Events.Update", DisplayName = "Update Events", Category = "Events", Resource = "Events", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Events.Events.Delete", DisplayName = "Delete Events", Category = "Events", Resource = "Events", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Events.Teams.Create", DisplayName = "Create Teams", Category = "Events", Resource = "Teams", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Events.Teams.Read", DisplayName = "View Teams", Category = "Events", Resource = "Teams", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Events.Teams.Update", DisplayName = "Update Teams", Category = "Events", Resource = "Teams", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Events.Teams.Delete", DisplayName = "Delete Teams", Category = "Events", Resource = "Teams", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Products.Products.Create", DisplayName = "Create Products", Category = "Products", Resource = "Products", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Products.Products.Read", DisplayName = "View Products", Category = "Products", Resource = "Products", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Products.Products.Update", DisplayName = "Update Products", Category = "Products", Resource = "Products", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Products.Products.Delete", DisplayName = "Delete Products", Category = "Products", Resource = "Products", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Products.Warehouse.Create", DisplayName = "Create Warehouse Operations", Category = "Products", Resource = "Warehouse", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Products.Warehouse.Read", DisplayName = "View Warehouse Operations", Category = "Products", Resource = "Warehouse", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Products.Warehouse.Update", DisplayName = "Update Warehouse Operations", Category = "Products", Resource = "Warehouse", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Products.Warehouse.Delete", DisplayName = "Delete Warehouse Operations", Category = "Products", Resource = "Warehouse", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Documents.Documents.Create", DisplayName = "Create Documents", Category = "Documents", Resource = "Documents", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Documents.Documents.Read", DisplayName = "View Documents", Category = "Documents", Resource = "Documents", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Documents.Documents.Update", DisplayName = "Update Documents", Category = "Documents", Resource = "Documents", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Documents.Documents.Delete", DisplayName = "Delete Documents", Category = "Documents", Resource = "Documents", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Financial.Banks.Create", DisplayName = "Create Banks", Category = "Financial", Resource = "Banks", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Financial.Banks.Read", DisplayName = "View Banks", Category = "Financial", Resource = "Banks", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Financial.Banks.Update", DisplayName = "Update Banks", Category = "Financial", Resource = "Banks", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Financial.Banks.Delete", DisplayName = "Delete Banks", Category = "Financial", Resource = "Banks", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Sales.Sales.Create", DisplayName = "Create Sales", Category = "Sales", Resource = "Sales", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Sales.Sales.Read", DisplayName = "View Sales", Category = "Sales", Resource = "Sales", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Sales.Sales.Update", DisplayName = "Update Sales", Category = "Sales", Resource = "Sales", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Sales.Sales.Delete", DisplayName = "Delete Sales", Category = "Sales", Resource = "Sales", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Sales.Tables.Create", DisplayName = "Create Tables", Category = "Sales", Resource = "Tables", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Sales.Tables.Read", DisplayName = "View Tables", Category = "Sales", Resource = "Tables", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Sales.Tables.Update", DisplayName = "Update Tables", Category = "Sales", Resource = "Tables", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Sales.Tables.Delete", DisplayName = "Delete Tables", Category = "Sales", Resource = "Tables", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Sales.PaymentMethods.Create", DisplayName = "Create Payment Methods", Category = "Sales", Resource = "PaymentMethods", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Sales.PaymentMethods.Read", DisplayName = "View Payment Methods", Category = "Sales", Resource = "PaymentMethods", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Sales.PaymentMethods.Update", DisplayName = "Update Payment Methods", Category = "Sales", Resource = "PaymentMethods", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Sales.PaymentMethods.Delete", DisplayName = "Delete Payment Methods", Category = "Sales", Resource = "PaymentMethods", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Communication.Notifications.Create", DisplayName = "Create Notifications", Category = "Communication", Resource = "Notifications", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Communication.Notifications.Read", DisplayName = "View Notifications", Category = "Communication", Resource = "Notifications", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Communication.Notifications.Update", DisplayName = "Update Notifications", Category = "Communication", Resource = "Notifications", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Communication.Notifications.Delete", DisplayName = "Delete Notifications", Category = "Communication", Resource = "Notifications", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Communication.Chat.Create", DisplayName = "Create Chat Messages", Category = "Communication", Resource = "Chat", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Communication.Chat.Read", DisplayName = "View Chat Messages", Category = "Communication", Resource = "Chat", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Communication.Chat.Update", DisplayName = "Update Chat Messages", Category = "Communication", Resource = "Chat", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Communication.Chat.Delete", DisplayName = "Delete Chat Messages", Category = "Communication", Resource = "Chat", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Retail.Carts.Create", DisplayName = "Create Retail Carts", Category = "Retail", Resource = "Carts", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Retail.Carts.Read", DisplayName = "View Retail Carts", Category = "Retail", Resource = "Carts", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Retail.Carts.Update", DisplayName = "Update Retail Carts", Category = "Retail", Resource = "Carts", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Retail.Carts.Delete", DisplayName = "Delete Retail Carts", Category = "Retail", Resource = "Carts", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Retail.Stores.Create", DisplayName = "Create Stores", Category = "Retail", Resource = "Stores", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Retail.Stores.Read", DisplayName = "View Stores", Category = "Retail", Resource = "Stores", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Retail.Stores.Update", DisplayName = "Update Stores", Category = "Retail", Resource = "Stores", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Retail.Stores.Delete", DisplayName = "Delete Stores", Category = "Retail", Resource = "Stores", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Printing.Print.Create", DisplayName = "Create Print Jobs", Category = "Printing", Resource = "Print", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Printing.Print.Read", DisplayName = "View Print Jobs", Category = "Printing", Resource = "Print", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Entities.Entities.Create", DisplayName = "Create Entities", Category = "Entities", Resource = "Entities", Action = "Create", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Entities.Entities.Read", DisplayName = "View Entities", Category = "Entities", Resource = "Entities", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Entities.Entities.Update", DisplayName = "Update Entities", Category = "Entities", Resource = "Entities", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Entities.Entities.Delete", DisplayName = "Delete Entities", Category = "Entities", Resource = "Entities", Action = "Delete", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "Reports.Reports.Read", DisplayName = "View Reports", Category = "Reports", Resource = "Reports", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "Reports.Audit.Read", DisplayName = "View Audit Logs", Category = "Reports", Resource = "Audit", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty },

 new Permission { Name = "System.Settings.Update", DisplayName = "Update System Settings", Category = "System", Resource = "Settings", Action = "Update", IsSystemPermission = true, TenantId = Guid.Empty },
 new Permission { Name = "System.Logs.Read", DisplayName = "View System Logs", Category = "System", Resource = "Logs", Action = "Read", IsSystemPermission = true, TenantId = Guid.Empty }
 };

 // Fetch existing permission names in a single query
 var existingPermissionNames = await _dbContext.Permissions.Select(p => p.Name).ToListAsync(cancellationToken);
 var existingNamesSet = existingPermissionNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

 var toAddPermissions = new List<Permission>();

 foreach (var permission in defaultPermissions)
 {
 if (!existingNamesSet.Contains(permission.Name))
 {
 permission.CreatedBy = "system";
 permission.CreatedAt = DateTime.UtcNow;
 toAddPermissions.Add(permission);
 }
 }

 if (toAddPermissions.Any())
 {
 _dbContext.Permissions.AddRange(toAddPermissions);
 _ = await _dbContext.SaveChangesAsync(cancellationToken);
 logger.LogInformation("Added {Count} new system permissions", toAddPermissions.Count);
 }
 else
 {
 logger.LogInformation("No new system permissions to add");
 }

 // Ensure default roles exist (create if missing)
 var defaultRoles = new[]
 {
 new Role { Name = "SuperAdmin", DisplayName = "Super Administrator", Description = "Full unrestricted system access", IsSystemRole = true, TenantId = Guid.Empty },
 new Role { Name = "Admin", DisplayName = "System Administrator", Description = "Full system access", IsSystemRole = true, TenantId = Guid.Empty },
 new Role { Name = "Manager", DisplayName = "Manager", Description = "Management level access", IsSystemRole = true, TenantId = Guid.Empty },
 new Role { Name = "User", DisplayName = "Standard User", Description = "Basic user access", IsSystemRole = true, TenantId = Guid.Empty },
 new Role { Name = "Viewer", DisplayName = "Viewer", Description = "Read-only access", IsSystemRole = true, TenantId = Guid.Empty }
 };

 var existingRoles = await _dbContext.Roles.Select(r => r.Name).ToListAsync(cancellationToken);
 var existingRolesSet = existingRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);

 var rolesToAdd = new List<Role>();
 foreach (var role in defaultRoles)
 {
 if (!existingRolesSet.Contains(role.Name))
 {
 role.CreatedBy = "system";
 role.CreatedAt = DateTime.UtcNow;
 rolesToAdd.Add(role);
 }
 }

 if (rolesToAdd.Any())
 {
 _dbContext.Roles.AddRange(rolesToAdd);
 _ = await _dbContext.SaveChangesAsync(cancellationToken);
 logger.LogInformation("Added {Count} new system roles", rolesToAdd.Count);
 }

 // Load permissions and roles to assign
 var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);

 // Helper to ensure role has permissions (idempotent)
 async Task EnsureRoleHasAllPermissions(string roleName)
 {
 var role = await _dbContext.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
 if (role == null) return;

 var existingPermIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
 var toAdd = allPermissions.Where(p => !existingPermIds.Contains(p.Id)).Select(p => new RolePermission
 {
 RoleId = role.Id,
 PermissionId = p.Id,
 GrantedBy = "system",
 GrantedAt = DateTime.UtcNow,
 CreatedBy = "system",
 CreatedAt = DateTime.UtcNow,
 TenantId = Guid.Empty
 }).ToList();

 if (toAdd.Any())
 {
 _dbContext.RolePermissions.AddRange(toAdd);
 _ = await _dbContext.SaveChangesAsync(cancellationToken);
 logger.LogInformation("Assigned {Count} permissions to {Role}", toAdd.Count, roleName);
 }
 }

 // Assign all permissions to Admin and SuperAdmin roles if missing
 await EnsureRoleHasAllPermissions("Admin");
 await EnsureRoleHasAllPermissions("SuperAdmin");

 logger.LogInformation("Default roles and permissions seeded successfully");
 return true;
 }
 catch (Exception ex)
 {
 logger.LogError(ex, "Error seeding default roles and permissions");
 return false;
 }
 }
}
