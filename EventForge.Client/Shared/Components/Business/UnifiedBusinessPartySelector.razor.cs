using EventForge.Client.Services;
using EventForge.Client.Shared.Components.Common;
using EventForge.Client.Shared.Components.Dialogs.Business;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EventForge.Client.Shared.Components.Business
{
    /// <summary>
    /// Unified component for Business Party search and display.
    /// Follows the same pattern as UnifiedProductScanner.
    /// Supports progressive enhancement for business party groups (badges).
    /// </summary>
    public partial class UnifiedBusinessPartySelector : ComponentBase
    {
        [Inject] private IBusinessPartyService BusinessPartyService { get; set; } = null!;
        [Inject] private ITranslationService TranslationService { get; set; } = null!;
        [Inject] private ILogger<UnifiedBusinessPartySelector> Logger { get; set; } = null!;
        [Inject] private IDialogService DialogService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;

        #region Parameters - Appearance

        /// <summary>
        /// Title to display at the top of the component.
        /// Set to null to hide the title section entirely.
        /// </summary>
        [Parameter] public string? Title { get; set; } = "Business Party";
        [Parameter] public string Placeholder { get; set; } = "Cerca per nome, P.IVA o C.F...";
        [Parameter] public bool Dense { get; set; } = true;
        [Parameter] public string? Class { get; set; }
        [Parameter] public string? Style { get; set; }

        #endregion

        #region Parameters - Display

        [Parameter] public bool ShowGroups { get; set; } = true;
        [Parameter] public bool ShowGroupPriority { get; set; } = false;
        [Parameter] public bool ShowFiscalInfo { get; set; } = true;
        [Parameter] public bool ShowLocation { get; set; } = true;
        [Parameter] public bool ShowContactStats { get; set; } = true;
        [Parameter] public bool ShowEditButton { get; set; } = false;
        [Parameter] public bool GroupClickable { get; set; } = false;
        [Parameter] public int MaxVisibleGroupsInAutocomplete { get; set; } = 2;

        #endregion

        #region Parameters - Search

        [Parameter] public BusinessPartyType? FilterByType { get; set; }
        [Parameter] public int MinSearchCharacters { get; set; } = 2;
        [Parameter] public int DebounceMs { get; set; } = 300;
        [Parameter] public int MaxResults { get; set; } = 50;
        [Parameter] public bool AutoFocus { get; set; } = true;
        [Parameter] public bool Disabled { get; set; } = false;
        [Parameter] public bool AllowClear { get; set; } = true;

        #endregion

        #region Parameters - Two-Way Binding

        /// <summary>
        /// Selected business party. Simple pattern like BusinessParty autocomplete.
        /// </summary>
        [Parameter] public BusinessPartyDto? SelectedBusinessParty { get; set; }
        [Parameter] public EventCallback<BusinessPartyDto?> SelectedBusinessPartyChanged { get; set; }

        #endregion

        #region Parameters - Behavior Modes

        /// <summary>
        /// Defines how editing should be handled
        /// </summary>
        [Parameter] public EntityEditMode EditMode { get; set; } = EntityEditMode.None;

        /// <summary>
        /// Defines how creation should be handled
        /// </summary>
        [Parameter] public EntityCreateMode CreateMode { get; set; } = EntityCreateMode.None;

        /// <summary>
        /// Defines what information to display
        /// </summary>
        [Parameter] public EntityDisplayMode DisplayMode { get; set; } = EntityDisplayMode.All;

        /// <summary>
        /// Preferred business party type for quick create
        /// </summary>
        [Parameter] public BusinessPartyType? PreferredCreateType { get; set; }

        #endregion

        #region Parameters - Events

        [Parameter] public EventCallback<BusinessPartyDto> OnEdit { get; set; }
        [Parameter] public EventCallback<BusinessPartyGroupDto> OnGroupClick { get; set; }
        [Parameter] public EventCallback<BusinessPartyDto> OnBusinessPartyCreated { get; set; }
        [Parameter] public EventCallback<BusinessPartyDto> OnBusinessPartyUpdated { get; set; }

        #endregion

        #region Private Fields

        private MudAutocomplete<BusinessPartyDto>? _autocomplete;
        private string _searchText = string.Empty;

        // Styling constants for group badges
        private const string InlineBadgeOpacity = "15"; // ~8% opacity for background
        private const string InlineBadgeHeight = "18px";
        private const string InlineBadgeFontSize = "0.7rem";
        private const string InlineBadgePadding = "0 6px";

        private const string CardBadgeBackgroundOpacity = "20"; // ~12% opacity for background
        private const string CardBadgeBorderOpacity = "40"; // ~25% opacity for border

        #endregion

        #region Lifecycle Methods

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && AutoFocus && _autocomplete != null && SelectedBusinessParty == null)
            {
                await Task.Delay(100); // Small delay to ensure rendering is complete
                await _autocomplete.FocusAsync();
            }
        }

        #endregion

        #region Search Methods

        /// <summary>
        /// Search business parties by name, tax code, or VAT number.
        /// IMPORTANT: Uses the EXACT same pattern as SearchProductsAsync in UnifiedProductScanner.
        /// Simple, clean, NO StateHasChanged during search.
        /// </summary>
        private async Task<IEnumerable<BusinessPartyDto>> SearchBusinessPartiesAsync(
            string searchTerm,
            CancellationToken cancellationToken)
        {
            // Early return for empty/short search terms
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < MinSearchCharacters)
                return Array.Empty<BusinessPartyDto>();

            try
            {
                var result = await BusinessPartyService.SearchBusinessPartiesAsync(
                    searchTerm,
                    FilterByType,
                    MaxResults);

                if (result == null)
                {
                    Logger.LogWarning("Business party search returned null for term: {SearchTerm}", searchTerm);
                    return Array.Empty<BusinessPartyDto>();
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error searching business parties");
                return Array.Empty<BusinessPartyDto>();
            }
        }

        /// <summary>
        /// Called when a business party is selected from the autocomplete dropdown.
        /// Pattern: Same as OnProductSelectionChangedAsync in UnifiedProductScanner.
        /// </summary>
        private async Task OnBusinessPartySelectionChangedAsync(BusinessPartyDto? businessParty)
        {
            Logger.LogInformation("OnBusinessPartySelectionChangedAsync called. BusinessParty: {Id} - {Name}",
                businessParty?.Id, businessParty?.Name ?? "NULL");

            // Update local property
            SelectedBusinessParty = businessParty;

            // Notify parent component
            if (SelectedBusinessPartyChanged.HasDelegate)
            {
                await SelectedBusinessPartyChanged.InvokeAsync(businessParty);
            }

            Logger.LogInformation("Business party selection propagated to parent. SelectedBusinessParty: {Id}",
                SelectedBusinessParty?.Id);
        }

        #endregion

        #region Action Methods

        /// <summary>
        /// Handle edit button click
        /// </summary>
        private async Task HandleEditClick()
        {
            if (SelectedBusinessParty == null) return;

            if (OnEdit.HasDelegate)
            {
                await OnEdit.InvokeAsync(SelectedBusinessParty);
            }
        }

        /// <summary>
        /// Clear selection and refocus autocomplete
        /// </summary>
        private async Task ClearSelection()
        {
            SelectedBusinessParty = null;
            await SelectedBusinessPartyChanged.InvokeAsync(null);
            _searchText = string.Empty;

            if (_autocomplete != null)
            {
                await _autocomplete.FocusAsync();
            }
        }

        /// <summary>
        /// Handle group chip click (if enabled)
        /// </summary>
        private async Task HandleGroupClick(BusinessPartyGroupDto group)
        {
            if (GroupClickable && OnGroupClick.HasDelegate)
            {
                await OnGroupClick.InvokeAsync(group);
            }
        }

        #endregion

        #region Helper Methods - Styling

        /// <summary>
        /// Get avatar color based on business party type
        /// </summary>
        private Color GetAvatarColor(BusinessPartyDto bp)
        {
            return bp.PartyType switch
            {
                BusinessPartyType.Cliente => Color.Primary,
                BusinessPartyType.Customer => Color.Primary,
                BusinessPartyType.Supplier => Color.Success,
                BusinessPartyType.Both => Color.Secondary,
                _ => Color.Default
            };
        }

        /// <summary>
        /// Get icon based on business party type
        /// </summary>
        private string GetIcon(BusinessPartyDto bp)
        {
            return bp.PartyType switch
            {
                BusinessPartyType.Cliente => Icons.Material.Outlined.Person,
                BusinessPartyType.Customer => Icons.Material.Outlined.Person,
                BusinessPartyType.Supplier => Icons.Material.Outlined.LocalShipping,
                BusinessPartyType.Both => Icons.Material.Outlined.Handshake,
                _ => Icons.Material.Outlined.Business
            };
        }

        /// <summary>
        /// Get business party type label
        /// </summary>
        private string GetBusinessPartyTypeLabel(BusinessPartyDto bp)
        {
            return bp.PartyType switch
            {
                BusinessPartyType.Cliente => "Cliente",
                BusinessPartyType.Customer => "Cliente",
                BusinessPartyType.Supplier => "Fornitore",
                BusinessPartyType.Both => "Cliente/Fornitore",
                _ => "N/D"
            };
        }

        /// <summary>
        /// Get location tooltip text
        /// </summary>
        private string GetLocationTooltip(BusinessPartyDto bp)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(bp.City))
                parts.Add(bp.City);

            if (!string.IsNullOrEmpty(bp.Province))
                parts.Add(bp.Province);

            if (!string.IsNullOrEmpty(bp.Country))
                parts.Add(bp.Country);

            return parts.Any() ? string.Join(", ", parts) : "Località non disponibile";
        }

        /// <summary>
        /// Get inline style for group badges in autocomplete (compact)
        /// </summary>
        private string GetGroupInlineStyle(BusinessPartyGroupDto group)
        {
            if (string.IsNullOrEmpty(group.ColorHex))
                return $"font-size: {InlineBadgeFontSize}; height: {InlineBadgeHeight};";

            return $"background-color: {group.ColorHex}{InlineBadgeOpacity}; color: {group.ColorHex}; font-size: {InlineBadgeFontSize}; height: {InlineBadgeHeight}; padding: {InlineBadgePadding};";
        }

        /// <summary>
        /// Get chip style for group badges in card detail
        /// </summary>
        private string GetGroupChipStyle(BusinessPartyGroupDto group)
        {
            if (string.IsNullOrEmpty(group.ColorHex))
                return string.Empty;

            var bgColor = $"{group.ColorHex}{CardBadgeBackgroundOpacity}";
            return $"background-color: {bgColor}; color: {group.ColorHex}; border: 1px solid {group.ColorHex}{CardBadgeBorderOpacity};";
        }

        /// <summary>
        /// Get tooltip text for group badge
        /// </summary>
        private string GetGroupTooltip(BusinessPartyGroupDto group)
        {
            var tooltip = group.Name;

            if (!string.IsNullOrEmpty(group.Description))
            {
                tooltip += $" - {group.Description}";
            }

            // Add validity info if available
            if (group.ValidFrom.HasValue || group.ValidTo.HasValue)
            {
                tooltip += " | Validità: ";

                if (group.ValidFrom.HasValue)
                {
                    tooltip += $"dal {group.ValidFrom.Value:dd/MM/yyyy}";
                }

                if (group.ValidTo.HasValue)
                {
                    tooltip += $" al {group.ValidTo.Value:dd/MM/yyyy}";
                }
            }

            return tooltip;
        }

        /// <summary>
        /// Get sorted groups by priority (descending)
        /// </summary>
        private IEnumerable<BusinessPartyGroupDto> GetSortedGroups(BusinessPartyDto bp)
        {
            if (bp.Groups == null || !bp.Groups.Any())
                return Enumerable.Empty<BusinessPartyGroupDto>();

            return bp.Groups.OrderByDescending(g => g.Priority);
        }

        #endregion

        #region Dialog Methods

        /// <summary>
        /// Open quick create dialog
        /// </summary>
        private async Task OpenQuickCreateDialog()
        {
            var parameters = new DialogParameters
            {
                ["PreferredPartyType"] = PreferredCreateType ?? FilterByType
            };

            var dialog = await DialogService.ShowAsync<QuickCreateBusinessPartyDialog>(
                "Creazione Rapida Partner",
                parameters,
                new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is BusinessPartyDto createdParty)
            {
                SelectedBusinessParty = createdParty;
                await SelectedBusinessPartyChanged.InvokeAsync(createdParty);
                await OnBusinessPartyCreated.InvokeAsync(createdParty);
                
                Snackbar.Add(
                    TranslationService.GetTranslation("business.partnerCreatedAndSelected", "Partner '{0}' creato e selezionato", createdParty.Name),
                    Severity.Success
                );
            }
        }

        /// <summary>
        /// Open quick edit dialog
        /// </summary>
        private async Task OpenQuickEditDialog()
        {
            if (SelectedBusinessParty == null) return;

            var parameters = new DialogParameters
            {
                ["BusinessPartyId"] = SelectedBusinessParty.Id,
                ["ExistingBusinessParty"] = SelectedBusinessParty
            };

            var dialog = await DialogService.ShowAsync<QuickCreateBusinessPartyDialog>(
                "Modifica Rapida Partner",
                parameters,
                new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is BusinessPartyDto updatedParty)
            {
                SelectedBusinessParty = updatedParty;
                await SelectedBusinessPartyChanged.InvokeAsync(updatedParty);
                await OnBusinessPartyUpdated.InvokeAsync(updatedParty);
                
                Snackbar.Add(
                    TranslationService.GetTranslation("business.partnerUpdated", "Partner aggiornato con successo"),
                    Severity.Success
                );
            }
        }

        #endregion

        #region Helper Methods - Additional Display

        /// <summary>
        /// Get full address from business party
        /// </summary>
        private string? GetFullAddress(BusinessPartyDto bp)
        {
            // Since BusinessPartyDto doesn't have Addresses list, we'll construct from available fields
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(bp.City))
                parts.Add(bp.City);
                
            if (!string.IsNullOrEmpty(bp.Province))
                parts.Add($"({bp.Province})");
                
            if (!string.IsNullOrEmpty(bp.Country))
                parts.Add(bp.Country);
            
            return parts.Any() ? string.Join(", ", parts) : null;
        }

        /// <summary>
        /// Get preferred contact from business party
        /// </summary>
        private ContactDto? GetPreferredContact(BusinessPartyDto bp)
        {
            return bp.Contacts?.FirstOrDefault(c => c.IsPrimary)
                   ?? bp.Contacts?.FirstOrDefault();
        }

        /// <summary>
        /// Get icon for contact type
        /// </summary>
        private string GetContactIcon(ContactType type)
        {
            return type switch
            {
                ContactType.Email => Icons.Material.Outlined.Email,
                ContactType.Phone => Icons.Material.Outlined.Phone,
                ContactType.PEC => Icons.Material.Outlined.MarkEmailRead,
                _ => Icons.Material.Outlined.ContactMail
            };
        }

        /// <summary>
        /// Get label for contact type
        /// </summary>
        private string GetContactTypeLabel(ContactType type)
        {
            return type switch
            {
                ContactType.Email => "Email",
                ContactType.Phone => "Telefono",
                ContactType.PEC => "PEC",
                ContactType.Fax => "Fax",
                _ => "Contatto"
            };
        }

        #endregion
    }
}
