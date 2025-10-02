# Sales Components - README

## 📍 Overview

This directory contains Blazor components for the EventForge Sales/POS system (Epic #277 Phase 3).

**Location**: `/EventForge.Client/Shared/Components/Sales/`

## 🎯 Components

### 1. CartSummary.razor

**Purpose**: Display and manage shopping cart items with totals calculation.

**Features**:
- Display list of items with product details
- Edit quantity inline (+/- buttons)
- Remove individual items
- Calculate subtotal, discount, VAT, and grand total
- Clear entire cart
- Responsive design for tablet/POS

**Usage**:
```razor
<CartSummary Items="_cartItems"
             AllowEdit="true"
             OnItemQuantityChanged="HandleQuantityChange"
             OnItemRemoved="HandleItemRemove"
             OnCartCleared="HandleCartClear" />
```

**Parameters**:
- `Items` (List<SaleItemDto>?) - List of cart items
- `AllowEdit` (bool) - Enable/disable editing (default: true)
- `OnItemQuantityChanged` (EventCallback<SaleItemDto>) - Fired when quantity changes
- `OnItemRemoved` (EventCallback<SaleItemDto>) - Fired when item is removed
- `OnCartCleared` (EventCallback) - Fired when cart is cleared

---

### 2. ProductSearch.razor

**Purpose**: Search products by name, code, or barcode with debounced input.

**Features**:
- Debounced search input (300ms)
- Display search results with product images
- Show price, stock, and category info
- Quick actions (scan barcode, clear)
- Loading state during search
- Responsive result cards

**Usage**:
```razor
<ProductSearch ShowQuickActions="true"
               OnProductSelected="HandleProductSelected" />
```

**Parameters**:
- `ShowQuickActions` (bool) - Show/hide quick action buttons (default: true)
- `OnProductSelected` (EventCallback<ProductDto>) - Fired when product is selected

**Note**: Currently uses placeholder ProductDto. Replace with actual API integration:
```csharp
// TODO: Replace mock with real API call
_searchResults = await ProductService.SearchAsync(_searchText);
```

---

### 3. PaymentPanel.razor

**Purpose**: Manage multi-payment transactions with various payment methods.

**Features**:
- Display total, paid amount, and remaining
- Grid of payment methods (touch-friendly)
- Input payment amount with quick amounts
- List of added payments with timestamps
- Remove individual payments
- Calculate change to give/receive
- Visual feedback for payment status

**Usage**:
```razor
<PaymentPanel TotalAmount="_totalAmount"
              PaymentMethods="_paymentMethods"
              Payments="_payments"
              AllowEdit="true"
              OnPaymentAdded="HandlePaymentAdd"
              OnPaymentRemoved="HandlePaymentRemove" />
```

**Parameters**:
- `TotalAmount` (decimal) - Total amount to pay
- `PaymentMethods` (List<PaymentMethodDto>?) - Available payment methods
- `Payments` (List<SalePaymentDto>?) - List of added payments
- `AllowEdit` (bool) - Enable/disable payment removal (default: true)
- `OnPaymentAdded` (EventCallback<(PaymentMethodDto, decimal)>) - Fired when payment is added
- `OnPaymentRemoved` (EventCallback<SalePaymentDto>) - Fired when payment is removed

---

## 🎨 Styling

All components use styles from `/EventForge.Client/wwwroot/css/sales.css`

**Key CSS Classes**:
- `.cart-summary` - CartSummary container
- `.product-search` - ProductSearch container
- `.payment-panel` - PaymentPanel container
- `.search-result-card` - Product result cards
- `.payment-method-button` - Payment method buttons

**Touch-Friendly Design**:
- Buttons: min 48px height (56px on tablets)
- Payment buttons: min 64px height (80px on tablets)
- Large touch targets for all interactive elements
- Responsive breakpoints for tablet/POS/mobile

---

## 📱 Responsive Breakpoints

| Device | Resolution | Button Size | Notes |
|--------|-----------|-------------|-------|
| Desktop POS | 1920x1080 | 48px | Standard |
| Tablet Landscape | 1024x768 | 56px | **Primary target** |
| Tablet Portrait | 768x1024 | 56px | Reduced padding |
| Mobile | 375x667 | 48px | Fallback |

---

## 🔗 Dependencies

**Required Namespaces**:
```csharp
@using EventForge.DTOs.Sales
@inject ISnackbar Snackbar
```

**MudBlazor Components Used**:
- MudPaper, MudCard, MudCardContent
- MudText, MudIcon, MudAvatar
- MudButton, MudIconButton
- MudTextField, MudRadioGroup
- MudSimpleTable, MudList, MudListItem
- MudAlert, MudDivider, MudChip
- MudGrid, MudItem
- MudProgressCircular

---

## 🧪 Testing

### Manual Testing Checklist

**CartSummary**:
- [ ] Items display correctly with all details
- [ ] Increase/decrease quantity works
- [ ] Remove item works
- [ ] Totals calculate correctly
- [ ] Clear cart works
- [ ] Responsive on different screen sizes

**ProductSearch**:
- [ ] Search input with debounce works
- [ ] Results display correctly
- [ ] Product selection triggers callback
- [ ] Loading state shows during search
- [ ] Clear search works
- [ ] Responsive cards on different screens

**PaymentPanel**:
- [ ] Payment methods display in grid
- [ ] Amount input accepts valid values
- [ ] Quick amount buttons work
- [ ] Add payment works
- [ ] Remove payment works
- [ ] Totals calculate correctly (remaining/change)
- [ ] Responsive on different screen sizes

---

## 🚀 Integration Example

### SalesWizard.razor Integration

```razor
@page "/sales/wizard"
@using EventForge.Client.Shared.Components.Sales

<MudContainer MaxWidth="MaxWidth.Large">
    <MudStepper>
        <!-- Step: Products -->
        <MudStep Title="Prodotti">
            <MudGrid>
                <MudItem xs="12" md="6">
                    <ProductSearch OnProductSelected="AddProductToCart" />
                </MudItem>
                <MudItem xs="12" md="6">
                    <CartSummary Items="_cartItems"
                                 OnItemQuantityChanged="UpdateCartItem"
                                 OnItemRemoved="RemoveCartItem" />
                </MudItem>
            </MudGrid>
        </MudStep>

        <!-- Step: Payment -->
        <MudStep Title="Pagamento">
            <PaymentPanel TotalAmount="_totalAmount"
                          PaymentMethods="_paymentMethods"
                          Payments="_payments"
                          OnPaymentAdded="AddPayment" />
        </MudStep>
    </MudStepper>
</MudContainer>

@code {
    private List<SaleItemDto> _cartItems = new();
    private List<SalePaymentDto> _payments = new();
    private List<PaymentMethodDto> _paymentMethods = new();
    private decimal _totalAmount => _cartItems.Sum(i => i.TotalAmount);

    private async Task AddProductToCart(ProductSearch.ProductDto product)
    {
        // TODO: Convert ProductDto to SaleItemDto
        // TODO: Add to cart
    }

    private async Task UpdateCartItem(SaleItemDto item)
    {
        // TODO: Recalculate totals
        // TODO: Update backend session
    }

    private async Task AddPayment((PaymentMethodDto method, decimal amount) payment)
    {
        // TODO: Add payment to session
        // TODO: Update payments list
    }
}
```

---

## 📋 TODO

**Short Term**:
- [ ] Integrate ProductService API for real product search
- [ ] Add barcode scanner integration
- [ ] Add validation for payment amounts
- [ ] Add keyboard shortcuts for power users
- [ ] Add unit tests with bUnit

**Long Term**:
- [ ] Add virtual scrolling for large product lists
- [ ] Add product image caching
- [ ] Add offline support for payment methods
- [ ] Add print receipt preview in PaymentPanel
- [ ] Add accessibility improvements (ARIA labels)

---

## 🛠️ Troubleshooting

**Issue**: Components not compiling  
**Solution**: Ensure `@using EventForge.DTOs.Sales` is in `_Imports.razor`

**Issue**: Styles not applying  
**Solution**: Verify `sales.css` is linked in `index.html`

**Issue**: EventCallbacks not firing  
**Solution**: Check parent component has handlers for all EventCallbacks

**Issue**: ProductSearch not showing results  
**Solution**: Replace placeholder ProductDto with actual API integration

---

## 📚 Resources

- [Epic #277 Master Documentation](../../docs/EPIC_277_MASTER_DOCUMENTATION.md)
- [Phase 3 Progress Report](../../docs/EPIC_277_PHASE3_PROGRESS.md)
- [MudBlazor Documentation](https://mudblazor.com/)
- [Blazor Component Documentation](https://learn.microsoft.com/aspnet/core/blazor/components/)

---

**Last Updated**: Gennaio 2025  
**Maintainer**: EventForge Development Team  
**Version**: 1.0 (Phase 3 Initial Implementation)
