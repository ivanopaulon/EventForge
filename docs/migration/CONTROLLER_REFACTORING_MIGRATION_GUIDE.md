# Controller Refactoring Breaking Changes & Migration Guide

## Overview

This document outlines the breaking changes introduced by the EventForge controller refactoring and provides a migration guide for clients using the API.

## Controller Consolidation

### Entity Management Consolidation

**Old Endpoints (Deprecated)**:
- `GET /api/v1/addresses`
- `GET /api/v1/contacts` 
- `GET /api/v1/classification-nodes`

**New Endpoints**:
- `GET /api/v1/entities/addresses`
- `GET /api/v1/entities/contacts`
- `GET /api/v1/entities/classification-nodes`

#### Migration Steps:
1. Update base URLs from `/api/v1/{entity}` to `/api/v1/entities/{entity}`
2. All existing query parameters remain the same
3. Response formats are unchanged

**Detailed Mapping**:

| Old Endpoint | New Endpoint | Notes |
|--------------|-------------|--------|
| `GET /api/v1/addresses` | `GET /api/v1/entities/addresses` | Pagination parameters unchanged |
| `GET /api/v1/addresses/owner/{ownerId}` | `GET /api/v1/entities/addresses/owner/{ownerId}` | Owner filtering unchanged |
| `GET /api/v1/addresses/{id}` | `GET /api/v1/entities/addresses/{id}` | Single resource retrieval |
| `POST /api/v1/addresses` | `POST /api/v1/entities/addresses` | Create operations unchanged |
| `GET /api/v1/contacts` | `GET /api/v1/entities/contacts` | Pagination parameters unchanged |
| `GET /api/v1/contacts/owner/{ownerId}` | `GET /api/v1/entities/contacts/owner/{ownerId}` | Owner filtering unchanged |
| `GET /api/v1/classification-nodes` | `GET /api/v1/entities/classification-nodes` | Hierarchical filtering unchanged |
| `GET /api/v1/classification-nodes/root` | `GET /api/v1/entities/classification-nodes/root` | Root node retrieval |

### Financial Entities Consolidation

**Old Endpoints (Deprecated)**:
- `GET /api/v1/banks`
- `GET /api/v1/vat-rates`
- `GET /api/v1/payment-terms`

**New Endpoints**:
- `GET /api/v1/financial/banks`
- `GET /api/v1/financial/vat-rates`
- `GET /api/v1/financial/payment-terms`

#### Migration Steps:
1. Update base URLs from `/api/v1/{entity}` to `/api/v1/financial/{entity}`
2. All existing query parameters remain the same
3. Response formats are unchanged

**Detailed Mapping**:

| Old Endpoint | New Endpoint | Notes |
|--------------|-------------|--------|
| `GET /api/v1/banks` | `GET /api/v1/financial/banks` | Pagination parameters unchanged |
| `GET /api/v1/banks/{id}` | `GET /api/v1/financial/banks/{id}` | Single resource retrieval |
| `POST /api/v1/banks` | `POST /api/v1/financial/banks` | Create operations unchanged |
| `GET /api/v1/vat-rates` | `GET /api/v1/financial/vat-rates` | Pagination parameters unchanged |
| `GET /api/v1/vat-rates/{id}` | `GET /api/v1/financial/vat-rates/{id}` | Single resource retrieval |
| `GET /api/v1/payment-terms` | `GET /api/v1/financial/payment-terms` | Pagination parameters unchanged |
| `GET /api/v1/payment-terms/{id}` | `GET /api/v1/financial/payment-terms/{id}` | Single resource retrieval |

## Enhanced Multi-Tenant Support

### New Response Codes

All endpoints now include consistent multi-tenant validation:

- **403 Forbidden**: Added to all endpoints when user doesn't have access to the current tenant
- **Enhanced 400 Bad Request**: Now uses standardized `ValidationProblemDetails` format

### Enhanced Error Responses

**Old Error Format**:
```json
{
  "message": "Page number must be greater than 0."
}
```

**New Error Format**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "Page number must be greater than 0.",
  "instance": "/api/v1/entities/addresses",
  "correlationId": "abc123",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Implementation Timeline

### Phase 1: Deprecation Period (Immediate)
- Old endpoints remain functional but marked as deprecated
- New endpoints are available and should be used for new integrations
- Documentation updated to reflect new endpoints

### Phase 2: Client Migration (30 days)
- Clients should migrate to new endpoints
- Monitoring will track usage of deprecated endpoints

### Phase 3: Removal (90 days)
- Deprecated endpoints will be removed
- Only new consolidated endpoints will be available

## Advantages of New Structure

1. **Logical Grouping**: Related entities are grouped under common base paths
2. **Reduced Endpoint Fragmentation**: From 6 controllers to 2 for common operations
3. **Enhanced Multi-Tenant Security**: Consistent tenant validation across all endpoints
4. **Improved Error Handling**: Standardized error responses with correlation IDs
5. **Better API Discoverability**: Clear categorization of entity types

## Client Migration Examples

### JavaScript/TypeScript

**Old Code**:
```typescript
// Old endpoints
const addresses = await fetch('/api/v1/addresses');
const banks = await fetch('/api/v1/banks');
```

**New Code**:
```typescript
// New consolidated endpoints
const addresses = await fetch('/api/v1/entities/addresses');
const banks = await fetch('/api/v1/financial/banks');
```

### C# HttpClient

**Old Code**:
```csharp
// Old endpoints
var addresses = await httpClient.GetAsync("api/v1/addresses");
var banks = await httpClient.GetAsync("api/v1/banks");
```

**New Code**:
```csharp
// New consolidated endpoints
var addresses = await httpClient.GetAsync("api/v1/entities/addresses");
var banks = await httpClient.GetAsync("api/v1/financial/banks");
```

## Support

For questions about migration or issues with the new endpoints:
1. Check this migration guide
2. Review the updated Swagger documentation
3. Contact the development team with specific migration questions

## Rollback Plan

If critical issues are discovered:
1. Old endpoints can be temporarily re-enabled
2. Hotfix deployment process available
3. Client applications can continue using old endpoints during emergency rollback