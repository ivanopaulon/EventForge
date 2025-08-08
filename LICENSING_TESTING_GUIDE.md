# Testing the EventForge Licensing System

This script demonstrates how to test the licensing system that has been implemented.

## Prerequisites

1. Run the application:
```bash
cd EventForge.Server
dotnet run
```

2. The application will be available at `https://localhost:7001` or `http://localhost:5001`

## Testing Scenarios

### 1. Create License Types (SuperAdmin only)

```bash
# Create Basic License
curl -X POST "https://localhost:7001/api/license" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_SUPER_ADMIN_TOKEN" \
  -d '{
    "name": "basic",
    "displayName": "Basic License",
    "description": "Licenza base con funzionalità essenziali",
    "maxUsers": 5,
    "maxApiCallsPerMonth": 1000,
    "tierLevel": 1,
    "isActive": true
  }'

# Create Premium License
curl -X POST "https://localhost:7001/api/license" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_SUPER_ADMIN_TOKEN" \
  -d '{
    "name": "premium",
    "displayName": "Premium License",
    "description": "Licenza premium con tutte le funzionalità",
    "maxUsers": 100,
    "maxApiCallsPerMonth": 50000,
    "tierLevel": 3,
    "isActive": true
  }'
```

### 2. Get All Licenses

```bash
curl -X GET "https://localhost:7001/api/license" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 3. Assign License to Tenant (SuperAdmin only)

```bash
curl -X POST "https://localhost:7001/api/license/assign" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_SUPER_ADMIN_TOKEN" \
  -d '{
    "tenantId": "YOUR_TENANT_ID",
    "licenseId": "BASIC_LICENSE_ID",
    "startsAt": "2024-01-01T00:00:00Z",
    "expiresAt": "2024-12-31T23:59:59Z",
    "isActive": true
  }'
```

### 4. Get Tenant License Information

```bash
curl -X GET "https://localhost:7001/api/license/tenant/YOUR_TENANT_ID" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 5. Test License Feature Requirements

```bash
# This should work if tenant has BasicEventManagement feature
curl -X GET "https://localhost:7001/api/v1/events" \
  -H "Authorization: Bearer YOUR_TOKEN"

# This should work if tenant has BasicTeamManagement feature  
curl -X GET "https://localhost:7001/api/v1/teams" \
  -H "Authorization: Bearer YOUR_TOKEN"

# This should require ProductManagement feature (Standard license or higher)
curl -X GET "https://localhost:7001/api/v1/products" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 6. Test API Limits

Make multiple calls to any protected endpoint to test API limiting:

```bash
# Make 1000+ calls to test API limits for basic license
for i in {1..1100}; do
  curl -X GET "https://localhost:7001/api/v1/events" \
    -H "Authorization: Bearer YOUR_TOKEN"
  echo "Call $i completed"
done
```

After 1000 calls (for basic license), you should receive a 429 (Too Many Requests) response.

## Expected Responses

### Success Responses

1. **License Creation**: Returns 201 Created with license details
2. **License Assignment**: Returns 201 Created with tenant license details  
3. **Feature Access**: Returns 200 OK with requested data
4. **API Usage**: Returns usage statistics

### Error Responses

1. **No License**: 403 Forbidden - "No active license found for tenant"
2. **Expired License**: 403 Forbidden - "License has expired or is not yet active"
3. **Missing Feature**: 403 Forbidden - "Feature 'X' is not available in current license"
4. **API Limit Exceeded**: 429 Too Many Requests - "API call limit exceeded for this month"
5. **Missing Permissions**: 403 Forbidden - "Missing required permissions: X, Y, Z"

## Manual Testing in Browser

1. Navigate to `https://localhost:7001/swagger`
2. Use the Swagger UI to test the licensing endpoints
3. Authenticate as SuperAdmin to create and assign licenses
4. Authenticate as regular user to test feature access

## Database Seeding

The system includes seed data that creates:
- 4 license types (Basic, Standard, Premium, Enterprise)
- Associated features for each license
- Permission mappings

You can call the seed method from the application startup or create a dedicated endpoint for testing.

## Monitoring and Analytics

Check the following tables in the database:
- `Licenses` - Available license types
- `LicenseFeatures` - Features per license
- `TenantLicenses` - License assignments to tenants
- `LicenseFeaturePermissions` - Permission mappings

Monitor API usage:
- `ApiCallsThisMonth` field in `TenantLicenses` table
- Check reset dates in `ApiCallsResetAt` field

## Troubleshooting

1. **License not working**: Check if tenant has an active license assigned
2. **Feature access denied**: Verify the license includes the required feature
3. **API limits not working**: Check if tenant ID is correctly identified in claims
4. **Permissions denied**: Ensure user has the required permissions for the feature

## Integration with Frontend

The licensing system can be integrated with the Blazor frontend by:
1. Checking license features before showing UI components
2. Displaying API usage statistics
3. Managing license assignments (SuperAdmin only)
4. Showing license expiration warnings