# Usage Examples: ManagementTableToolbar & AuditHistoryDialog

This document provides practical examples for using the new foundation components created for issue #540.

## ManagementTableToolbar Usage Examples

### Example 1: Basic Usage (Refresh & Create)

```razor
@page "/example-management"
@inject IExampleService ExampleService
@inject ITranslationService TranslationService

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudPaper Elevation="2" Class="pa-4">
        <!-- Toolbar -->
        <ManagementTableToolbar ShowRefresh="true"
                                ShowCreate="true"
                                ShowDelete="false"
                                OnRefresh="@LoadDataAsync"
                                OnCreate="@OpenCreateDialog" />
        
        <!-- Your table here -->
        <MudTable T="ExampleDto" Items="_items">
            <!-- Table content -->
        </MudTable>
    </MudPaper>
</MudContainer>

@code {
    private List<ExampleDto> _items = new();
    
    private async Task LoadDataAsync()
    {
        _items = await ExampleService.GetAllAsync();
    }
    
    private void OpenCreateDialog()
    {
        // Open your create dialog
    }
}
```

### Example 2: With Selection & Delete

```razor
<ManagementTableToolbar ShowSelectionBadge="true"
                        SelectedCount="_selectedItems.Count"
                        ShowRefresh="true"
                        ShowCreate="true"
                        ShowDelete="true"
                        IsDisabled="_isDeleting"
                        OnRefresh="@LoadDataAsync"
                        OnCreate="@OpenCreateDialog"
                        OnDelete="@DeleteSelectedAsync" />

<MudTable T="BrandDto" 
          Items="_brands"
          @bind-SelectedItems="_selectedItems"
          MultiSelection="true">
    <!-- Table columns -->
</MudTable>

@code {
    private HashSet<BrandDto> _selectedItems = new();
    private bool _isDeleting = false;
    
    private async Task DeleteSelectedAsync()
    {
        try
        {
            _isDeleting = true;
            StateHasChanged();
            
            foreach (var item in _selectedItems)
            {
                await BrandService.DeleteAsync(item.Id);
            }
            
            Snackbar.Add("Eliminazione completata", Severity.Success);
            _selectedItems.Clear();
            await LoadDataAsync();
        }
        finally
        {
            _isDeleting = false;
            StateHasChanged();
        }
    }
}
```

### Example 3: Custom Labels & Icons

```razor
<ManagementTableToolbar ShowRefresh="true"
                        ShowCreate="true"
                        CreateLabel="brand.createNew"
                        CreateTooltip="brand.createNewTooltip"
                        CreateIcon="@Icons.Material.Outlined.Sell"
                        RefreshTooltip="brand.refreshData"
                        OnRefresh="@LoadDataAsync"
                        OnCreate="@OpenCreateDialog" />
```

### Example 4: With Additional Custom Actions

```razor
<ManagementTableToolbar ShowRefresh="true"
                        ShowCreate="true"
                        OnRefresh="@LoadDataAsync"
                        OnCreate="@OpenCreateDialog">
    <AdditionalActions>
        <MudTooltip Text="Esporta in Excel">
            <MudIconButton Icon="@Icons.Material.Outlined.Download"
                           Color="Color.Success"
                           OnClick="@ExportToExcel"
                           Size="Size.Small" />
        </MudTooltip>
        <MudTooltip Text="Importa dati">
            <MudIconButton Icon="@Icons.Material.Outlined.Upload"
                           Color="Color.Info"
                           OnClick="@ImportData"
                           Size="Size.Small" />
        </MudTooltip>
    </AdditionalActions>
</ManagementTableToolbar>

@code {
    private async Task ExportToExcel()
    {
        // Export logic
    }
    
    private async Task ImportData()
    {
        // Import logic
    }
}
```

---

## AuditHistoryDialog Usage Examples

### Example 1: Basic Usage with Placeholder

```razor
@page "/product-detail/{ProductId:guid}"
@inject ITranslationService TranslationService

<MudButton OnClick="@(() => _showAuditHistory = true)">
    Visualizza Cronologia
</MudButton>

<AuditHistoryDialog IsOpen="_showAuditHistory"
                    IsOpenChanged="@((value) => _showAuditHistory = value)"
                    EntityType="Product"
                    EntityId="_productId"
                    EntityName="@_productName" />

@code {
    [Parameter] public Guid ProductId { get; set; }
    
    private bool _showAuditHistory = false;
    private Guid _productId;
    private string _productName = "Example Product";
    
    protected override void OnInitialized()
    {
        _productId = ProductId;
    }
}
```

### Example 2: With Custom Content

```razor
<AuditHistoryDialog IsOpen="_showAuditHistory"
                    IsOpenChanged="@((value) => _showAuditHistory = value)"
                    EntityType="Brand"
                    EntityId="_brandId"
                    EntityName="@_brand?.Name"
                    IsLoading="_isLoadingHistory">
    <ContentTemplate>
        @if (_auditLogs.Any())
        {
            <MudTimeline TimelineOrientation="TimelineOrientation.Vertical">
                @foreach (var log in _auditLogs)
                {
                    <MudTimelineItem Color="Color.Primary">
                        <ItemContent>
                            <MudText Typo="Typo.body1">@log.Action</MudText>
                            <MudText Typo="Typo.caption">@log.Timestamp.ToString("dd/MM/yyyy HH:mm")</MudText>
                            <MudText Typo="Typo.body2">@log.UserName</MudText>
                        </ItemContent>
                    </MudTimelineItem>
                }
            </MudTimeline>
        }
        else
        {
            <MudText Align="Align.Center" Class="pa-4">
                Nessuna modifica trovata
            </MudText>
        }
    </ContentTemplate>
</AuditHistoryDialog>

@code {
    private bool _showAuditHistory = false;
    private bool _isLoadingHistory = false;
    private List<AuditLogDto> _auditLogs = new();
    private Guid _brandId;
    private BrandDto? _brand;
    
    protected override async Task OnParametersSetAsync()
    {
        if (_showAuditHistory)
        {
            await LoadAuditHistoryAsync();
        }
    }
    
    private async Task LoadAuditHistoryAsync()
    {
        try
        {
            _isLoadingHistory = true;
            StateHasChanged();
            
            _auditLogs = await AuditService.GetHistoryAsync(_brandId);
        }
        finally
        {
            _isLoadingHistory = false;
            StateHasChanged();
        }
    }
}
```

### Example 3: With Additional Action Buttons

```razor
<AuditHistoryDialog IsOpen="_showAuditHistory"
                    IsOpenChanged="@((value) => _showAuditHistory = value)"
                    EntityType="Product"
                    EntityId="_productId"
                    EntityName="@_productName"
                    OnClose="@OnDialogClose">
    <ContentTemplate>
        <!-- Your audit history content -->
    </ContentTemplate>
    <ActionButtons>
        <MudButton Variant="Variant.Text"
                   Color="Color.Primary"
                   StartIcon="@Icons.Material.Outlined.Download"
                   OnClick="@ExportAuditHistory">
            @TranslationService.GetTranslation("audit.export", "Esporta")
        </MudButton>
        <MudButton Variant="Variant.Text"
                   Color="Color.Info"
                   StartIcon="@Icons.Material.Outlined.Print"
                   OnClick="@PrintAuditHistory">
            @TranslationService.GetTranslation("common.print", "Stampa")
        </MudButton>
    </ActionButtons>
</AuditHistoryDialog>

@code {
    private async Task ExportAuditHistory()
    {
        // Export logic
    }
    
    private async Task PrintAuditHistory()
    {
        // Print logic
    }
    
    private void OnDialogClose()
    {
        // Cleanup logic
    }
}
```

---

## Combined Usage Example: Complete Management Page

```razor
@page "/brand-management"
@using EventForge.DTOs.Products
@inject IBrandService BrandService
@inject ITranslationService TranslationService
@inject ISnackbar Snackbar
@inject IDialogService DialogService

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <!-- Page Loading Overlay -->
    <PageLoadingOverlay IsVisible="_isLoading || _isDeleting"
                        Message="@GetLoadingMessage()" />

    @if (!_isLoading)
    {
        <MudPaper Elevation="2" Class="pa-4 mb-4">
            <div class="d-flex justify-space-between align-center mb-4">
                <div>
                    <MudText Typo="Typo.h4">
                        <MudIcon Icon="@Icons.Material.Outlined.Sell" Class="mr-2" />
                        @TranslationService.GetTranslation("brand.management", "Gestione Marchi")
                    </MudText>
                    <MudText Typo="Typo.body2" Class="mud-text-secondary mt-2">
                        @TranslationService.GetTranslation("brand.managementDescription", "Gestisci i marchi dei prodotti")
                    </MudText>
                </div>
            </div>

            <!-- Management Toolbar -->
            <ManagementTableToolbar ShowSelectionBadge="true"
                                    SelectedCount="_selectedBrands.Count"
                                    ShowRefresh="true"
                                    ShowCreate="true"
                                    ShowDelete="true"
                                    IsDisabled="_isDeleting"
                                    CreateLabel="brand.createNew"
                                    CreateTooltip="brand.createNewTooltip"
                                    CreateIcon="@Icons.Material.Outlined.Sell"
                                    OnRefresh="@LoadBrandsAsync"
                                    OnCreate="@CreateBrand"
                                    OnDelete="@DeleteSelectedBrandsAsync">
                <AdditionalActions>
                    <MudTooltip Text="Esporta in Excel">
                        <MudIconButton Icon="@Icons.Material.Outlined.Download"
                                       Color="Color.Success"
                                       OnClick="@ExportToExcel"
                                       Size="Size.Small" />
                    </MudTooltip>
                </AdditionalActions>
            </ManagementTableToolbar>

            <!-- Data Table -->
            <MudTable T="BrandDto" 
                      Items="_brands"
                      @bind-SelectedItems="_selectedBrands"
                      MultiSelection="true"
                      Hover="true"
                      Striped="true">
                <HeaderContent>
                    <MudTh>Nome</MudTh>
                    <MudTh>Descrizione</MudTh>
                    <MudTh>Azioni</MudTh>
                </HeaderContent>
                <RowTemplate>
                    <MudTd>@context.Name</MudTd>
                    <MudTd>@context.Description</MudTd>
                    <MudTd>
                        <MudIconButton Icon="@Icons.Material.Outlined.History"
                                       Size="Size.Small"
                                       OnClick="@(() => ShowAuditHistory(context))" />
                    </MudTd>
                </RowTemplate>
            </MudTable>
        </MudPaper>
    }

    <!-- Audit History Dialog -->
    <AuditHistoryDialog IsOpen="_showAuditDialog"
                        IsOpenChanged="@((value) => _showAuditDialog = value)"
                        EntityType="Brand"
                        EntityId="_selectedBrandForAudit?.Id"
                        EntityName="@_selectedBrandForAudit?.Name"
                        IsLoading="_isLoadingAudit">
        <ContentTemplate>
            @if (_auditLogs.Any())
            {
                <MudTimeline TimelineOrientation="TimelineOrientation.Vertical">
                    @foreach (var log in _auditLogs)
                    {
                        <MudTimelineItem Color="Color.Primary">
                            <ItemContent>
                                <MudText Typo="Typo.body1">@log.Action</MudText>
                                <MudText Typo="Typo.caption">@log.Timestamp.ToString("dd/MM/yyyy HH:mm")</MudText>
                            </ItemContent>
                        </MudTimelineItem>
                    }
                </MudTimeline>
            }
        </ContentTemplate>
    </AuditHistoryDialog>
</MudContainer>

@code {
    private bool _isLoading = true;
    private bool _isDeleting = false;
    private bool _showAuditDialog = false;
    private bool _isLoadingAudit = false;
    
    private List<BrandDto> _brands = new();
    private HashSet<BrandDto> _selectedBrands = new();
    private BrandDto? _selectedBrandForAudit;
    private List<AuditLogDto> _auditLogs = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadBrandsAsync();
    }

    private async Task LoadBrandsAsync()
    {
        try
        {
            _isLoading = true;
            StateHasChanged();
            
            _brands = await BrandService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task DeleteSelectedBrandsAsync()
    {
        if (!_selectedBrands.Any())
            return;

        var result = await DialogService.ShowMessageBox(
            "Conferma eliminazione",
            $"Sei sicuro di voler eliminare {_selectedBrands.Count} marchi?",
            yesText: "Elimina", 
            cancelText: "Annulla");

        if (result != true)
            return;

        try
        {
            _isDeleting = true;
            StateHasChanged();
            
            foreach (var brand in _selectedBrands)
            {
                await BrandService.DeleteAsync(brand.Id);
            }
            
            Snackbar.Add("Eliminazione completata", Severity.Success);
            _selectedBrands.Clear();
            await LoadBrandsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isDeleting = false;
            StateHasChanged();
        }
    }

    private void CreateBrand()
    {
        // Open create brand dialog
    }

    private async Task ShowAuditHistory(BrandDto brand)
    {
        _selectedBrandForAudit = brand;
        _showAuditDialog = true;
        
        try
        {
            _isLoadingAudit = true;
            StateHasChanged();
            
            // Load audit history
            _auditLogs = await AuditService.GetHistoryAsync(brand.Id);
        }
        finally
        {
            _isLoadingAudit = false;
            StateHasChanged();
        }
    }

    private async Task ExportToExcel()
    {
        // Export implementation
    }

    private string GetLoadingMessage()
    {
        if (_isDeleting)
            return TranslationService.GetTranslation("messages.deleting", "Eliminazione in corso...");
        if (_isLoading)
            return TranslationService.GetTranslation("messages.loadingPage", "Caricamento pagina...");
        return string.Empty;
    }
}
```

---

## Translation Keys Reference

### Required Translation Keys for ManagementTableToolbar

```json
{
  "toolbar": {
    "itemsSelected": "{0} elementi selezionati"
  },
  "button": {
    "delete": "Elimina",
    "create": "Crea"
  },
  "tooltip": {
    "refresh": "Aggiorna dati",
    "create": "Crea nuovo elemento"
  }
}
```

### Required Translation Keys for AuditHistoryDialog

```json
{
  "audit": {
    "historyDialog": {
      "title": "Cronologia Modifiche"
    },
    "loadingHistory": "Caricamento cronologia...",
    "noHistoryAvailable": "Nessuna cronologia disponibile",
    "noHistoryDescription": "Non sono state trovate modifiche per questo elemento."
  },
  "button": {
    "close": "Chiudi"
  }
}
```

---

## Notes

- All components are fully compatible with MudBlazor theming
- TranslationService integration is mandatory for all text
- Follow the PageLoadingOverlay guidelines when using these components together
- Components are designed to be minimal and non-breaking to existing code
