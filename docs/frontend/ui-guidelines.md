# EventForge UI/UX Layout Guidelines

## Overview
This document outlines the standardized UI/UX layout guidelines for EventForge, established as part of issue #164 to optimize container sizes, component dimensions, and spacing for improved user experience.

## Container Width Standards

### Maximum Container Widths
- **Main Application Pages**: `MaxWidth.Medium` (e.g., Home, Activity Feed, Notification Center)
- **Administrative Pages**: `MaxWidth.Large` (e.g., Admin Dashboard, SuperAdmin pages)
- **Error/Auth Pages**: `MaxWidth.Medium` (e.g., 404, Authentication Required)
- **Chat Interface**: `MaxWidth.False` for full-width chat experience
- **Modal Dialogs**: `MaxWidth.Medium` for consistency

### Container Width Migration
- **From**: `MaxWidth.ExtraLarge` → **To**: `MaxWidth.Large`
- **From**: `MaxWidth.Large` → **To**: `MaxWidth.Medium` (where appropriate)

## Typography Standards

### Heading Hierarchy
- **Page Titles**: `Typo.h4` (reduced from `Typo.h3` for better proportion)
- **Section Headers**: `Typo.h5` (reduced from `Typo.h4`)
- **Subsection Headers**: `Typo.h6`
- **Body Text**: `Typo.body1` and `Typo.body2`
- **Captions**: `Typo.caption`

### Typography Migration
- **From**: `Typo.h3` → **To**: `Typo.h4` (main page titles)
- **From**: `Typo.h4` → **To**: `Typo.h5` (error pages, smaller sections)

## Component Size Standards

### Icon Sizes
- **Primary Actions**: `Size.Medium` (24px - reduced from `Size.Large`)
- **Secondary Actions**: `Size.Small` (20px)
- **Decorative Icons**: `Size.Small` (20px)
- **Error/Status Icons**: `Size.Medium` with reduced font-size (48px instead of 72px)

### Button Sizes
- **Primary Buttons**: `Size.Medium` (48px height - reduced from `Size.Large`)
- **Secondary Buttons**: `Size.Medium` (48px height for consistency)
- **Icon Buttons in Rows**: `Size.Small` (36px - for ActionButtonGroup)
- **Icon Buttons in Toolbars**: `Size.Medium` (48px)

### Form Field Heights (UX Best Practice)
- **All Input Fields**: 48px minimum height (MudBlazor default without Dense)
- **MudTextField**: Use `Variant.Outlined` with default height (48px)
- **MudSelect**: Use `Variant.Outlined` with default height (48px)
- **MudAutocomplete**: Use `Variant.Outlined` with default height (48px)
- **MudDatePicker**: Use `Variant.Outlined` with default height (48px)
- **Consistency Rule**: All form fields in the same row should have the same height

### Avatar Sizes
- **User Profiles**: `Size.Medium` (reduced from `Size.Large`)
- **Chat Messages**: `Size.Small`
- **Card Headers**: `Size.Medium`
- **Table Row Avatars**: `Size.Small`

### Progress Indicators
- **Loading States**: `Size.Medium` or `Size.Small` (reduced from `Size.Large`)
- **Drawer Loading**: `Size.Small`

### Chips and Badges
- **Tags**: `Size.Small`
- **Status Indicators**: `Size.Small`

## Spacing Standards

### Padding (pa-X)
- **Main Content Cards**: `pa-2 pa-sm-3 pa-md-4` (responsive, reduced from `pa-4 pa-sm-6 pa-md-8`)
- **Drawer Components**: `pa-2` (reduced from `pa-4`)
- **Button Padding**: `pa-2` (reduced from `pa-4`)
- **Secondary Content**: `pa-2` to `pa-3`
- **Filter Sections**: `pa-3` for comfortable spacing

### Margin Bottom (mb-X)
- **Section Spacing**: `mb-3` to `mb-4` (reduced from `mb-6`)
- **Element Spacing**: `mb-2` to `mb-3`
- **Tight Spacing**: `mb-1` to `mb-2`

### Gap Spacing (gap-X) - Material Design 8px Baseline
- **Form Fields in Row**: `gap-2` or `gap-3` (16px or 24px between fields)
- **Action Buttons**: `gap-1` (8px between icon buttons)
- **Card Sections**: `gap-4` (32px between major sections)
- **Inline Elements**: `gap-2` (16px for chips, badges)

### Padding Migration
- **From**: `pa-4 pa-sm-6 pa-md-8` → **To**: `pa-2 pa-sm-3 pa-md-4`
- **From**: `pa-4` → **To**: `pa-2`
- **From**: `pa-8` → **To**: `pa-4`

## Responsive Design Principles

### Breakpoint Behavior
- **Mobile (xs)**: Minimal padding, single column layouts
- **Tablet (sm/md)**: Moderate padding, flexible grid layouts  
- **Desktop (lg/xl)**: Standard padding, multi-column layouts

### Mobile-First Approach
- Start with mobile constraints and progressively enhance
- Use responsive padding: `pa-2 pa-sm-3 pa-md-4`
- Ensure touch targets remain accessible (minimum 44px)

## Implementation Examples

### Standard Page Header
```razor
<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudPaper Elevation="2" Class="pa-2 pa-sm-3 pa-md-4 mb-4">
        <div class="d-flex justify-space-between align-center mb-4">
            <div>
                <MudText Typo="Typo.h4">
                    <MudIcon Icon="@PageIcon" Class="mr-2" Size="Size.Medium" />
                    @PageTitle
                </MudText>
                <MudText Typo="Typo.body2" Class="mud-text-secondary mt-2">
                    @PageDescription
                </MudText>
            </div>
        </div>
    </MudPaper>
</MudContainer>
```

### Standard Form Fields Pattern (Consistent Heights)
```razor
<!-- ✅ CORRECT: All fields have same height (48px default) -->
<div class="d-flex gap-3 align-center flex-wrap">
    <MudTextField @bind-Value="_searchTerm"
                  Label="@TranslationService.GetTranslation("search", "Cerca")"
                  Variant="Variant.Outlined"
                  Adornment="Adornment.End"
                  AdornmentIcon="@Icons.Material.Outlined.Search"
                  Clearable="true"
                  Style="flex: 2;" />
    <MudSelect T="string" @bind-Value="_filter" 
               Label="@TranslationService.GetTranslation("filter", "Filtra")"
               Variant="Variant.Outlined"
               Clearable="true"
               Style="flex: 1;">
        <MudSelectItem Value="@("all")">Tutti</MudSelectItem>
        <MudSelectItem Value="@("active")">Attivi</MudSelectItem>
    </MudSelect>
    <MudButton Variant="Variant.Filled" 
               Color="Color.Primary" 
               Size="Size.Medium"
               StartIcon="@Icons.Material.Outlined.Search">
        Cerca
    </MudButton>
</div>

<!-- ❌ WRONG: Mixed heights create visual imbalance -->
<div class="d-flex gap-2">
    <MudTextField Dense="true" ... />  <!-- 40px height -->
    <MudSelect Dense="false" ... />     <!-- 48px height -->
    <MudButton Size="Size.Large" ... /> <!-- 56px height -->
</div>
```

### Standard Button Pattern
```razor
<!-- Primary action button (48px height) -->
<MudButton Variant="Variant.Filled" 
           Color="Color.Primary" 
           Size="Size.Medium"
           StartIcon="@Icons.Material.Filled.Add">
    Action Text
</MudButton>

<!-- Icon button in toolbar (48px) -->
<MudIconButton Icon="@Icons.Material.Outlined.Refresh"
               Color="Color.Primary"
               Size="Size.Medium" />

<!-- Icon button in table row (36px) -->
<MudIconButton Icon="@Icons.Material.Outlined.Edit"
               Color="Color.Warning"
               Size="Size.Small" />
```

### Standard Drawer Padding
```razor
<div class="entity-drawer-header d-flex justify-space-between align-center pa-2 border-b">
    <!-- Header content -->
</div>
<div class="entity-drawer-content flex-grow-1 pa-2">
    <!-- Main content with consistent field heights -->
    <MudGrid>
        <MudItem xs="12" md="6">
            <MudTextField @bind-Value="_model.Name"
                          Label="Name"
                          Variant="Variant.Outlined"
                          Required="true" />
        </MudItem>
        <MudItem xs="12" md="6">
            <MudSelect @bind-Value="_model.Type"
                       Label="Type"
                       Variant="Variant.Outlined">
                <!-- Options -->
            </MudSelect>
        </MudItem>
    </MudGrid>
</div>
<div class="entity-drawer-actions d-flex justify-end gap-2 pa-2 border-t">
    <!-- Action buttons (all Size.Medium for 48px height) -->
    <MudButton Variant="Variant.Text" 
               Color="Color.Default" 
               Size="Size.Medium">
        Cancel
    </MudButton>
    <MudButton Variant="Variant.Filled" 
               Color="Color.Primary" 
               Size="Size.Medium">
        Save
    </MudButton>
</div>
```

## Best Practices

### Do's ✅
- Use consistent container widths across similar page types
- Apply responsive padding patterns (`pa-2 pa-sm-3 pa-md-4`)
- Prefer `Size.Medium` over `Size.Large` for most components
- Use `Typo.h4` for main page titles instead of `Typo.h3`
- Maintain adequate spacing for accessibility (minimum touch targets)
- Test layouts on mobile, tablet, and desktop viewports
- **Ensure all form fields have consistent 48px height (no Dense on main forms)**
- **Use gap-2 or gap-3 for consistent spacing between form fields**
- **Align buttons with form field heights (48px) for visual balance**
- **Apply Variant.Outlined consistently for all form inputs**
- **Use d-flex with gap classes instead of manual margins**

### Don'ts ❌
- Don't use `MaxWidth.ExtraLarge` for standard content pages
- Don't use `pa-8` or excessive padding on main content areas
- Don't use `Size.Large` icons unless specifically needed for emphasis
- Don't use `Typo.h3` for standard page titles
- Don't ignore responsive design - always test on multiple screen sizes
- Don't remove accessibility features in favor of tighter layouts
- **Don't mix Dense and non-Dense fields in the same form**
- **Don't use different heights for buttons and inputs in the same row**
- **Don't use Size.Large for buttons in standard forms (breaks visual balance)**
- **Don't use inline styles for spacing when gap classes are available**

## Migration Checklist

When updating existing components to follow these guidelines:

- [ ] Update container `MaxWidth` to appropriate size (False → Large/Medium)
- [ ] Reduce padding from high values (`pa-6`, `pa-8`) to standard values (`pa-2`, `pa-3`, `pa-4`)
- [ ] Change `Typo.h3` to `Typo.h4` for main titles
- [ ] Update icon `Size.Large` to `Size.Medium` where appropriate
- [ ] Update button sizes from `Size.Large` to `Size.Medium` (except table row actions use Size.Small)
- [ ] **Remove `Dense="true"` from form fields to ensure consistent 48px height**
- [ ] **Ensure all fields in same row have `Variant.Outlined` consistently**
- [ ] **Replace manual margins with gap classes (gap-2, gap-3) for spacing**
- [ ] **Align button heights with input field heights (both 48px with Size.Medium)**
- [ ] **Use d-flex with gap instead of individual margin classes**
- [ ] Test responsive behavior on mobile and tablet
- [ ] Verify accessibility is maintained (48px minimum touch targets)
- [ ] Ensure visual hierarchy remains clear

## Version History

- **v1.1** (Issue #Current): UI/UX Consistency Enhancement
  - Added form field height standards (48px touch-friendly default)
  - Added gap spacing guidelines (Material Design 8px baseline)
  - Enhanced button size standards with context-specific sizing
  - Added visual consistency rules for mixed form elements
  - Expanded implementation examples with do's and don'ts
  - Added form field alignment best practices
  
- **v1.0** (Issue #164): Initial layout optimization guidelines established
  - Container width standardization
  - Typography hierarchy optimization  
  - Component size reduction
  - Spacing standardization
  - Responsive design improvements

---

*Last updated: Current Issue - UI/UX Consistency and Form Field Alignment*