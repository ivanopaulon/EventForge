# EventForge Promotion Engine

## Overview

The EventForge Promotion Engine is a comprehensive solution for applying discounts and promotions to shopping carts with advanced business rules, priority handling, and caching optimization.

## Features

### Supported Rule Types

1. **Discount** - Percentage or fixed amount discounts on products
2. **CategoryDiscount** - Discounts applied to specific product categories  
3. **CartAmountDiscount** - Discounts based on minimum cart total thresholds
4. **BuyXGetY** - Buy X quantity, get Y quantity free or discounted
5. **FixedPrice** - Set fixed price for specific products
6. **Bundle** - Bundle multiple products at a special combined price
7. **CustomerSpecific** - Discounts for specific customers/groups
8. **Coupon** - Promotions requiring coupon codes
9. **TimeLimited** - Promotions active only during specific time periods
10. **Exclusive** - Non-combinable promotions that stop further applications

### Precedence and Stacking Model

- **Priority-based ordering**: Promotions are applied in descending order of Priority, then by Name
- **IsCombinable flag**: Controls whether promotions can be stacked with others
- **Exclusive rules**: When applied, stop all further promotion applications
- **Line locking**: Non-combinable promotions lock affected cart lines from further discounts

### Caching and Performance

- **60-second TTL** cache for active promotions per tenant
- **Automatic cache invalidation** on promotion CRUD operations
- **EF Include optimizations** to prevent N+1 queries
- **Fast path** for carts with no applicable rules

### Currency and Precision

- **Currency-safe calculations** using decimal arithmetic
- **MidpointRounding.AwayFromZero** for consistent rounding behavior
- **2 decimal place precision** for all monetary calculations

## API Endpoint

### POST /api/v1/Promotions/apply

Applies promotion rules to a cart or order.

#### Request Body

```json
{
  "cartItems": [
    {
      "productId": "uuid",
      "productCode": "SKU123",
      "productName": "Product Name",
      "unitPrice": 10.00,
      "quantity": 2,
      "categoryIds": ["uuid1", "uuid2"],
      "existingLineDiscount": 5.0
    }
  ],
  "customerId": "uuid",
  "salesChannel": "online",
  "couponCodes": ["SAVE10", "WELCOME"],
  "orderDateTime": "2023-12-01T10:00:00Z",
  "currency": "EUR"
}
```

#### Response

```json
{
  "originalTotal": 20.00,
  "finalTotal": 16.20,
  "totalDiscountAmount": 3.80,
  "success": true,
  "cartItems": [
    {
      "productId": "uuid",
      "originalLineTotal": 20.00,
      "finalLineTotal": 16.20,
      "promotionDiscount": 1.80,
      "effectiveDiscountPercentage": 19.0,
      "appliedPromotions": [
        {
          "promotionId": "uuid",
          "promotionName": "10% Off All Items",
          "promotionRuleId": "uuid",
          "ruleType": "Percentage",
          "discountAmount": 1.80,
          "discountPercentage": 10.0,
          "description": "10% discount on Product Name",
          "affectedProductIds": ["uuid"]
        }
      ]
    }
  ],
  "appliedPromotions": [...],
  "messages": ["Promotion applied successfully"]
}
```

## Validation Rules

### Input Validation

- **Currency required**: Currency code must be provided
- **Non-empty cart**: At least one cart item required
- **Non-negative prices**: Unit prices must be >= 0
- **Positive quantities**: Quantities must be > 0
- **Valid discount percentages**: Existing discounts between 0-100%
- **Coupon code format**: Max 50 characters when provided

### Business Rules

- **Date-based filtering**: Promotions active within StartDate/EndDate window
- **Tenant isolation**: Only promotions for current tenant are considered
- **Sales channel filtering**: Optional filtering by sales channel
- **Time-based restrictions**: Support for day-of-week and time-of-day rules
- **Minimum order amounts**: Both promotion and rule-level minimums supported

## Example Use Cases

### Basic Percentage Discount

```json
{
  "cartItems": [{"productId": "123", "unitPrice": 100, "quantity": 1}],
  "currency": "EUR"
}
```

Result: 10% discount applied → Final total: 90 EUR

### Buy 2 Get 1 Free

```json
{
  "cartItems": [{"productId": "123", "unitPrice": 50, "quantity": 3}],
  "currency": "EUR"
}
```

Result: 1 item free → Final total: 100 EUR (2 × 50)

### Cart Amount Threshold

```json
{
  "cartItems": [...], // Total: 150 EUR
  "currency": "EUR"
}
```

Result: 5% off orders over 100 EUR → Final total: 142.50 EUR

### Coupon Required

```json
{
  "cartItems": [...],
  "couponCodes": ["SAVE20"],
  "currency": "EUR"
}
```

Result: 20% discount applied with valid coupon

### Bundle Pricing

```json
{
  "cartItems": [
    {"productId": "A", "unitPrice": 30, "quantity": 1},
    {"productId": "B", "unitPrice": 40, "quantity": 1}
  ],
  "currency": "EUR"
}
```

Result: Bundle price 60 EUR instead of 70 EUR → Save 10 EUR

## Error Handling

### Validation Errors (400)

- Missing required fields
- Invalid data types or ranges
- Business rule violations

### Success with Warnings (200)

- Inapplicable promotions (logged as messages)
- Insufficient cart total for thresholds
- Invalid or expired coupon codes
- Non-combinable promotion conflicts

## Logging

- **Debug level**: Cache hits/misses, rule evaluations
- **Information level**: Promotion application results, totals
- **Warning level**: Skipped rules, invalid coupons
- **Error level**: System errors, database issues
- **No PII logging**: Product names and amounts are logged, but no customer data

## Implementation Notes

- **Thread-safe caching** using IMemoryCache
- **Tenant isolation** enforced at all levels  
- **Optimistic promotion application** with rollback capability
- **Extensible rule engine** for adding new promotion types
- **Database-first approach** with Entity Framework optimizations