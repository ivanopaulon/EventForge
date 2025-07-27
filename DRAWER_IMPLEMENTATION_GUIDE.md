# Drawer Pattern Implementation Guide

## Overview
This document outlines the implementation of parametric Drawer components in EventForge, replacing traditional dialog-based CRUD operations with modern, accessible drawer interfaces.

## EntityDrawer Component

### Core Features
- **Multi-modal support**: Create, Edit, View modes in a single component
- **Accessibility compliant**: WCAG/EAA standards with ARIA attributes
- **Responsive design**: Automatic width adjustment for mobile/tablet/desktop
- **Keyboard navigation**: ESC key support, focus management
- **Loading states**: Integrated MudProgressCircular indicators
- **Error handling**: Centralized feedback via Snackbar

### Basic Usage

```razor
<EntityDrawer @bind-IsOpen="@_drawerOpen"
              @bind-Mode="@_drawerMode"
              EntityName="User"
              Model="@_model"
              OnSave="@HandleSave"
              OnCancel="@HandleCancel"
              OnClose="@HandleClose">
    
    <FormContent>
        <!-- Your form fields here -->
    </FormContent>
    
    <ViewContent>
        <!-- Your read-only view content here -->
    </ViewContent>
    
</EntityDrawer>
```

### Advanced Parameters

```razor
<EntityDrawer @bind-IsOpen="@_drawerOpen"
              @bind-Mode="@_drawerMode"
              EntityName="User"
              CustomTitle="@_customTitle"
              Model="@_model"
              AllowEdit="true"
              ShowEditButton="true"
              Width="700px"
              OnSave="@HandleSave"
              OnCancel="@HandleCancel"
              OnClose="@HandleClose">
    
    <ActionButtons>
        <!-- Custom action buttons -->
        <MudButton OnClick="@CustomAction">Custom Action</MudButton>
    </ActionButtons>
    
</EntityDrawer>
```

## Specialized Drawer Components

### UserDrawer
Complete CRUD implementation for user management:

```razor
<UserDrawer @bind-IsOpen="@_userDrawerOpen" 
            @bind-Mode="@_userDrawerMode"
            OriginalUser="@_selectedUser"
            AllowEdit="true"
            OnUserCreated="@OnUserCreated"
            OnUserUpdated="@OnUserUpdated" />
```

Features:
- Automatic tenant loading
- Role management with checkboxes
- Password validation for creation
- User status management
- Accessibility descriptions for all fields

### TenantDrawer
Complete CRUD implementation for tenant management:

```razor
<TenantDrawer @bind-IsOpen="@_tenantDrawerOpen"
              @bind-Mode="@_tenantDrawerMode" 
              OriginalTenant="@_selectedTenant"
              AllowEdit="true"
              OnTenantCreated="@OnTenantCreated"
              OnTenantUpdated="@OnTenantUpdated" />
```

Features:
- User count tracking
- Usage percentage visualization
- Domain validation
- Contact email validation
- Max users configuration

## Accessibility Implementation

### WCAG/EAA Compliance Features

1. **ARIA Attributes**
   ```razor
   <MudTextField aria-describedby="field-help"
                 aria-label="User name input field" />
   <MudText id="field-help" Typo="Typo.caption">
       Help text for screen readers
   </MudText>
   ```

2. **Keyboard Navigation**
   - ESC key closes drawer
   - Tab/Shift+Tab navigation
   - Focus management between modes
   
3. **Screen Reader Support**
   ```razor
   <div role="dialog" 
        aria-modal="true" 
        aria-labelledby="drawer-title"
        aria-describedby="drawer-content">
   ```

4. **Loading States**
   ```razor
   <div aria-live="polite" aria-label="Loading operation">
       <MudProgressCircular />
       <MudText>Loading...</MudText>
   </div>
   ```

### Accessibility Checklist

- [ ] All form fields have proper labels
- [ ] Helper text is associated with aria-describedby  
- [ ] Focus moves to first field on open
- [ ] ESC key closes drawer
- [ ] Loading states are announced
- [ ] Error messages are accessible
- [ ] Color is not the only means of communication
- [ ] Sufficient color contrast ratios
- [ ] Works with keyboard only
- [ ] Compatible with screen readers

## Responsive Design

### Breakpoint Strategy

```css
/* Mobile: Full width */
@media (max-width: 768px) {
    .entity-drawer {
        width: 100vw !important;
    }
}

/* Tablet: 80% width, max 600px */
@media (min-width: 769px) and (max-width: 1024px) {
    .entity-drawer {
        width: 80vw !important;
        max-width: 600px !important;
    }
}

/* Desktop: Fixed width or custom */
@media (min-width: 1025px) {
    .entity-drawer {
        width: var(--drawer-width, 700px);
    }
}
```

### Responsive Width Function

```csharp
private string GetResponsiveWidth()
{
    if (!string.IsNullOrEmpty(Width))
        return Width;
        
    // Responsive width based on screen size  
    return "min(90vw, 700px)";
}
```

## HttpClient Centralization

### HttpClientService Implementation

```csharp
public interface IHttpClientService
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default);
    Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
}
```

### Features
- Automatic authentication header management
- Centralized error handling with ProblemDetails support
- Request correlation IDs for tracking
- Configurable JSON serialization options
- Consistent logging across all HTTP operations

### Registration in Program.cs

```csharp
// Configure HttpClient with factory pattern
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "EventForge-Client/1.0");
});

// Register centralized service
builder.Services.AddScoped<IHttpClientService, HttpClientService>();
```

## Best Practices

### Component Structure
1. Use EntityDrawer as base for consistent behavior
2. Create specialized components for complex entities
3. Implement proper parameter validation
4. Handle loading and error states gracefully

### State Management
```csharp
// Page-level state
private bool _drawerOpen = false;
private EntityDrawerMode _drawerMode = EntityDrawerMode.Create;
private UserDto? _selectedUser = null;

// Mode switching
private async Task OpenCreateDrawer()
{
    _drawerMode = EntityDrawerMode.Create;
    _selectedUser = null;
    _drawerOpen = true;
}

private async Task OpenEditDrawer(UserDto user)
{
    _drawerMode = EntityDrawerMode.Edit; 
    _selectedUser = user;
    _drawerOpen = true;
}
```

### Error Handling
```csharp
try
{
    await SaveOperation();
    Snackbar.Add(Messages.SaveSuccessful, Severity.Success);
    await HandleClose();
}
catch (HttpRequestException ex) when (ex.Data.ContainsKey("ProblemDetails"))
{
    var problem = (ProblemDetailsDto)ex.Data["ProblemDetails"];
    Snackbar.Add(problem.Title ?? "Operation failed", Severity.Error);
}
catch (Exception ex)
{
    Logger.LogError(ex, "Unexpected error during save operation");
    Snackbar.Add("An unexpected error occurred", Severity.Error);
}
```

## Testing Strategy

### Unit Tests
- Test drawer mode switching
- Validate form submission logic
- Check accessibility attributes
- Test responsive behavior

### Integration Tests  
- Test complete CRUD workflows
- Validate API integration
- Test error scenarios
- Check state management

### Accessibility Tests
- Automated WAVE/axe testing
- Manual keyboard navigation testing
- Screen reader compatibility testing
- Color contrast validation

## Migration Guide

### From Dialog to Drawer

1. **Replace Dialog Components**
   ```csharp
   // Old approach
   var dialog = await DialogService.ShowAsync<CreateUserDialog>();
   var result = await dialog.Result;
   
   // New approach
   _userDrawerMode = EntityDrawerMode.Create;
   _userDrawerOpen = true;
   ```

2. **Update Event Handling**
   ```csharp
   // Old approach - dialog result handling
   if (result?.Data is UserDto user)
   {
       // Handle result
   }
   
   // New approach - direct callbacks
   private async Task OnUserCreated(UserDto user)
   {
       // Handle creation
   }
   ```

3. **State Management Migration**
   ```csharp
   // Add drawer state fields
   private bool _drawerOpen = false;
   private EntityDrawerMode _drawerMode = EntityDrawerMode.Create;
   private UserDto? _selectedUser = null;
   ```

## Performance Considerations

### Lazy Loading
- Load dropdown data only when drawer opens
- Implement pagination for large datasets
- Cache frequently accessed data

### Optimization
```csharp
protected override async Task OnParametersSetAsync()
{
    // Only reload data when actually needed
    if (Mode == EntityDrawerMode.Edit && OriginalUser?.Id != _previousUserId)
    {
        await LoadUserSpecificData();
        _previousUserId = OriginalUser?.Id;
    }
}
```

### Memory Management
- Dispose of subscriptions in drawer components
- Clear large object references when closing
- Implement proper cleanup in IAsyncDisposable

This implementation provides a robust, accessible, and maintainable foundation for CRUD operations throughout the EventForge application.