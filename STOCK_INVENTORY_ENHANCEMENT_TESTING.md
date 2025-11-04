# Stock Inventory Tab Enhancement - Testing Guide

## Overview
This implementation adds document movement tracking with quantities and a stock trend chart to the product detail page (StockInventoryTab).

## New Features

### 1. Document Movements Table (Enhanced)
The document history table now displays:
- **Quantity**: The actual quantity of the product in each document
- **Unit of Measure (UM)**: The unit of measure for the quantity
- **Movement Type**: Visual indicator (with icon) showing if it's a stock increase (Carico) or decrease (Scarico)
- **Warehouse**: The warehouse involved in the movement

### 2. Stock Trend Chart
A new MudChart line chart component that displays:
- **Visual Chart**: Monthly stock levels over the current year
- **Statistics Cards**: Current stock, Average, Minimum, and Maximum stock levels
- **Dynamic Data**: Based on stock movements from the current year

## API Endpoints

### Get Product Document Movements
```
GET /api/v1/product-management/products/{id}/document-movements
```

Query Parameters:
- `fromDate` (optional): Filter start date
- `toDate` (optional): Filter end date
- `businessPartyName` (optional): Filter by customer/supplier name
- `page` (default: 1): Page number
- `pageSize` (default: 10): Items per page

Response: `PagedResult<ProductDocumentMovementDto>`

### Get Product Stock Trend
```
GET /api/v1/product-management/products/{id}/stock-trend
```

Query Parameters:
- `year` (optional, default: current year): Year for trend data

Response: `StockTrendDto`

## Testing Instructions

### 1. Test Document Movements
1. Navigate to a product detail page
2. Click on the "Magazzino e Inventario" tab
3. Verify the table shows document movements with:
   - Quantity column with values
   - UM (Unit of Measure) column
   - Movement Type column with colored chips and icons
   - Warehouse column
4. Test filtering by date range and customer/supplier
5. Test pagination if there are more than 10 movements

### 2. Test Stock Trend Chart
1. On the same tab, verify the stock trend chart appears
2. Check that statistics cards show:
   - Current stock value
   - Average stock
   - Minimum stock
   - Maximum stock
3. Verify the line chart displays monthly data points
4. Check that the year is displayed below the chart

### 3. Test Loading States
1. Verify loading spinners appear while data is being fetched
2. Check that appropriate messages are shown when no data exists
3. Test error handling by checking console for any errors

## Translation Keys

New translation keys used (with Italian fallbacks):
- `product.stockTrend`: "Andamento Giacenza"
- `stock.current`: "Attuale"
- `stock.average`: "Media"
- `stock.minimum`: "Minimo"
- `stock.maximum`: "Massimo"
- `stock.quantity`: "Quantità"
- `stock.trendYear`: "Anno"
- `product.noStockTrendData`: "Nessun dato di giacenza disponibile per questo periodo"
- `field.quantity`: "Quantità"
- `field.unitOfMeasure`: "UM"
- `field.movementType`: "Movimento"
- `field.warehouse`: "Magazzino"
- `movement.stockIncrease`: "Carico"
- `movement.stockDecrease`: "Scarico"

## Technical Details

### Components Created
- `StockTrendChart.razor`: Reusable MudChart component for displaying stock trends

### DTOs Created
- `ProductDocumentMovementDto`: Contains document information plus product-specific quantities
- `StockTrendDto`: Contains trend data with statistics
- `StockTrendDataPoint`: Individual data point in the trend

### Services Modified
- `IProductService` and `ProductService`: Added two new methods
- `ProductManagementController`: Added two new endpoints

### Files Modified
- `StockInventoryTab.razor`: Updated to use new service methods and display enhanced data
- Test files updated to include new dependencies

## Known Limitations

1. The stock trend chart aggregates data by month for better visualization
2. The movement type determination is based on document type name keywords
3. Pagination for document movements is currently limited to the total count of movements found
4. The chart uses monthly averages for better visualization when there are many data points

## Future Enhancements

1. Add ability to export document movements to CSV/Excel
2. Add more granular filtering options (document type, warehouse)
3. Add ability to select different time periods for the trend chart
4. Add comparative analysis (year-over-year comparison)
5. Add alerts/notifications for low stock conditions
