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

### 1. Automatic Detection
- Checks if any tenants exist in the database
- If tenants exist, skips the entire bootstrap process
- Only runs when starting with a completely empty database

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

**Basic License:**
- Name: "basic"
- DisplayName: "Basic License"
- MaxUsers: 10
- MaxApiCallsPerMonth: 1000
- TierLevel: 1
- IsActive: true

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
- **Generated credentials are logged** for initial access
- Security warnings about password changes
- Skip notifications when tenants already exist

Example log output:
```
info: Starting bootstrap process...
info: No tenants found. Starting initial bootstrap...
info: Default tenant created: DefaultTenant (Code: default)
info: SuperAdmin user created: superadmin (superadmin@localhost)
info: Password: SuperAdmin#2025!
warn: SECURITY: Please change the SuperAdmin password immediately after first login!
info: Basic license assigned to tenant with 10 users and 1000 API calls per month
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

## Troubleshooting

### Bootstrap Not Running
- Check if tenants already exist in database
- Verify database connection
- Review application logs for errors

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