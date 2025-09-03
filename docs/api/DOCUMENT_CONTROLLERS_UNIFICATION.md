# Document Controllers Unification - Migration Guide

## Overview

The document-related API controllers have been unified into a single `DocumentsController` to provide a more cohesive and easier-to-use API. The specialized controllers are now **DEPRECATED** but remain functional for backward compatibility.

## Deprecated Controllers

The following controllers are marked as deprecated and will be removed in a future version:

- `DocumentTemplatesController`
- `DocumentCommentsController` 
- `DocumentWorkflowsController`
- `DocumentAnalyticsController`
- `DocumentAttachmentsController`

## Migration Mapping

### DocumentTemplatesController → DocumentsController

**OLD ROUTES (DEPRECATED):**
```
GET    /api/v1/DocumentTemplates
GET    /api/v1/DocumentTemplates/public
GET    /api/v1/DocumentTemplates/by-document-type/{id}
GET    /api/v1/DocumentTemplates/by-category/{category}
GET    /api/v1/DocumentTemplates/{id}
POST   /api/v1/DocumentTemplates
PUT    /api/v1/DocumentTemplates/{id}
DELETE /api/v1/DocumentTemplates/{id}
PATCH  /api/v1/DocumentTemplates/{id}/usage
```

**NEW ROUTES (UNIFIED):**
```
GET    /api/v1/documents/templates
GET    /api/v1/documents/templates/public
GET    /api/v1/documents/templates/by-document-type/{id}
GET    /api/v1/documents/templates/by-category/{category}
GET    /api/v1/documents/templates/{id}
POST   /api/v1/documents/templates
PUT    /api/v1/documents/templates/{id}
DELETE /api/v1/documents/templates/{id}
PATCH  /api/v1/documents/templates/{id}/usage
```

### DocumentCommentsController → DocumentsController

**OLD ROUTES (DEPRECATED):**
```
GET    /api/v1/DocumentComments/document-header/{id}
GET    /api/v1/DocumentComments/document-row/{id}
GET    /api/v1/DocumentComments/{id}
POST   /api/v1/DocumentComments
PUT    /api/v1/DocumentComments/{id}
POST   /api/v1/DocumentComments/{id}/resolve
POST   /api/v1/DocumentComments/{id}/reopen
GET    /api/v1/DocumentComments/document-header/{id}/stats
GET    /api/v1/DocumentComments/assigned
DELETE /api/v1/DocumentComments/{id}
HEAD   /api/v1/DocumentComments/{id}
GET    /api/v1/DocumentComments/{id}/exists
```

**NEW ROUTES (UNIFIED):**
```
GET    /api/v1/documents/{documentId}/comments
GET    /api/v1/documents/comments/document-row/{id}
GET    /api/v1/documents/comments/{id}
POST   /api/v1/documents/comments
PUT    /api/v1/documents/comments/{id}
POST   /api/v1/documents/comments/{id}/resolve
POST   /api/v1/documents/comments/{id}/reopen
GET    /api/v1/documents/{documentId}/comments/stats
GET    /api/v1/documents/comments/assigned
DELETE /api/v1/documents/comments/{id}
HEAD   /api/v1/documents/comments/{id}
GET    /api/v1/documents/comments/{id}/exists
```

### DocumentWorkflowsController → DocumentsController

**OLD ROUTES (DEPRECATED):**
```
GET    /api/v1/DocumentWorkflows
GET    /api/v1/DocumentWorkflows/{id}
POST   /api/v1/DocumentWorkflows
PUT    /api/v1/DocumentWorkflows/{id}
DELETE /api/v1/DocumentWorkflows/{id}
```

**NEW ROUTES (UNIFIED):**
```
GET    /api/v1/documents/workflows
GET    /api/v1/documents/{documentId}/workflows
GET    /api/v1/documents/workflows/{id}
POST   /api/v1/documents/workflows
PUT    /api/v1/documents/workflows/{id}
DELETE /api/v1/documents/workflows/{id}
```

### DocumentAnalyticsController → DocumentsController

**OLD ROUTES (DEPRECATED):**
```
GET    /api/v1/DocumentAnalytics/document/{id}
GET    /api/v1/DocumentAnalytics/summary
GET    /api/v1/DocumentAnalytics/kpi
POST   /api/v1/DocumentAnalytics/document/{id}/refresh
POST   /api/v1/DocumentAnalytics/document/{id}/events
```

**NEW ROUTES (UNIFIED):**
```
GET    /api/v1/documents/{documentId}/analytics
GET    /api/v1/documents/analytics/summary
GET    /api/v1/documents/analytics/kpi
POST   /api/v1/documents/{documentId}/analytics/refresh
POST   /api/v1/documents/{documentId}/analytics/events
```

### DocumentAttachmentsController → DocumentsController

**OLD ROUTES (DEPRECATED):**
```
GET    /api/v1/DocumentAttachments/document-header/{id}
GET    /api/v1/DocumentAttachments/document-row/{id}
GET    /api/v1/DocumentAttachments/{id}
POST   /api/v1/DocumentAttachments
PUT    /api/v1/DocumentAttachments/{id}
POST   /api/v1/DocumentAttachments/{id}/versions
GET    /api/v1/DocumentAttachments/{id}/versions
POST   /api/v1/DocumentAttachments/{id}/sign
GET    /api/v1/DocumentAttachments/category/{category}
DELETE /api/v1/DocumentAttachments/{id}
HEAD   /api/v1/DocumentAttachments/{id}
GET    /api/v1/DocumentAttachments/{id}/exists
```

**NEW ROUTES (UNIFIED):**
```
GET    /api/v1/documents/{documentId}/attachments
GET    /api/v1/documents/attachments/document-row/{id}
GET    /api/v1/documents/attachments/{id}
POST   /api/v1/documents/attachments
PUT    /api/v1/documents/attachments/{id}
POST   /api/v1/documents/attachments/{id}/versions
GET    /api/v1/documents/attachments/{id}/versions
POST   /api/v1/documents/attachments/{id}/sign
GET    /api/v1/documents/attachments/category/{category}
DELETE /api/v1/documents/attachments/{id}
HEAD   /api/v1/documents/attachments/{id}
GET    /api/v1/documents/attachments/{id}/exists
```

## Migration Strategy

### Phase 1: Immediate (Current)
- ✅ All specialized controllers marked as `[Obsolete]`
- ✅ Unified `DocumentsController` provides all functionality
- ✅ Backward compatibility maintained
- ✅ Deprecation warnings displayed in IDE and build output

### Phase 2: Transition Period (Next 2-3 releases)
- Update client applications to use unified API routes
- Monitor usage of deprecated endpoints
- Provide migration tools if needed

### Phase 3: Removal (Future major version)
- Remove deprecated controllers entirely
- Clean up unused service interfaces if applicable

## Benefits of Unification

1. **Consistency**: All document operations under `/api/v1/documents/*`
2. **Discoverability**: Easier to find related functionality
3. **Maintenance**: Single controller to maintain vs. 5 separate ones
4. **Documentation**: Unified API documentation
5. **Testing**: Consolidated test scenarios

## Implementation Details

The unified `DocumentsController`:
- Uses hybrid approach: `IDocumentFacade` for basic operations + individual services for advanced functionality
- Maintains all existing functionality and error handling
- Preserves multi-tenant support and authorization
- Uses same DTOs and service interfaces
- Provides comprehensive Swagger documentation

## Notes for Developers

- The deprecated controllers will generate compiler warnings when referenced
- All functionality remains exactly the same, only URLs have changed
- Response formats and status codes are identical
- Authentication and authorization requirements unchanged
- Rate limiting and other middleware still applies