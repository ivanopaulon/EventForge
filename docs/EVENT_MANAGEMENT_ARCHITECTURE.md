# Event Management Architecture - Current State

## Overview
Events in EventForge represent real-world events like festivals, parties, and gatherings (feste, sagre).

## Server Architecture (EventsController)

The server provides full CRUD operations for events via the `/api/v1/events` endpoints:

### Available Endpoints
- **GET** `/api/v1/events` - Get paginated list of events
  - Query params: `page`, `pageSize`, `deleted`
  - Returns: `PagedResult<EventDto>`

- **GET** `/api/v1/events/{id}` - Get event by ID
  - Returns: `EventDto`

- **GET** `/api/v1/events/{id}/details` - Get event details with teams and members
  - Returns: `EventDetailDto`

- **POST** `/api/v1/events` - Create new event
  - Body: `CreateEventDto`
  - Returns: `EventDto`

- **PUT** `/api/v1/events/{id}` - Update event
  - Body: `UpdateEventDto`
  - Returns: `EventDto`

- **DELETE** `/api/v1/events/{id}` - Delete event
  - Returns: 204 No Content

### Event Entity Structure
Events have the following properties:
- Name (required)
- ShortDescription (required)
- LongDescription
- Location
- StartDate (required)
- EndDate (optional)
- Capacity (min 1)
- Status (Planned, Ongoing, Completed, Cancelled)
- Teams (related collection)
- PriceLists (related collection)

**Note:** Events do NOT have EventType or EventCategory - those were client-only concepts with no server support.

## Client Architecture

### EventService
Located at: `EventForge.Client/Services/EventService.cs`

The EventService provides a clean interface to interact with the EventsController:

```csharp
public interface IEventService
{
    Task<PagedResult<EventDto>> GetEventsAsync(int page = 1, int pageSize = 20);
    Task<EventDto?> GetEventByIdAsync(Guid id);
    Task<EventDetailDto?> GetEventDetailAsync(Guid id);
    Task<EventDto> CreateEventAsync(CreateEventDto createDto);
    Task<EventDto> UpdateEventAsync(Guid id, UpdateEventDto updateDto);
    Task DeleteEventAsync(Guid id);
}
```

### Removed Features (Not Backed by Server)
The following features were removed as they had no corresponding server endpoints:

1. **Event Type Management** - Was SuperAdmin-only, no server entity or controller
2. **Event Category Management** - Was SuperAdmin-only, no server entity or controller  
3. **SuperAdmin Event Management** - Called non-existent `/api/v1/super-admin/events/*` endpoints
4. **Management Event Management** - Called non-existent SuperAdmin endpoints

## Multi-Tenancy
Events are **tenant-specific**. The EventsController enforces tenant context:
- Users can only see/manage events within their own tenant
- The `ITenantContext` is used to filter events by tenant
- SuperAdmin cannot manage events across tenants via special endpoints (those don't exist)

## Future Implementation
To implement event management UI:

1. Create a new page (e.g., `/management/events` or `/admin/events`)
2. Inject `IEventService` 
3. Use the service methods to perform CRUD operations
4. Handle pagination using the `PagedResult<EventDto>` structure
5. Ensure proper authorization (e.g., `[Authorize(Roles = "Admin,Manager")]`)

## Licensing
Event management requires the `BasicEventManagement` license feature (enforced by `[RequireLicenseFeature]` attribute on EventsController).
