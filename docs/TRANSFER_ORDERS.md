# Transfer Orders - Multi-Warehouse Stock Transfer Management

## Overview

The Transfer Orders feature enables EventForge to manage stock transfers between warehouses with full workflow control, audit trail, and real-time tracking. This enterprise-grade functionality ensures complete visibility and control over inter-warehouse movements.

## Features

### Core Functionality
- **Create Transfer Orders**: Define stock transfers from source to destination warehouses
- **Ship Orders**: Execute shipment, create stock movements OUT, reduce source stock
- **Receive Orders**: Confirm receipt, create stock movements IN, increase destination stock
- **Track Status**: Monitor orders through complete lifecycle
- **Audit Trail**: Complete history of all transfer operations

### Status Workflow

```
Draft → Pending → Shipped → InTransit → Completed
                    ↓
                Cancelled
```

**Status Definitions:**
- **Draft**: Order being created (not yet in system)
- **Pending**: Order confirmed, awaiting shipment
- **Shipped**: Stock deducted from source, in transit
- **InTransit**: Acknowledged as in transit (optional intermediate state)
- **Completed**: Transfer fully processed, stock added at destination
- **Cancelled**: Order cancelled (only before shipping)

## Database Schema

### TransferOrders Table
- **Id**: Unique identifier (GUID)
- **Number**: Transfer order number (auto-generated)
- **Series**: Optional series/prefix for numbering
- **OrderDate**: When order was created
- **SourceWarehouseId**: Source warehouse reference
- **DestinationWarehouseId**: Destination warehouse reference
- **Status**: Current order status (enum)
- **ShipmentDate**: When order was shipped
- **ExpectedArrivalDate**: Expected delivery date
- **ActualArrivalDate**: Actual receipt date
- **Notes**: Additional notes
- **ShippingReference**: Tracking number, carrier info
- **TenantId**: Multi-tenant isolation
- **CreatedAt/By, ModifiedAt/By**: Audit fields

### TransferOrderRows Table
- **Id**: Unique identifier (GUID)
- **TransferOrderId**: Parent transfer order
- **ProductId**: Product being transferred
- **SourceLocationId**: Source storage location
- **DestinationLocationId**: Destination storage location
- **QuantityOrdered**: Requested quantity
- **QuantityShipped**: Actually shipped quantity
- **QuantityReceived**: Actually received quantity
- **LotId**: Optional lot tracking
- **Notes**: Row-specific notes
- **TenantId**: Multi-tenant isolation

## API Endpoints

### Base URL
```
/api/v1/transferorder
```

### Endpoints

#### List Transfer Orders
```http
GET /api/v1/transferorder?page=1&pageSize=20&sourceWarehouseId={guid}&destinationWarehouseId={guid}&status={status}&searchTerm={term}
```
Returns paginated list with filters.

#### Get Transfer Order
```http
GET /api/v1/transferorder/{id}
```
Returns complete transfer order with all rows.

#### Create Transfer Order
```http
POST /api/v1/transferorder
Content-Type: application/json

{
  "number": "TO-20251122-0001",
  "series": "TO",
  "orderDate": "2025-11-22T14:00:00Z",
  "sourceWarehouseId": "guid",
  "destinationWarehouseId": "guid",
  "notes": "Transfer for restocking",
  "rows": [
    {
      "productId": "guid",
      "sourceLocationId": "guid",
      "quantity": 10,
      "lotId": "guid",
      "notes": "Handle with care"
    }
  ]
}
```
Creates new transfer order in Pending status.

#### Ship Transfer Order
```http
POST /api/v1/transferorder/{id}/ship
Content-Type: application/json

{
  "shipmentDate": "2025-11-22T15:00:00Z",
  "expectedArrivalDate": "2025-11-23T10:00:00Z",
  "shippingReference": "TRACK123456"
}
```
Ships order, creates StockMovement OUT records, reduces stock at source.

#### Receive Transfer Order
```http
POST /api/v1/transferorder/{id}/receive
Content-Type: application/json

{
  "actualArrivalDate": "2025-11-23T09:30:00Z",
  "rows": [
    {
      "rowId": "guid",
      "destinationLocationId": "guid",
      "quantityReceived": 10
    }
  ]
}
```
Receives order, creates StockMovement IN records, increases stock at destination.

#### Cancel Transfer Order
```http
DELETE /api/v1/transferorder/{id}/cancel
```
Cancels order (only if not yet shipped).

## Business Rules

### Creation Rules
1. Source and destination warehouses must be different
2. At least one row required
3. Products and locations must exist
4. Source locations must belong to source warehouse
5. Auto-generates number if not provided (format: {Series}-{YYYYMMDD}-{Sequence})

### Shipping Rules
1. Only Pending orders can be shipped
2. Validates stock availability at source locations
3. Creates StockMovement records with MovementType=Transfer
4. Reduces stock quantity at source
5. Sets QuantityShipped = QuantityOrdered for all rows
6. Updates order status to Shipped

### Receiving Rules
1. Only Shipped or InTransit orders can be received
2. Destination locations must belong to destination warehouse
3. Creates or updates Stock entries at destination
4. Creates StockMovement records with MovementType=Transfer
5. Allows quantity discrepancies (quantity received can differ from shipped)
6. Updates order status to Completed
7. Receiving completes the transfer and makes stock available at destination

### Cancellation Rules
1. Only Draft or Pending orders can be cancelled
2. Cannot cancel shipped/completed orders
3. No stock reversals (cancel before shipping)

## Stock Movement Integration

### Outbound Movement (Shipping)
```csharp
StockMovement {
  MovementType = Transfer,
  FromLocationId = sourceLocationId,
  ToLocationId = null, // In transit
  Quantity = -quantityOrdered, // Negative
  Reference = transferOrderNumber,
  Status = Completed
}
```

### Inbound Movement (Receiving)
```csharp
StockMovement {
  MovementType = Transfer,
  FromLocationId = null, // From transit
  ToLocationId = destinationLocationId,
  Quantity = +quantityReceived, // Positive
  Reference = transferOrderNumber,
  Status = Completed
}
```

## UI Navigation

### Access
- Navigate to **Warehouse > Transfer Orders** from the main menu
- Requires warehouse management permissions

### Features
- **Search**: By order number or shipping reference
- **Filters**: Source warehouse, destination warehouse, status
- **Actions**:
  - View details
  - Ship (Pending orders)
  - Receive (Shipped orders)
  - Cancel (Pending orders)

### Management Page
Lists all transfer orders with:
- Order number and date
- Source and destination warehouses
- Current status (color-coded)
- Number of items
- Quick actions based on status

### Detail Page
Shows complete order information:
- Order header (number, dates, status)
- Warehouse information
- List of items with quantities
- Audit trail (created by, dates)

## Example Scenarios

### Scenario 1: Basic Transfer
1. **Create**: Transfer 50 units of Product A from Warehouse Main to Warehouse Regional
2. **Ship**: Execute shipment, stock reduced at Main warehouse
3. **Receive**: Confirm receipt at Regional warehouse, stock increased

### Scenario 2: Multi-Product Transfer
1. **Create**: Transfer order with multiple products and locations
2. **Ship**: All products shipped together
3. **Receive**: Products received in different destination locations

### Scenario 3: Discrepancy Handling
1. **Create**: Order 100 units
2. **Ship**: Ship 100 units
3. **Receive**: Receive only 98 units (2 damaged in transit)
4. **Result**: Stock movements reflect actual quantities

## Technical Implementation

### Service Layer
- **TransferOrderService**: Core business logic
- **Tenant Isolation**: All queries filtered by TenantId
- **Transaction Management**: Ship/Receive operations are atomic
- **Validation**: Comprehensive checks at each stage

### Data Access
- **Entity Framework Core**: ORM layer
- **Migration**: 20251122143916_AddTransferOrders
- **Indexes**: Optimized for common queries

### Frontend
- **Blazor Components**: Modern, responsive UI
- **MudBlazor**: Material Design components
- **Real-time Updates**: SignalR integration ready

## Security

### Authentication
- All endpoints require authentication (`[Authorize]`)
- License feature check: `ProductManagement`

### Authorization
- Tenant isolation enforced at service layer
- User actions logged in audit trail
- Role-based access control ready

### Data Protection
- Tenant data completely isolated
- Audit trail tracks all changes
- Soft delete support

## Performance Considerations

### Optimizations
- Paginated queries for large datasets
- Eager loading of related entities
- Indexed columns for fast filtering
- Efficient number generation

### Scalability
- Stateless service design
- Async/await throughout
- Cancellation token support
- Batch operations possible

## Future Enhancements

### Planned Features
1. **Smart Allocation**: Automatic transfer suggestions based on demand
2. **Batch Transfers**: Process multiple orders together
3. **Return Transfers**: Handle returns from destination to source
4. **Mobile App**: Scan and receive with mobile devices
5. **Barcode Integration**: Quick product/location scanning
6. **Email Notifications**: Alerts for shipment/receipt
7. **Advanced Reporting**: Analytics and KPIs
8. **Integration**: ERP, WMS, shipping carriers

## Troubleshooting

### Common Issues

**Issue**: Cannot create transfer order
- **Check**: Source and destination warehouses are different
- **Check**: All products and locations exist
- **Check**: User has required permissions

**Issue**: Cannot ship order
- **Check**: Order status is Pending
- **Check**: Sufficient stock available at source locations
- **Check**: No stock reservations blocking transfer

**Issue**: Cannot receive order
- **Check**: Order status is Shipped or InTransit
- **Check**: Destination locations belong to destination warehouse
- **Check**: Quantities are valid

## Support

For issues or questions:
- Check audit logs for detailed error information
- Review stock movements for transfer history
- Contact system administrator
- Consult EventForge documentation

## Version History

- **v1.0.0** (2025-11-22): Initial release
  - Core transfer order functionality
  - Ship and receive workflows
  - Audit trail integration
  - Basic UI implementation
