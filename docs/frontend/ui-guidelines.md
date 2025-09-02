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
- **Primary Actions**: `Size.Medium` (reduced from `Size.Large`)
- **Secondary Actions**: `Size.Small`
- **Decorative Icons**: `Size.Small`
- **Error/Status Icons**: `Size.Medium` with reduced font-size (48px instead of 72px)

### Button Sizes
- **Primary Buttons**: `Size.Medium` (reduced from `Size.Large`)
- **Secondary Buttons**: `Size.Small`
- **Icon Buttons**: `Size.Small`

### Avatar Sizes
- **User Profiles**: `Size.Medium` (reduced from `Size.Large`)
- **Chat Messages**: `Size.Small`
- **Card Headers**: `Size.Medium`

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

### Margin Bottom (mb-X)
- **Section Spacing**: `mb-3` to `mb-4` (reduced from `mb-6`)
- **Element Spacing**: `mb-2` to `mb-3`
- **Tight Spacing**: `mb-1` to `mb-2`

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
<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    <MudCard Class="pa-2 pa-sm-3 pa-md-4" Elevation="4">
        <MudCardContent>
            <header class="text-center mb-4">
                <MudText Typo="Typo.h4" Component="h1" Align="Align.Center" Class="mb-4">
                    <MudIcon Icon="@PageIcon" Class="mr-2" Size="Size.Medium" />
                    @PageTitle
                </MudText>
            </header>
        </MudCardContent>
    </MudCard>
</MudContainer>
```

### Standard Button Pattern
```razor
<MudButton Variant="Variant.Filled" 
           Color="Color.Primary" 
           Size="Size.Medium"
           StartIcon="@Icons.Material.Filled.Action">
    Action Text
</MudButton>
```

### Standard Drawer Padding
```razor
<div class="entity-drawer-header d-flex justify-space-between align-center pa-2 border-b">
    <!-- Header content -->
</div>
<div class="entity-drawer-content flex-grow-1 pa-2">
    <!-- Main content -->
</div>
<div class="entity-drawer-actions d-flex justify-end gap-2 pa-2 border-t">
    <!-- Action buttons -->
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

### Don'ts ❌
- Don't use `MaxWidth.ExtraLarge` for standard content pages
- Don't use `pa-8` or excessive padding on main content areas
- Don't use `Size.Large` icons unless specifically needed for emphasis
- Don't use `Typo.h3` for standard page titles
- Don't ignore responsive design - always test on multiple screen sizes
- Don't remove accessibility features in favor of tighter layouts

## Migration Checklist

When updating existing components to follow these guidelines:

- [ ] Update container `MaxWidth` to appropriate size
- [ ] Reduce padding from high values (`pa-6`, `pa-8`) to standard values (`pa-2`, `pa-3`, `pa-4`)
- [ ] Change `Typo.h3` to `Typo.h4` for main titles
- [ ] Update icon `Size.Large` to `Size.Medium` where appropriate
- [ ] Update button sizes from `Size.Large` to `Size.Medium`
- [ ] Test responsive behavior on mobile and tablet
- [ ] Verify accessibility is maintained
- [ ] Ensure visual hierarchy remains clear

## Version History

- **v1.0** (Issue #164): Initial layout optimization guidelines established
  - Container width standardization
  - Typography hierarchy optimization  
  - Component size reduction
  - Spacing standardization
  - Responsive design improvements

---

*Last updated: Issue #164 implementation - UI Layout and Component Optimization*