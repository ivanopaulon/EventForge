# LookupCacheService Usage Guide

## Overview

The `LookupCacheService` has been refactored to provide robust error handling, structured error propagation, and automatic retry logic for transient failures. This guide shows how to use the service in both legacy and modern patterns.

## Key Features

✅ **Structured Error Handling** - Return `LookupResult<T>` with success state and detailed error information  
✅ **Polly Retry Logic** - Automatic retry (3 attempts with exponential backoff) for transient network errors  
✅ **Smart Caching** - Only cache successful results, never cache errors  
✅ **Comprehensive Logging** - Error, Warning, and Info levels for different scenarios  
✅ **Backward Compatibility** - Legacy "Raw" methods for gradual migration  

## Architecture

### LookupResult<T> Wrapper

```csharp
public record LookupResult<T>(
    bool Success,                      // True if operation succeeded
    IReadOnlyCollection<T> Items,      // The actual data
    string? ErrorCode = null,          // Error code (e.g., "NULL_RESPONSE", "UNHANDLED_EXCEPTION")
    string? ErrorMessage = null,       // Human-readable error message
    bool IsTransient = false)          // True if error is transient (network issue)
```

### Retry Policy

```csharp
// Retry 3 times with exponential backoff
// Delays: 200ms, 500ms, 1000ms
// Only retries on: HttpRequestException, TaskCanceledException
```

## Usage Patterns

### Pattern 1: Legacy/Backward Compatible (Using Raw Methods)

Current UI code continues to work without changes by using the `RawAsync` methods:

```csharp
// Existing code pattern - still works
var brands = await LookupCacheService.GetBrandsRawAsync();
_brands = brands.ToList();

var models = await LookupCacheService.GetModelsRawAsync(brandId);
_models = models.ToList();

var vatRates = await LookupCacheService.GetVatRatesRawAsync();
_vatRates = vatRates.ToList();

var units = await LookupCacheService.GetUnitsOfMeasureRawAsync();
_unitsOfMeasure = units.ToList();
```

### Pattern 2: Modern with Error Handling (Recommended)

New code should use the full `LookupResult<T>` to provide better user feedback:

```csharp
@code {
    private LookupResult<BrandDto>? _brandResult;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _brandResult = await LookupCacheService.GetBrandsAsync();
        _loading = false;
        StateHasChanged();
    }
}
```

### Pattern 3: UI Component with Complete Error States

Example of a MudSelect with full error handling:

```razor
<MudSelect T="Guid?"
           @bind-Value="SelectedBrandId"
           Label="Brand"
           Variant="Variant.Outlined"
           Disabled="@(_loading || _brandResult is { Success: false, IsTransient: true })">
    @if (_loading)
    {
        <MudProgressCircular Size="Size.Small" Indeterminate="true" />
        <MudText Typo="Typo.caption">Loading...</MudText>
    }
    else if (_brandResult is not null && !_brandResult.Success)
    {
        @if (_brandResult.IsTransient)
        {
            <MudAlert Severity="Severity.Warning" Dense="true">
                @(_brandResult.ErrorMessage ?? "Temporary connection issue")
                <MudButton OnClick="@(() => ReloadBrands())" Size="Size.Small" Color="Color.Primary">
                    Retry
                </MudButton>
            </MudAlert>
        }
        else
        {
            <MudAlert Severity="Severity.Error" Dense="true">
                @(_brandResult.ErrorMessage ?? "Error loading brands")
            </MudAlert>
        }
    }
    else if (_brandResult is not null && _brandResult.Items.Count == 0)
    {
        <MudText Typo="Typo.caption" Color="Color.Secondary">No brands available</MudText>
    }
    else if (_brandResult is not null)
    {
        @foreach (var brand in _brandResult.Items)
        {
            <MudSelectItem T="Guid?" Value="@((Guid?)brand.Id)">@brand.Name</MudSelectItem>
        }
    }
</MudSelect>

@code {
    private LookupResult<BrandDto>? _brandResult;
    private bool _loading = true;
    private Guid? SelectedBrandId;

    protected override async Task OnInitializedAsync()
    {
        await LoadBrands();
    }

    private async Task LoadBrands()
    {
        _loading = true;
        _brandResult = await LookupCacheService.GetBrandsAsync();
        _loading = false;
        StateHasChanged();
    }

    private async Task ReloadBrands()
    {
        await LoadBrands();
    }
}
```

### Pattern 4: Simplified Error Handling

For simpler UI components, check only success state:

```csharp
private async Task LoadData()
{
    try
    {
        var result = await LookupCacheService.GetBrandsAsync();
        
        if (result.Success)
        {
            _brands = result.Items.ToList();
        }
        else
        {
            // Show generic error
            Snackbar.Add(result.ErrorMessage ?? "Failed to load brands", 
                result.IsTransient ? Severity.Warning : Severity.Error);
            
            // Optionally use legacy empty list
            _brands = new List<BrandDto>();
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Unexpected error loading brands");
        Snackbar.Add("An unexpected error occurred", Severity.Error);
    }
}
```

## Error Codes

The service uses the following error codes:

| Error Code | Description | Is Transient |
|------------|-------------|--------------|
| `NULL_RESPONSE` | API returned null | Yes |
| `UNHANDLED_EXCEPTION` | Unexpected exception occurred | No |
| (Network errors) | HttpRequestException, TaskCanceledException | Yes (auto-retried) |

## Logging Behavior

The service logs at different levels:

- **Info**: Successful data loads (e.g., "Loaded 5 brands (Total=5)")
- **Warning**: Transient failures, retry attempts (e.g., "Transient lookup failure on attempt 2")
- **Error**: Unrecoverable failures (e.g., "Unrecoverable brands error")
- **Debug**: Cache invalidation (e.g., "forceRefresh invalidated brands cache")

## Migration Guide

### Step 1: Update Service Calls

Replace old method calls with `RawAsync` versions:

```diff
- var brands = await LookupCacheService.GetBrandsAsync();
+ var brands = await LookupCacheService.GetBrandsRawAsync();
```

### Step 2: Gradually Migrate to LookupResult

Once working, update to use full result:

```diff
- var brands = await LookupCacheService.GetBrandsRawAsync();
- _brands = brands.ToList();
+ var result = await LookupCacheService.GetBrandsAsync();
+ if (result.Success)
+ {
+     _brands = result.Items.ToList();
+ }
+ else
+ {
+     // Handle error appropriately
+ }
```

### Step 3: Add UI Error States

Update UI to show error states:

```diff
  <MudSelect T="Guid?" ...>
+     @if (_brandResult is { Success: false })
+     {
+         <MudAlert Severity="Severity.Warning">
+             @_brandResult.ErrorMessage
+         </MudAlert>
+     }
      @foreach (var brand in _brands)
      {
          ...
      }
  </MudSelect>
```

## Best Practices

1. **Always check Success flag** before using Items
2. **Show appropriate UI feedback** for errors
3. **Use IsTransient** to determine if user can retry
4. **Log unexpected errors** at appropriate levels
5. **Don't catch and suppress errors** - let the service handle them
6. **Use forceRefresh** sparingly - only when user explicitly requests it
7. **Provide retry buttons** for transient errors

## Cache Behavior

- **Success results**: Cached for 10 minutes
- **Failed results**: Never cached (always re-attempted)
- **Force refresh**: Invalidates cache immediately
- **Cache keys**: Separate for each lookup type and brand-filtered models

## Performance Optimization

The service includes automatic performance optimizations:

1. **Cached Results**: Hit cache first, avoiding API calls
2. **Retry Logic**: Only retries transient errors (network issues)
3. **No Cached Errors**: Failed lookups don't poison the cache
4. **Exponential Backoff**: Reduces load during temporary issues

## Example: Complete Implementation

```razor
@inject ILookupCacheService LookupCacheService
@inject ISnackbar Snackbar

<MudPaper Class="pa-4">
    <MudSelect T="Guid?" 
               @bind-Value="Product.BrandId"
               Label="Brand"
               Variant="Variant.Outlined">
        @if (_loading)
        {
            <MudProgressLinear Indeterminate="true" />
        }
        else if (_brandResult is not null && !_brandResult.Success)
        {
            <MudAlert Severity="@(_brandResult.IsTransient ? Severity.Warning : Severity.Error)" Dense="true">
                <MudText>@_brandResult.ErrorMessage</MudText>
                @if (_brandResult.IsTransient)
                {
                    <MudButton OnClick="ReloadBrands" Size="Size.Small">Retry</MudButton>
                }
            </MudAlert>
        }
        else if (_brandResult?.Items.Count == 0)
        {
            <MudText Typo="Typo.caption">No brands available</MudText>
        }
        else
        {
            @foreach (var brand in _brandResult?.Items ?? Enumerable.Empty<BrandDto>())
            {
                <MudSelectItem T="Guid?" Value="@((Guid?)brand.Id)">@brand.Name</MudSelectItem>
            }
        }
    </MudSelect>
</MudPaper>

@code {
    [Parameter] public ProductDto Product { get; set; } = new();
    
    private LookupResult<BrandDto>? _brandResult;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadBrands();
    }

    private async Task LoadBrands()
    {
        _loading = true;
        StateHasChanged();
        
        try
        {
            _brandResult = await LookupCacheService.GetBrandsAsync();
            
            if (!_brandResult.Success && !_brandResult.IsTransient)
            {
                // Only show snackbar for permanent errors
                Snackbar.Add(_brandResult.ErrorMessage ?? "Failed to load brands", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error in LoadBrands");
            Snackbar.Add("An unexpected error occurred", Severity.Error);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task ReloadBrands()
    {
        await LoadBrands();
    }
}
```

## Testing

The service includes comprehensive unit tests covering:

- Success scenarios with valid data
- Transient failures (null responses)
- Unhandled exceptions
- Caching behavior (successful results cached, failures not cached)
- Force refresh invalidation
- Backward-compatible Raw methods
- Brand-filtered models
- All lookup types (Brands, Models, VAT Rates, Units)

Run tests with:
```bash
dotnet test --filter "FullyQualifiedName~LookupCacheServiceTests"
```

## Summary

The refactored `LookupCacheService` provides:

✅ No more silent empty caching after errors  
✅ UI can distinguish between error vs. 'no data'  
✅ All logs are clear and actionable  
✅ Polly retries only on transient exceptions  
✅ Backward compatibility for gradual migration  
✅ Comprehensive test coverage  
