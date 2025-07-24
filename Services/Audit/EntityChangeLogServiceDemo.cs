using Microsoft.EntityFrameworkCore;

/// <summary>
/// Demo class to show how to use the EntityChangeLogService.
/// This can be called from a controller, service, or any other part of the application.
/// </summary>
public static class EntityChangeLogServiceDemo
{
    /// <summary>
    /// Demonstrates the usage of EntityChangeLogService with examples.
    /// </summary>
    public static async Task DemonstrateUsage(IEntityChangeLogService changeLogService)
    {
        var sampleEntityId = Guid.NewGuid();
        var userId = "admin@example.com";

        Console.WriteLine("=== EntityChangeLogService Demo ===\n");

        // Example 1: Log an entity creation
        Console.WriteLine("1. Logging entity creation...");
        await changeLogService.AddChangeLogAsync(
            entityName: "Product",
            entityId: sampleEntityId,
            operationType: "Insert",
            propertyName: "Name",
            oldValue: null,
            newValue: "Sample Product",
            changedBy: userId,
            entityDisplayName: "Sample Product"
        );

        // Example 2: Log property updates
        Console.WriteLine("2. Logging property updates...");
        await changeLogService.AddChangeLogAsync(
            entityName: "Product",
            entityId: sampleEntityId,
            operationType: "Update",
            propertyName: "Price",
            oldValue: "10.00",
            newValue: "15.00",
            changedBy: userId
        );

        await changeLogService.AddChangeLogAsync(
            entityName: "Product",
            entityId: sampleEntityId,
            operationType: "Update",
            propertyName: "Description",
            oldValue: "Old description",
            newValue: "New updated description",
            changedBy: userId
        );

        // Example 3: Get entity history with filtering
        Console.WriteLine("3. Retrieving entity history...");
        var history = await changeLogService.GetEntityHistoryAsync(
            entityName: "Product",
            entityId: sampleEntityId
        );
        Console.WriteLine($"   Found {history.Count} changes for this entity");

        // Example 4: Get changes by user
        Console.WriteLine("4. Retrieving changes by user...");
        var userChanges = await changeLogService.GetUserChangesAsync(userId);
        Console.WriteLine($"   User {userId} made {userChanges.Count} changes");

        // Example 5: Get last change for entity
        Console.WriteLine("5. Getting last change...");
        var lastChange = await changeLogService.GetLastChangeAsync("Product", sampleEntityId);
        if (lastChange != null)
        {
            Console.WriteLine($"   Last change: {lastChange.PropertyName} = {lastChange.NewValue} at {lastChange.ChangedAt}");
        }

        // Example 6: Get property-specific changes
        Console.WriteLine("6. Getting price changes...");
        var priceChanges = await changeLogService.GetPropertyChangesAsync(
            entityName: "Product",
            entityId: sampleEntityId,
            propertyName: "Price"
        );
        Console.WriteLine($"   Found {priceChanges.Count} price changes");

        // Example 7: Get all changes for Product entity type
        Console.WriteLine("7. Getting all Product entity changes...");
        var productChanges = await changeLogService.GetEntityTypeHistoryAsync("Product");
        Console.WriteLine($"   Found {productChanges.Count} total Product changes");

        Console.WriteLine("\n=== Demo completed successfully! ===");
    }

    /// <summary>
    /// Example of how to clean up change logs (use with caution!)
    /// </summary>
    public static async Task CleanupDemo(IEntityChangeLogService changeLogService)
    {
        var sampleEntityId = Guid.NewGuid();
        
        // Create some test data
        await changeLogService.AddChangeLogAsync(
            "TestEntity", sampleEntityId, "Insert", "TestProperty", 
            null, "TestValue", "TestUser"
        );

        Console.WriteLine("Cleanup Demo:");

        // Delete specific entity history
        var deletedCount = await changeLogService.DeleteEntityHistoryAsync("TestEntity", sampleEntityId);
        Console.WriteLine($"   Deleted {deletedCount} records for specific entity");

        // WARNING: This deletes ALL audit history - use with extreme caution!
        // var allDeletedCount = await changeLogService.DeleteAllHistoryAsync();
        // Console.WriteLine($"   Deleted {allDeletedCount} total records");
    }
}