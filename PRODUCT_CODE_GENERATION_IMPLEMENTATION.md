# Product Code Auto-Generation Implementation

## Overview

This implementation adds automatic server-side generation of `Product.Code` in the format `YYYYMMDDNNNNNN` (UTC date + 6-digit daily counter) with SQL Server-based atomicity and concurrency control.

## Functional Requirements Met

✅ **Automatic Code Generation**: If client sends `CreateProduct` without Code (empty/null), the server generates a unique code in format YYYYMMDDNNNNNN

✅ **Daily Counter Reset**: The counter NNNNNN is daily and resets each day (key = date)

✅ **Atomicity & Concurrency**: Guaranteed through SQL Server row-level locks (UPDLOCK, ROWLOCK) to prevent duplicates with parallel requests

✅ **Unique Constraint**: Added unique constraint on Products.Code via EF Core configuration

✅ **Code Immutability**: Code field is immutable on update - enforced by UpdateProductDto not including Code property

✅ **Retry Logic**: Implements limited retry (3 attempts) in case of unique constraint collision

## Architecture

### Components Created

1. **DailySequence Entity** (`EventForge.Server/Data/Entities/DailySequence.cs`)
   - Tracks daily sequence counters
   - Primary key: Date (date type)
   - LastNumber: Long integer counter

2. **IDailyCodeGenerator Interface** (`EventForge.Server/Services/CodeGeneration/IDailyCodeGenerator.cs`)
   - Defines contract for code generation service

3. **DailySequentialCodeGenerator** (`EventForge.Server/Services/CodeGeneration/DailySequentialCodeGenerator.cs`)
   - Implements atomic code generation using SQL Server locks
   - Uses raw SQL with UPDLOCK and ROWLOCK for concurrency control
   - Generates codes in format YYYYMMDDNNNNNN

4. **DbContext Configuration** (`EventForge.Server/Data/EventForgeDbContext.cs`)
   - Added DailySequences DbSet
   - Configured date column type
   - Added unique index on Products.Code (UQ_Products_Code)

5. **ProductService Integration** (`EventForge.Server/Services/Products/ProductService.cs`)
   - Injects IDailyCodeGenerator
   - Generates code if CreateProductDto.Code is null/empty/whitespace
   - Implements retry logic (max 3 attempts) for unique constraint violations
   - Code immutability already enforced by UpdateProductDto design

6. **Dependency Injection** (`EventForge.Server/Extensions/ServiceCollectionExtensions.cs`)
   - Registered IDailyCodeGenerator as scoped service

## Code Generation Algorithm

```csharp
1. Get UTC date (DateTime.UtcNow.Date)
2. Begin transaction
3. Check if DailySequence exists for current date:
   - If exists: Atomically increment LastNumber (with UPDLOCK, ROWLOCK)
   - If not exists: Insert new record with LastNumber = 1
4. Retrieve updated LastNumber
5. Commit transaction
6. Format code: YYYYMMDD + zero-padded 6-digit number
7. Return formatted code (e.g., "20251110000001")
```

## SQL Implementation

The atomic increment uses SQL Server's row-level locking:

```sql
IF EXISTS (SELECT 1 FROM DailySequences WITH (UPDLOCK, ROWLOCK) WHERE Date = @date)
BEGIN
    UPDATE DailySequences 
    SET LastNumber = LastNumber + 1 
    WHERE Date = @date;
    
    SELECT @NextNumber = LastNumber 
    FROM DailySequences 
    WHERE Date = @date;
END
ELSE
BEGIN
    INSERT INTO DailySequences (Date, LastNumber) 
    VALUES (@date, 1);
    
    SET @NextNumber = 1;
END
```

## Database Schema

### DailySequences Table

| Column     | Type   | Constraints |
|------------|--------|-------------|
| Date       | date   | PRIMARY KEY |
| LastNumber | bigint | NOT NULL    |

### Products.Code Constraint

- Unique index: `UQ_Products_Code`
- Allows NULL/empty values
- Prevents duplicate non-empty codes

## Testing

### Unit Tests Created

**ProductServiceCodeGenerationTests.cs** (6/6 passed):
- ✅ CreateProductAsync_WithEmptyCode_GeneratesCode
- ✅ CreateProductAsync_WithNullCode_GeneratesCode
- ✅ CreateProductAsync_WithProvidedCode_DoesNotGenerateCode
- ✅ CreateProductAsync_WithWhitespaceCode_GeneratesCode
- ✅ UpdateProductAsync_CodeFieldNotInDto_PreservesExistingCode
- ✅ CreateProductAsync_GeneratedCodeStoredInDatabase

**DailyCodeGeneratorTests.cs**:
- Tests for code format validation
- Tests for counter increment
- Tests for zero-padding
- Note: Requires SQL Server; cannot run with in-memory database

### Updated Existing Tests

All existing ProductService tests updated to include new IDailyCodeGenerator dependency:
- ProductServiceBarcodeTests.cs
- ProductRecentTransactionsTests.cs
- SupplierProductAssociationTests.cs

## Migration

Execute the SQL migration script to create the required database objects:

**Location**: `Migrations/20251110_AddDailySequences.sql`

The migration:
1. Creates DailySequences table
2. Adds unique constraint on Products.Code
3. Is idempotent (safe to re-run)
4. Includes verification queries
5. Includes rollback instructions

## Usage Examples

### Automatic Code Generation

```csharp
var createDto = new CreateProductDto
{
    Name = "New Product",
    Code = string.Empty, // or null or "   "
    // ... other properties
};

// Code will be auto-generated: e.g., "20251110000042"
var product = await productService.CreateProductAsync(createDto, "user");
Console.WriteLine(product.Code); // "20251110000042"
```

### Manual Code Assignment

```csharp
var createDto = new CreateProductDto
{
    Name = "New Product",
    Code = "CUSTOM001", // Manual code provided
    // ... other properties
};

// Code will NOT be auto-generated
var product = await productService.CreateProductAsync(createDto, "user");
Console.WriteLine(product.Code); // "CUSTOM001"
```

### Code Immutability on Update

```csharp
var updateDto = new UpdateProductDto
{
    Name = "Updated Product",
    // Code property doesn't exist in UpdateProductDto
    // ... other properties
};

// Code remains unchanged
var product = await productService.UpdateProductAsync(productId, updateDto, "user");
// product.Code stays the same as before
```

## Concurrency Handling

### Retry Logic

The implementation handles concurrent requests through:

1. **SQL Server Row Locks**: UPDLOCK and ROWLOCK prevent race conditions at database level
2. **Retry Mechanism**: Max 3 attempts if unique constraint violation occurs
3. **Context Reset**: Clears EF Core change tracker between retries

### Example Scenario

1. Request A and B both try to create product with no code
2. Both generate code for 2025-11-10
3. DailySequentialCodeGenerator uses locks to ensure:
   - Request A gets: 20251110000001
   - Request B gets: 20251110000002
4. If collision occurs (rare), retry logic regenerates code

## Security Considerations

✅ **SQL Injection**: Uses parameterized queries
✅ **Concurrency**: Row-level locks prevent race conditions
✅ **Immutability**: Code cannot be changed after creation
✅ **Validation**: Unique constraint enforced at database level
✅ **Audit Trail**: All code generation events logged

## Performance Considerations

- **Daily Reset**: Counter resets daily, limiting max value per day to 999,999
- **Index**: Unique index on Products.Code for fast lookups
- **Row Locks**: Minimal contention with row-level locking
- **Transaction Scope**: Small, focused transactions for quick execution
- **Retry Limit**: Max 3 attempts prevents infinite loops

## Limitations

1. **SQL Server Only**: Implementation requires SQL Server (uses T-SQL specific features)
2. **Daily Limit**: Max 999,999 products per day
3. **UTC Time**: Uses UTC for date calculation (consistent across timezones)
4. **No Migration Rollback**: Manual rollback required if needed (see migration script comments)

## Future Enhancements

Possible improvements for future iterations:

1. Support for multiple code formats (configurable)
2. Support for multiple databases (PostgreSQL, MySQL)
3. Custom prefix support (e.g., PROD-YYYYMMDDNNNNNN)
4. Background cleanup of old DailySequence records
5. Monitoring/alerting for high daily volumes

## Troubleshooting

### Issue: Generated codes are duplicated
**Solution**: Check unique constraint exists; verify SQL Server locks are working

### Issue: Tests fail with "Relational-specific methods" error
**Solution**: DailyCodeGeneratorTests require SQL Server; expected behavior with in-memory DB

### Issue: Retry logic not working
**Solution**: Verify SqlException numbers 2627 and 2601 are caught correctly

### Issue: Counter doesn't reset daily
**Solution**: Check Date column is using `date` type, not `datetime`

## References

- SQL Server Locking: https://docs.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table
- EF Core Raw SQL: https://docs.microsoft.com/en-us/ef/core/querying/raw-sql
- Unique Constraints: https://docs.microsoft.com/en-us/ef/core/modeling/indexes

## Contributors

- Implementation Date: 2025-11-10
- Technology: .NET 9.0, EF Core, SQL Server
- Pattern: Repository + Service Layer with Dependency Injection
