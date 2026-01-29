namespace EventForge.Server.Exceptions;

/// <summary>
/// Exception thrown when a requested entity is not found
/// </summary>
public class NotFoundException : EventForgeException
{
    public string EntityName { get; }
    public object EntityId { get; }
    
    public NotFoundException(string entityName, object entityId) 
        : base($"{entityName} with ID '{entityId}' was not found")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
    
    public NotFoundException(string message) : base(message) { }
}
