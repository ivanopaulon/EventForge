# EventForge Settings Management Panel

## Overview
The Settings Management Panel is a comprehensive web-based interface for managing EventForge runtime configuration, database operations, and system monitoring. It provides SuperAdmin users with a centralized dashboard for system administration tasks.

## Access
- **URL**: `/settings`
- **Authorization**: SuperAdmin role required
- **Authentication**: JWT token (stored in localStorage or sessionStorage)

## Features

### 📊 Dashboard
Real-time system status monitoring:
- Environment detection (IIS, Kestrel, Docker)
- Server uptime tracking
- Database connection status
- System version information
- Quick action buttons

### ⚙️ Configuration
View and manage system configurations:
- Configurations grouped by category
- Hot-reload indicators (🔄 = immediate, ⚠️ = requires restart)
- Version tracking and audit trail
- Configuration source tracking (Database, overrides file, defaults)
- Values masked for encrypted settings

### 🗄️ Database
Database management interface:
- Real-time connection status monitoring
- Connection provider and database name display
- Connection performance metrics
- Placeholder for future features (migrations, bootstrap, backup/restore)

### 📜 Audit Log
Complete system operation history:
- Configuration changes
- Database operations
- Bootstrap events
- Pagination support (50 entries per page)
- Success/failure indicators
- User and timestamp tracking
- IP address and user agent logging

## Technical Details

### API Endpoints
All endpoints require SuperAdmin authorization and are located at `/api/v1/settings`:

#### Configuration
- `GET /configuration` - List all configurations
- `GET /configuration/{key}` - Get specific configuration

#### Database
- `GET /database/status` - Database connection status

#### Server
- `GET /server/restart/status` - Server status and uptime

#### Audit
- `GET /audit?page=1&pageSize=50` - System operation logs

### SignalR Hub
Real-time notifications via SignalR hub at `/hubs/configuration`:
- Configuration changes
- Restart requirements
- System operations

### Technology Stack
- **Frontend**: Vanilla JavaScript (no framework dependencies)
- **Styling**: Custom CSS with responsive design
- **Real-time**: SignalR for live updates
- **Authentication**: JWT Bearer token
- **API**: RESTful endpoints with JSON responses

## Usage

### Accessing the Panel
1. Login as a SuperAdmin user
2. Navigate to `/settings` in your browser
3. The panel will automatically load if you have a valid JWT token

### Viewing Configurations
1. Click the "Configuration" tab
2. Browse configurations grouped by category
3. Check hot-reload indicators for each setting
4. View version, modified date, and modified by information

### Monitoring Database
1. Click the "Database" tab
2. View real-time connection status
3. Check provider and database name
4. Monitor connection performance

### Reviewing Audit Logs
1. Click the "Audit Log" tab
2. Browse recent system operations
3. Use pagination to view older entries
4. Check success/failure status and details

## Database Schema

### SystemConfiguration
Extended configuration entity with:
- Version tracking (int)
- Active status (bool)
- Created/Modified by tracking
- Encrypted value support
- Restart requirement flag

### JwtKeyHistory
JWT signing key management:
- Key identifier
- Encrypted key storage (AES-256)
- Active status
- Valid from/until dates
- Audit trail

### SystemOperationLog
Complete audit trail:
- Operation type (ConfigChange, Migration, Restart, Bootstrap, etc.)
- Entity tracking
- Action performed
- Old/New values
- Success status
- Execution details (user, IP, user agent, timestamp)

## Security

### Authorization
- All endpoints require `RequireSuperAdmin` policy
- JWT token validation on every request
- Unauthorized access returns 401/403 status codes

### Data Protection
- Encrypted values are masked in API responses
- IP address and user agent tracking
- Comprehensive audit logging
- SQL injection protection via EF Core

### Best Practices
- Replace JWT placeholder key before production use
- Keep audit logs for compliance
- Review configuration changes regularly
- Monitor database connection health

## Configuration Priority
Settings are loaded in the following order (highest to lowest priority):
1. **Database** (SystemConfiguration table) - Highest priority
2. **appsettings.overrides.json** (git-ignored)
3. **appsettings.json** (defaults) - Lowest priority

## Future Enhancements (Planned)
The current implementation is an MVP with viewing capabilities. Future enhancements include:

### Configuration Management
- Inline configuration editing
- Hot-reload implementation for supported settings
- Version history with diff viewer
- Export/Import (JSON/Environment Variables)
- Batch updates
- Reset to default values

### Database Operations
- Apply migrations via UI
- Rollback migrations
- Bootstrap operations (seed data)
- Backup/Restore functionality
- Database statistics and optimization

### JWT Key Rotation
- Multi-key JWT validation
- Automated key rotation
- Key cleanup utilities
- Encryption key management

### Server Management
- Smart server restart
- Health monitoring with polling
- Restart script generation
- Countdown timer for restarts

## Development

### Building
```bash
dotnet build EventForge.Server/EventForge.Server.csproj
```

### Database Migration
Apply the settings management migration:
```sql
-- Run the migration script
Migrations/20260129_CreateSettingsManagementTables.sql

-- Rollback if needed
Migrations/ROLLBACK_20260129_CreateSettingsManagementTables.sql
```

### Console Logging
Console logging is disabled by default. To enable:
```json
{
  "Serilog": {
    "EnableConsole": true
  }
}
```

## Troubleshooting

### Cannot Access /settings
- Ensure you are logged in as a SuperAdmin user
- Check that JWT token is present in localStorage or sessionStorage
- Verify SuperAdmin policy is configured in authorization

### API Calls Fail
- Check browser console for error messages
- Verify JWT token is valid and not expired
- Ensure SuperAdmin role is assigned to your user
- Check server logs for authorization errors

### Database Connection Issues
- Verify connection string in appsettings.json
- Check database server is running and accessible
- Review connection status in Database tab
- Check SQL Server logs for connection errors

## Support
For issues or questions, contact the EventForge development team.

## Version
Settings Management Panel v1.0 - MVP Release
