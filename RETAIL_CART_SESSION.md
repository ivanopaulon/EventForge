# EventForge Retail Cart Session

## Overview

The EventForge Retail Cart Session service provides a comprehensive solution for managing shopping cart sessions with automatic promotion application, tenant isolation, and real-time total calculations.

## Features

### Core Functionality

- **Session Management**: Create, retrieve, and manage cart sessions with unique identifiers
- **Item Management**: Add, update, remove items with automatic quantity consolidation
- **Promotion Integration**: Automatic application of promotions when cart contents change
- **Coupon Support**: Apply and manage coupon codes for promotion activation
- **Real-time Totals**: Automatic recalculation of totals including discounts

### Multi-tenancy and Security

- **Tenant Isolation**: Cart sessions are isolated per tenant with secure access control
- **User Scoping**: Sessions are scoped to authenticated users (future enhancement)
- **In-memory Storage**: Current implementation uses in-memory storage for fast access

### Performance Features

- **Incremental Updates**: Promotions are recalculated only when cart contents change
- **Caching Integration**: Leverages promotion engine caching for optimal performance
- **Graceful Degradation**: Falls back to basic totals if promotion service is unavailable

## API Endpoints

### POST /api/v1/RetailCartSessions

Creates a new cart session.

#### Request Body

```json
{
  "customerId": "uuid-string",
  "salesChannel": "online",
  "currency": "EUR"
}
```

#### Response

```json
{
  "id": "session-uuid",
  "customerId": "customer-uuid",
  "salesChannel": "online",
  "currency": "EUR",
  "items": [],
  "couponCodes": [],
  "originalTotal": 0.00,
  "finalTotal": 0.00,
  "totalDiscountAmount": 0.00,
  "appliedPromotions": [],
  "createdAt": "2023-12-01T10:00:00Z",
  "updatedAt": "2023-12-01T10:00:00Z"
}
```

### GET /api/v1/RetailCartSessions/{id}

Retrieves a cart session by ID with current totals.

#### Response

Returns the cart session with recalculated promotions and totals.

### POST /api/v1/RetailCartSessions/{id}/items

Adds an item to the cart session.

#### Request Body

```json
{
  "productId": "product-uuid",
  "productCode": "SKU123",
  "productName": "Product Name",
  "unitPrice": 25.99,
  "quantity": 2,
  "categoryIds": ["category-uuid"]
}
```

#### Response

Returns the updated cart session with the new item and recalculated totals.

### PATCH /api/v1/RetailCartSessions/{id}/items/{itemId}

Updates the quantity of an item in the cart.

#### Request Body

```json
{
  "quantity": 5
}
```

#### Response

Returns the updated cart session. If quantity is set to 0 or negative, the item is removed.

### DELETE /api/v1/RetailCartSessions/{id}/items/{itemId}

Removes an item from the cart session.

#### Response

Returns the updated cart session without the removed item.

### POST /api/v1/RetailCartSessions/{id}/coupons

Applies coupon codes to the cart session.

#### Request Body

```json
{
  "couponCodes": ["SAVE10", "WELCOME20"]
}
```

#### Response

Returns the updated cart session with applied coupons and recalculated promotions.

### POST /api/v1/RetailCartSessions/{id}/clear

Clears all items and coupons from the cart session.

#### Response

Returns the cleared cart session with zero totals.

## Data Models

### CartSessionDto

```json
{
  "id": "uuid",
  "customerId": "uuid",
  "salesChannel": "string",
  "currency": "string",
  "items": [CartSessionItemDto],
  "couponCodes": ["string"],
  "originalTotal": 0.00,
  "finalTotal": 0.00,
  "totalDiscountAmount": 0.00,
  "appliedPromotions": [AppliedPromotionDto],
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### CartSessionItemDto

```json
{
  "id": "uuid",
  "productId": "uuid",
  "productCode": "string",
  "productName": "string",
  "unitPrice": 0.00,
  "quantity": 0,
  "categoryIds": ["uuid"],
  "originalLineTotal": 0.00,
  "finalLineTotal": 0.00,
  "promotionDiscount": 0.00,
  "effectiveDiscountPercentage": 0.00,
  "appliedPromotions": [AppliedPromotionDto]
}
```

## Usage Examples

### Basic Cart Lifecycle

```javascript
// 1. Create a cart session
const session = await fetch('/api/v1/RetailCartSessions', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    customerId: 'customer-123',
    salesChannel: 'online',
    currency: 'EUR'
  })
});

// 2. Add items to cart
const addItem = await fetch(`/api/v1/RetailCartSessions/${session.id}/items`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    productId: 'product-456',
    productName: 'Laptop',
    unitPrice: 999.99,
    quantity: 1,
    categoryIds: ['electronics']
  })
});

// 3. Apply coupons
const withCoupons = await fetch(`/api/v1/RetailCartSessions/${session.id}/coupons`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    couponCodes: ['SAVE10']
  })
});

// 4. Get final totals
const finalCart = await fetch(`/api/v1/RetailCartSessions/${session.id}`);
```

### Integration with Promotion Engine

The cart session automatically integrates with the promotion engine:

1. **Automatic Recalculation**: Promotions are applied whenever cart contents change
2. **Coupon Support**: Coupons are passed to the promotion engine for validation
3. **Detailed Results**: Returns line-by-line promotion details and totals
4. **Error Handling**: Gracefully handles promotion engine failures

### Example Response with Promotions

```json
{
  "id": "cart-session-uuid",
  "originalTotal": 199.98,
  "finalTotal": 169.98,
  "totalDiscountAmount": 30.00,
  "items": [
    {
      "id": "item-uuid",
      "productName": "Laptop",
      "unitPrice": 999.99,
      "quantity": 2,
      "originalLineTotal": 199.98,
      "finalLineTotal": 169.98,
      "promotionDiscount": 30.00,
      "effectiveDiscountPercentage": 15.0,
      "appliedPromotions": [
        {
          "promotionName": "Electronics 15% Off",
          "discountAmount": 30.00,
          "discountPercentage": 15.0,
          "description": "15% off electronics category"
        }
      ]
    }
  ],
  "appliedPromotions": [
    {
      "promotionName": "Electronics 15% Off",
      "discountAmount": 30.00,
      "ruleType": "CategoryDiscount"
    }
  ],
  "couponCodes": ["ELECTRONICS15"]
}
```

## Implementation Details

### Tenant Isolation

- Cart sessions are stored with tenant-specific keys
- All operations validate tenant access through ITenantContext
- Cross-tenant session access is prevented

### Storage Strategy

- **Current**: In-memory storage using ConcurrentDictionary for thread safety
- **Future**: Pluggable storage interface for database persistence
- **Session Key Format**: `{tenantId}:{sessionId}` for isolation

### Promotion Integration

```csharp
// Automatic promotion application on cart changes
private async Task<CartSessionDto> RecalculateAndMapToDto(CartSession session)
{
    if (session.Items.Any())
    {
        var applyDto = new ApplyPromotionRulesDto
        {
            CartItems = MapSessionItemsToCartItems(session.Items),
            CustomerId = session.CustomerId,
            SalesChannel = session.SalesChannel,
            CouponCodes = session.CouponCodes,
            OrderDateTime = DateTime.UtcNow,
            Currency = session.Currency
        };
        
        var promotionResult = await _promotionService.ApplyPromotionRulesAsync(applyDto);
        return MapSessionWithPromotions(session, promotionResult);
    }
    
    return MapSessionWithoutPromotions(session);
}
```

### Error Handling

- **Promotion Service Failures**: Cart returns with original totals, logs warning
- **Invalid Operations**: Returns appropriate HTTP status codes with ProblemDetails
- **Session Not Found**: Returns 404 with clear error message
- **Tenant Access Violations**: Returns 403 Forbidden

## Performance Considerations

### Optimization Strategies

1. **Lazy Promotion Calculation**: Only calculated when cart data is accessed
2. **Promotion Engine Caching**: Leverages 60-second promotion cache
3. **Minimal Data Transfer**: Only essential data in API responses
4. **Graceful Degradation**: Continues operation even if promotions fail

### Scalability Notes

- **In-Memory Limitations**: Current implementation doesn't scale across instances
- **Future Enhancements**: Redis or database storage for production scalability
- **Session Cleanup**: Implement TTL-based session expiration

## Future Enhancements

### Planned Features

1. **Persistent Storage**: Database-backed session storage
2. **Real-time Updates**: SignalR integration for live cart updates
3. **Advanced Caching**: Redis-based distributed caching
4. **User Association**: Link sessions to authenticated users
5. **Session Sharing**: Support for guest-to-authenticated user session transfer
6. **Audit Trail**: Track cart modifications for analytics

### Integration Points

1. **Order Processing**: Convert cart sessions to orders
2. **Inventory**: Real-time stock validation
3. **Payment**: Secure checkout flow integration
4. **Analytics**: Cart abandonment and conversion tracking