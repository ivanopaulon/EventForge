# Testing Guide for PriceTrendChart Component

## Overview
The PriceTrendChart component tracks purchase and sale prices of products daily, displaying trends and statistics in the "Prezzi e Finanza" (Prices and Finance) tab of the product detail page.

## What Was Implemented

### 1. Backend (Server)
- **PriceTrendDto** (`EventForge.DTOs/Warehouse/PriceTrendDto.cs`): New DTO for price trend data
- **GetProductPriceTrend** endpoint in `ProductManagementController`: API endpoint at `GET /api/product-management/products/{id}/price-trend`

### 2. Frontend (Client)
- **GetProductPriceTrendAsync** in `IProductService` and `ProductService`: Service method to call the API
- **PriceTrendChart.razor**: New component displaying price trends with charts
- **Updated PricingFinancialTab.razor**: Integrated the new component

## Features

### Price Statistics
- **Average Purchase Price**: Weighted average of all purchase prices
- **Average Sale Price**: Weighted average of all sale prices
- **Price Ranges**: Min and max for both purchase and sale prices
- **Margin Analysis**: Automatic calculation of profit margin

### Chart Visualization
- Line chart showing purchase prices over time (blue line)
- Line chart showing sale prices over time (green line)
- Daily aggregation of prices
- X-axis shows dates in dd/MM format
- Y-axis shows prices

## How to Test Manually

### Prerequisites
1. Application must be running (both server and client)
2. Database must contain:
   - At least one product
   - Document headers with rows containing price information
   - Documents should be marked as either purchases or sales

### Test Steps

1. **Navigate to a Product Detail Page**
   - Go to Product Management → Products
   - Click on any existing product

2. **Navigate to "Prezzi e Finanza" Tab**
   - Click on the "Prezzi e Finanza" (Prices and Finance) tab

3. **Verify Price Trend Chart Display**
   - The component should load below the pricing information fields
   - If the product has document movements with prices, you should see:
     - Two statistics cards showing average purchase and sale prices
     - A margin analysis card (if both purchase and sale prices exist)
     - A line chart with up to two lines (purchase prices in one color, sale prices in another)

4. **Test Edge Cases**
   - **No Price Data**: If product has no document movements, should display "Nessun dato di prezzi disponibile per questo periodo"
   - **Only Purchase Prices**: Should only show purchase price line
   - **Only Sale Prices**: Should only show sale price line
   - **Loading State**: Should show a loading spinner while fetching data

### Expected Results

#### Statistics Cards
- **Purchase Price Card (Left)**
  - Title: "Prezzo Acquisto Medio"
  - Displays current weighted average purchase price
  - Shows range with min and max purchase prices
  - Blue color scheme

- **Sale Price Card (Right)**
  - Title: "Prezzo Vendita Medio"
  - Displays current weighted average sale price
  - Shows range with min and max sale prices
  - Green color scheme

#### Margin Analysis Card (appears only if both prices exist)
- Title: "Margine Medio"
- Shows calculated margin in euros and percentage
- Green color for positive margin, red for negative

#### Chart
- Title: "Andamento Prezzi" with trending up icon
- X-axis: Dates in dd/MM format
- Y-axis: Price values
- Legend: "Prezzi Acquisto" and "Prezzi Vendita"
- Footer: Shows the year of the data

## API Endpoints

### Get Price Trend
```
GET /api/product-management/products/{productId}/price-trend?year={year}
```

**Parameters:**
- `productId` (required): GUID of the product
- `year` (optional): Year to fetch data for (defaults to current year)

**Response:**
```json
{
  "productId": "guid",
  "year": 2025,
  "purchasePrices": [
    {
      "date": "2025-01-15T00:00:00",
      "price": 10.50,
      "quantity": 100,
      "documentType": "Purchase Order",
      "businessPartyName": "Supplier XYZ"
    }
  ],
  "salePrices": [
    {
      "date": "2025-01-20T00:00:00",
      "price": 15.75,
      "quantity": 50,
      "documentType": "Invoice",
      "businessPartyName": "Customer ABC"
    }
  ],
  "currentAveragePurchasePrice": 10.50,
  "currentAverageSalePrice": 15.75,
  "minPurchasePrice": 10.00,
  "maxPurchasePrice": 11.00,
  "minSalePrice": 15.00,
  "maxSalePrice": 16.50,
  "averagePurchasePrice": 10.50,
  "averageSalePrice": 15.75
}
```

## Data Source

The price data is extracted from:
- **Document Headers**: Purchase orders, invoices, etc.
- **Document Rows**: Specifically the `UnitPrice` field for each product
- **Document Type Classification**: Determined by document type name (purchase keywords vs. sale keywords)

Purchase keywords: "purchase", "receipt", "return", "acquisto", "carico", "reso"
Sale keywords: "sale", "invoice", "shipment", "delivery", "vendita", "fattura", "scarico", "consegna"

## Translation Keys

The component uses the following translation keys (with Italian defaults):

- `product.priceTrend`: "Andamento Prezzi"
- `price.averagePurchase`: "Prezzo Acquisto Medio"
- `price.averageSale`: "Prezzo Vendita Medio"
- `price.range`: "Range"
- `price.averageMargin`: "Margine Medio"
- `price.purchase`: "Prezzi Acquisto"
- `price.sale`: "Prezzi Vendita"
- `price.trendYear`: "Anno"
- `product.noPriceTrendData`: "Nessun dato di prezzi disponibile per questo periodo"

## Notes

- The component only loads price trend data once when the tab is first opened
- Data is cached to avoid repeated API calls
- Prices are weighted by quantity for more accurate averages
- The chart aggregates multiple prices on the same day by averaging them
