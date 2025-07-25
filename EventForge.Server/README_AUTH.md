# EventForge - Authentication & Authorization System

EventForge is now equipped with a comprehensive enterprise-level authentication and authorization system.

## 🔐 Authentication Features

### JWT-Based Authentication
- **Token Type**: JWT Bearer tokens
- **Claims**: User ID, username, email, roles, and permissions
- **Configurable Expiration**: Default 60 minutes (configurable)
- **Secure Secret Key**: Minimum 32 characters required

### Password Security
- **Hashing Algorithm**: Argon2id (industry standard)
- **Configurable Policy**: Length, complexity, special characters
- **Account Lockout**: Failed attempt tracking and temporary locks
- **Password Expiration**: Configurable password age limits

### User Management
- **Complete Entity Model**: Users, Roles, Permissions with many-to-many relationships
- **Soft Delete**: All entities support logical deletion with audit trail
- **Login Audit**: Complete tracking of login attempts, successes, and failures

## 🚀 Quick Start

### Default Admin Account
After first startup, the system automatically creates:
- **Username**: `admin`
- **Email**: `admin@eventforge.com`
- **Password**: `EventForge@2024!`
- **Role**: Admin (full permissions)

⚠️ **IMPORTANT**: Change the default admin password immediately after first login!

### API Authentication

1. **Login** to get JWT token:
```bash
POST /api/v1/auth/login
{
  "username": "admin",
  "password": "EventForge@2024!",
  "rememberMe": false
}
```

2. **Use token** in subsequent requests:
```bash
Authorization: Bearer <your-jwt-token>
```

3. **Access Swagger UI** at `http://localhost:5000` with JWT authentication support

## 🔒 Authorization System

### Role-Based Access Control (RBAC)
- **Admin**: Full system access
- **Manager**: Management-level operations
- **User**: Standard user operations
- **Viewer**: Read-only access

### Permission-Based Authorization
Fine-grained permissions for:
- **Users & Roles**: Create, Read, Update, Delete
- **Events & Teams**: Full CRUD operations
- **Reports & Audit**: View access logs and reports
- **System Settings**: Configuration management

### Protected Endpoints
All API endpoints are protected except:
- `/api/v1/health` - Public health check
- `/api/v1/auth/login` - Authentication endpoint

## 📊 Audit Logging

### Automatic Audit Tracking
- **All CRUD Operations**: Automatically tracked on auditable entities
- **Field-Level Changes**: What changed, from what to what, when, and by whom
- **Login Audit**: All authentication attempts with IP, user agent, and session info
- **Soft Delete Tracking**: Deletion events preserved with restoration capability

### Audit Information Captured
- Entity name and ID
- Property name and values (old → new)
- Operation type (Insert, Update, Delete)
- User who made the change
- Timestamp (UTC)
- Request correlation ID

## ⚙️ Configuration

### appsettings.json
```json
{
  "Authentication": {
    "Jwt": {
      "Issuer": "EventForge",
      "Audience": "EventForge",
      "SecretKey": "YourSecureSecretKeyHere",
      "ExpirationMinutes": 60
    },
    "PasswordPolicy": {
      "MinimumLength": 8,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigits": true,
      "RequireSpecialCharacters": true
    },
    "AccountLockout": {
      "MaxFailedAttempts": 5,
      "LockoutDurationMinutes": 30
    },
    "Bootstrap": {
      "AutoCreateAdmin": true,
      "DefaultAdminUsername": "admin",
      "DefaultAdminPassword": "EventForge@2024!"
    }
  }
}
```

## 🛡️ Security Features

### Password Policies
- **Minimum Length**: 8 characters (configurable)
- **Complexity Requirements**: Upper, lower, digits, special characters
- **Password History**: Prevents reuse of recent passwords
- **Expiration**: Configurable password age limits

### Account Protection
- **Failed Login Tracking**: Monitors unsuccessful attempts
- **Account Lockout**: Temporary locks after max failed attempts
- **Session Management**: JWT token-based stateless sessions
- **IP & User Agent Tracking**: Complete login audit trail

### API Security
- **Authorization Middleware**: All endpoints protected by default
- **Policy-Based Authorization**: Fine-grained permission checking
- **CORS Protection**: Configurable cross-origin policies
- **HTTPS Enforcement**: Secure communication in production

## 📈 Health Monitoring

The `/api/v1/health` endpoint provides comprehensive system status:
- API health status
- Database connectivity
- Authentication system status
- Configuration validation
- Dependency health checks

Enhanced health endpoint at `/api/v1/health/detailed` includes:
- Authentication configuration details
- Performance metrics
- System resource usage
- Detailed dependency status

## 🔧 API Endpoints

### Authentication
- `POST /api/v1/auth/login` - User login
- `POST /api/v1/auth/logout` - User logout
- `POST /api/v1/auth/change-password` - Change password
- `GET /api/v1/auth/me` - Get current user info
- `POST /api/v1/auth/validate-token` - Validate JWT token

### All Business Endpoints
All existing business endpoints (Events, Teams, Products, etc.) are now protected and require authentication.

## 🧪 Testing

You can test the authentication system using:
1. **Swagger UI**: Built-in JWT authentication support
2. **Postman**: Import the API collection and configure Bearer token
3. **curl**: Use JWT token in Authorization header

Example curl test:
```bash
# Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"EventForge@2024!"}'

# Use returned token for protected endpoints
curl -X GET http://localhost:5000/api/v1/events \
  -H "Authorization: Bearer <your-token>"
```

## 📝 Development Notes

### Database Migrations
The system includes EF Core migrations for authentication entities:
```bash
dotnet ef database update
```

### Bootstrap Process
On first startup, the system automatically:
1. Creates database if it doesn't exist
2. Applies pending migrations
3. Seeds default roles and permissions
4. Creates default admin user if enabled

### Audit System
The enhanced DbContext automatically tracks all changes to auditable entities. No additional code required in controllers - just save changes normally and audit logs are created automatically.

---

**Security Notice**: This system implements enterprise-grade security. Always use HTTPS in production, configure strong JWT secret keys, and regularly review audit logs for security monitoring.