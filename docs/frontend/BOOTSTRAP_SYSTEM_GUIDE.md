# EventForge Bootstrap System

This document describes the automatic bootstrap routine implemented for EventForge that runs on server startup after applying EF Core migrations to create an initial environment when the database is empty.

## Overview

The bootstrap system automatically sets up a complete initial environment including:
- Default tenant
- SuperAdmin user with secure credentials
- System roles and permissions
- Basic license assignment
- AdminTenant access permissions

## Features

### 1. Automatic Detection and Updates
- **On every startup**, the system checks and updates bootstrap configuration data
- **License definitions** are automatically synchronized with code-defined defaults
- **License features** are kept in sync (new features added, obsolete ones deactivated)
- **Initial tenant creation** only happens when no tenants exist
- **Updates run automatically** even when tenants exist to keep system data current

### 2. Complete Initial Setup
When no tenants are found, the system creates:

**Default Tenant:**
- Name: "DefaultTenant"
- Code: "default" 
- DisplayName: "Default Tenant"
- ContactEmail: "superadmin@localhost"
- Domain: "localhost"
- MaxUsers: 10
- IsActive: true

**SuperAdmin User:**
- Username: "superadmin"
- Email: "superadmin@localhost"
- FirstName: "Super"
- LastName: "Admin"
- IsActive: true
- MustChangePassword: true
- Password: (configurable, see below)

**System Roles:**
- SuperAdmin (IsSystemRole = true)
- Admin, Manager, User, Viewer roles
- Complete permission system

**SuperAdmin License:**
- Name: "superadmin"
- DisplayName: "SuperAdmin License"
- Description: "SuperAdmin license with unlimited features for complete system management"
- MaxUsers: Unlimited (int.MaxValue)
- MaxApiCallsPerMonth: Unlimited (int.MaxValue)
- TierLevel: 5
- IsActive: true
- **Automatically updates** if code-defined defaults change

**Access Permissions:**
- TenantLicense assignment (active, starts immediately)
- AdminTenant record (FullAccess level)
- UserRole assignment (SuperAdmin role)

### 3. Password Configuration Priority

The SuperAdmin password is determined with the following precedence:

1. **Environment Variable** (highest priority)
   ```bash
   EVENTFORGE_BOOTSTRAP_SUPERADMIN_PASSWORD="YourSecurePassword123!"
   ```

2. **Configuration Setting**
   ```json
   {
     "Bootstrap": {
       "SuperAdminPassword": "YourConfigPassword123!"
     }
   }
   ```

3. **Fallback Default** (lowest priority)
   ```
   "SuperAdmin#2025!"
   ```

### 4. Comprehensive Logging

The system provides detailed logging including:
- Bootstrap process start/completion
- Each component creation (tenant, user, license, etc.)
- **License configuration updates** when defaults change
- **License feature synchronization** (additions, updates, deactivations)
- **Generated credentials are logged** for initial access
- Security warnings about password changes
- Update notifications when existing data is synchronized

Example log output for initial bootstrap:
```
info: Starting bootstrap process...
info: No tenants found. Starting initial bootstrap...
info: Default tenant created: DefaultTenant (Code: default)
info: SuperAdmin user created: superadmin (superadmin@localhost)
info: Password: SuperAdmin#2025!
warn: SECURITY: Please change the SuperAdmin password immediately after first login!
info: SuperAdmin license assigned with unlimited users and API calls, including all features
```

Example log output for subsequent runs with updates:
```
info: Starting bootstrap process...
info: Updating SuperAdmin license MaxUsers: 100 -> 2147483647
info: Updating SuperAdmin license MaxApiCallsPerMonth: 1000 -> 2147483647
info: SuperAdmin license updated with new configuration
info: Adding new SuperAdmin license feature: AdvancedSecurity
info: SuperAdmin license features synchronized: 1 added, 2 updated, 0 deactivated
info: Tenants already exist. Bootstrap data update completed.
```

## Configuration

### appsettings.json
```json
{
  "Bootstrap": {
    "SuperAdminPassword": "SuperAdmin#2025!",
    "DefaultAdminUsername": "superadmin",
    "DefaultAdminEmail": "superadmin@localhost", 
    "AutoCreateAdmin": true
  }
}
```

### Environment Variables
```bash
# Production recommended approach
EVENTFORGE_BOOTSTRAP_SUPERADMIN_PASSWORD="YourVerySecurePasswordHere123!"
```

## Hosted Service Implementation

The bootstrap process is handled by `BootstrapHostedService` which:

1. **Runs on application startup** (`StartAsync`)
2. **Applies EF Core migrations** first
3. **Executes bootstrap process** after migrations
4. **Handles errors gracefully** - application continues even if bootstrap fails
5. **Provides detailed logging** throughout the process

## Security Considerations

1. **Password Security:**
   - Use environment variables in production
   - Default passwords should be changed immediately
   - System logs credential information for initial setup
   - MustChangePassword flag forces password change on first login

2. **Initial Access:**
   - SuperAdmin has full system access
   - Can manage the default tenant
   - Has access to all system permissions

3. **Production Deployment:**
   - Set `EVENTFORGE_BOOTSTRAP_SUPERADMIN_PASSWORD` environment variable
   - Monitor logs for successful bootstrap completion
   - Change default password after first login

## Usage

### Development
1. Start the application
2. Check logs for bootstrap completion
3. Log in with: `superadmin` / configured password
4. Change password on first login

### Production
1. Set environment variable `EVENTFORGE_BOOTSTRAP_SUPERADMIN_PASSWORD`
2. Deploy and start the application
3. Verify bootstrap completion in logs
4. Log in and change password immediately

### Container/Docker
```bash
docker run -e EVENTFORGE_BOOTSTRAP_SUPERADMIN_PASSWORD="YourSecurePassword" eventforge:latest
```

## Customization

The bootstrap process can be customized through configuration:

```json
{
  "Bootstrap": {
    "SuperAdminPassword": "CustomPassword123!",
    "DefaultAdminUsername": "admin",
    "DefaultAdminEmail": "admin@company.com",
    "AutoCreateAdmin": true
  }
}
```

Note: The tenant values (DefaultTenant, default code, localhost domain, etc.) are fixed by design for consistency.

## Automatic Configuration Updates

### How It Works

Starting from this version, the bootstrap system automatically keeps your installation up to date with code-defined defaults:

1. **Every Application Startup:**
   - System checks SuperAdmin license configuration
   - Compares existing data with code-defined defaults
   - Updates any differences automatically
   - Synchronizes license features

2. **License Configuration Updates:**
   - DisplayName, Description
   - MaxUsers, MaxApiCallsPerMonth
   - TierLevel, IsActive status
   - All properties compared and updated if different

3. **License Features Synchronization:**
   - New features are automatically added
   - Existing features are updated if properties changed
   - Obsolete features are marked as inactive
   - All changes are logged for transparency

### Benefits

- **No Manual Intervention:** System stays current automatically
- **Version Upgrades:** New features and limits are applied seamlessly
- **Consistency:** All installations match the intended configuration
- **Audit Trail:** All updates are logged with detailed information
- **Safe Updates:** Only system-level data is updated (preserves user data)

### Example Scenario

If you update the code to change the SuperAdmin license from:
```csharp
MaxUsers = 1000
MaxApiCallsPerMonth = 100000
```

To:
```csharp
MaxUsers = int.MaxValue  // Unlimited
MaxApiCallsPerMonth = int.MaxValue  // Unlimited
```

The system will automatically:
1. Detect the difference on next startup
2. Update the license record in the database
3. Log the changes: `"Updating SuperAdmin license MaxUsers: 1000 -> 2147483647"`
4. Continue without requiring manual intervention

### What Gets Updated

✅ **Always Updated:**
- SuperAdmin license properties
- SuperAdmin license features
- System roles and permissions

❌ **Never Updated:**
- User-created tenants
- User accounts
- User-created licenses
- Tenant-specific data
- User settings and preferences

## Troubleshooting

### Bootstrap Not Running
- Check if database connection is available
- Verify application logs for errors
- Ensure migrations have been applied

### License Not Updating
- Check application logs for "SuperAdmin license" messages
- Verify the license exists in database with name "superadmin"
- Look for error messages in startup logs
- Ensure database write permissions are available

### Features Not Synchronizing
- Review logs for "license features synchronized" messages
- Check if ModifiedAt timestamp is updated on license features
- Verify no database constraints are blocking updates

### Password Issues
- Verify password meets policy requirements (8+ chars, uppercase, lowercase, digits, special chars)
- Check environment variable is set correctly
- Ensure configuration syntax is valid

### Permission Issues
- Verify SuperAdmin role was created
- Check UserRole assignment in database
- Confirm AdminTenant record exists

## Technical Details

- **Service:** `BootstrapService` implements `IBootstrapService`
- **Hosted Service:** `BootstrapHostedService` implements `IHostedService`
- **Registration:** Automatically registered in DI container
- **Dependencies:** EventForgeDbContext, IPasswordService, IConfiguration, ILogger
- **Database:** Uses EF Core migrations and entity framework
- **Update Strategy:** Compare-and-update pattern for all bootstrap data
- **Execution:** Runs on every application startup
- **Performance:** Optimized to only update changed values
- **Safety:** Preserves user data, only updates system-level configuration