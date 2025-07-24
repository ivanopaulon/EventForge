using Microsoft.EntityFrameworkCore;

/// <summary>
/// Basic test class for EntityChangeLogService to verify functionality.
/// Note: This is a simple test class without a full test framework.
/// </summary>
public class EntityChangeLogServiceTest
{
    /// <summary>
    /// Simple test method to verify the service can be instantiated and basic operations work.
    /// Run this in a console application or debug to verify functionality.
    /// </summary>
    public static async Task TestBasicFunctionality()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        await using var context = new EventForgeDbContext(options);
        var service = new EntityChangeLogService(context);

        // Test 1: Add a change log entry
        var testEntityId = Guid.NewGuid();
        var changeLog = await service.AddChangeLogAsync(
            entityName: "TestEntity",
            entityId: testEntityId,
            operationType: "Insert",
            propertyName: "TestProperty",
            oldValue: null,
            newValue: "TestValue",
            changedBy: "TestUser",
            entityDisplayName: "Test Entity Display"
        );

        Console.WriteLine($"Test 1 - Added change log with ID: {changeLog.Id}");

        // Test 2: Get entity history
        var history = await service.GetEntityHistoryAsync("TestEntity", testEntityId);
        Console.WriteLine($"Test 2 - Retrieved {history.Count} history entries");

        // Test 3: Get last change
        var lastChange = await service.GetLastChangeAsync("TestEntity", testEntityId);
        Console.WriteLine($"Test 3 - Last change ID: {lastChange?.Id}");

        // Test 4: Get user changes
        var userChanges = await service.GetUserChangesAsync("TestUser");
        Console.WriteLine($"Test 4 - User made {userChanges.Count} changes");

        // Test 5: Get entity type history
        var typeHistory = await service.GetEntityTypeHistoryAsync("TestEntity");
        Console.WriteLine($"Test 5 - Entity type has {typeHistory.Count} history entries");

        // Test 6: Get property changes
        var propertyChanges = await service.GetPropertyChangesAsync("TestEntity", testEntityId, "TestProperty");
        Console.WriteLine($"Test 6 - Property has {propertyChanges.Count} changes");

        // Test 7: Delete entity history
        var deletedCount = await service.DeleteEntityHistoryAsync("TestEntity", testEntityId);
        Console.WriteLine($"Test 7 - Deleted {deletedCount} history entries");

        Console.WriteLine("All basic tests completed successfully!");
    }
}