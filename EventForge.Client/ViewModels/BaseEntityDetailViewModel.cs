using System.Text.Json;

namespace EventForge.Client.ViewModels;

/// <summary>
/// Base ViewModel for entity detail pages providing common functionality
/// </summary>
/// <typeparam name="TDto">The DTO type for the entity</typeparam>
/// <typeparam name="TCreateDto">The create DTO type</typeparam>
/// <typeparam name="TUpdateDto">The update DTO type</typeparam>
public abstract class BaseEntityDetailViewModel<TDto, TCreateDto, TUpdateDto> : IDisposable
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    protected readonly ILogger Logger;
    private string _originalSnapshot = string.Empty;
    private bool _disposed;

    protected BaseEntityDetailViewModel(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Current entity being edited
    /// </summary>
    public TDto? Entity { get; protected set; }

    /// <summary>
    /// Indicates if this is a new entity
    /// </summary>
    public bool IsNewEntity { get; protected set; }

    /// <summary>
    /// Indicates if the entity is currently loading
    /// </summary>
    public bool IsLoading { get; protected set; }

    /// <summary>
    /// Indicates if the entity is currently being saved
    /// </summary>
    public bool IsSaving { get; protected set; }

    /// <summary>
    /// Event raised when the ViewModel state changes
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Loads an entity by its ID
    /// </summary>
    public async Task LoadEntityAsync(Guid entityId)
    {
        IsLoading = true;
        NotifyStateChanged();

        try
        {
            if (entityId == Guid.Empty)
            {
                IsNewEntity = true;
                Entity = CreateNewEntity();
            }
            else
            {
                IsNewEntity = false;
                Entity = await LoadEntityFromServiceAsync(entityId);
                if (Entity != null)
                {
                    await LoadRelatedEntitiesAsync(entityId);
                }
            }

            _originalSnapshot = SerializeEntity(Entity);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading entity {EntityId}", entityId);
            throw;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Saves the current entity (create or update)
    /// </summary>
    public async Task<bool> SaveEntityAsync()
    {
        if (Entity == null) return false;

        IsSaving = true;
        NotifyStateChanged();

        try
        {
            if (IsNewEntity)
            {
                var createDto = MapToCreateDto(Entity);
                var created = await CreateEntityAsync(createDto);
                if (created != null)
                {
                    Entity = created;
                    IsNewEntity = false;
                    _originalSnapshot = SerializeEntity(Entity);
                    return true;
                }
                return false;
            }
            else
            {
                var updateDto = MapToUpdateDto(Entity);
                var entityId = GetEntityId(Entity);
                var updated = await UpdateEntityAsync(entityId, updateDto);
                if (updated != null)
                {
                    Entity = updated;
                    _originalSnapshot = SerializeEntity(Entity);
                    return true;
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving entity");
            throw;
        }
        finally
        {
            IsSaving = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Checks if there are unsaved changes
    /// </summary>
    public bool HasUnsavedChanges()
    {
        if (Entity == null) return false;
        var current = SerializeEntity(Entity);
        return !string.Equals(current, _originalSnapshot, StringComparison.Ordinal);
    }

    /// <summary>
    /// Reloads the entity from the service
    /// </summary>
    public async Task ReloadEntityAsync()
    {
        if (Entity == null || IsNewEntity) return;
        var entityId = GetEntityId(Entity);
        await LoadEntityAsync(entityId);
    }

    /// <summary>
    /// Notifies that the entity has been changed locally
    /// </summary>
    public void NotifyEntityChanged()
    {
        NotifyStateChanged();
    }

    protected void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    private string SerializeEntity(TDto? entity)
    {
        if (entity == null) return string.Empty;
        return JsonSerializer.Serialize(entity, JsonOptions);
    }

    #region Abstract Methods - Must be implemented by derived classes

    /// <summary>
    /// Creates a new empty entity instance
    /// </summary>
    protected abstract TDto CreateNewEntity();

    /// <summary>
    /// Loads entity from the service
    /// </summary>
    protected abstract Task<TDto?> LoadEntityFromServiceAsync(Guid entityId);

    /// <summary>
    /// Loads related entities (optional)
    /// </summary>
    protected virtual Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Maps entity to create DTO
    /// </summary>
    protected abstract TCreateDto MapToCreateDto(TDto entity);

    /// <summary>
    /// Maps entity to update DTO
    /// </summary>
    protected abstract TUpdateDto MapToUpdateDto(TDto entity);

    /// <summary>
    /// Creates entity via service
    /// </summary>
    protected abstract Task<TDto?> CreateEntityAsync(TCreateDto createDto);

    /// <summary>
    /// Updates entity via service
    /// </summary>
    protected abstract Task<TDto?> UpdateEntityAsync(Guid entityId, TUpdateDto updateDto);

    /// <summary>
    /// Gets the entity ID
    /// </summary>
    protected abstract Guid GetEntityId(TDto entity);

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Cleanup managed resources
            }
            _disposed = true;
        }
    }
}
