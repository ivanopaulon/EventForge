# EventForge Deployment Guide

## Environment Configuration

### Required Environment Variables for Production

Set these environment variables before deploying to production:

```bash
# JWT Configuration
JWT_SECRET_KEY="YourSecureRandomKeyAtLeast32CharactersLong!"

# Database Configuration
DB_CONNECTION_STRING="Server=your-server;Database=EventData;User Id=your-user;Password=your-password;"
LOG_DB_CONNECTION_STRING="Server=your-server;Database=EventLogger;User Id=your-user;Password=your-password;"

# Optional: Application Environment
ASPNETCORE_ENVIRONMENT="Production"
```

### Configuration Priority

The application uses the following configuration priority:
1. Environment variables (highest priority)
2. appsettings.{Environment}.json
3. appsettings.json (lowest priority)

### JWT Secret Key Security

⚠️ **CRITICAL**: Never use default or weak JWT secret keys in production.

- Generate a secure random key: `openssl rand -base64 32`
- Minimum length: 32 characters
- Store securely using your platform's secret management system

## Health Checks

The application provides multiple health check endpoints:

- `/health` - Full health status with detailed information
- `/health/ready` - Readiness probe (database connectivity)
- `/health/live` - Liveness probe (application running)

### Health Check Response Format

```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Entity Framework database health check",
      "duration": 23.456
    }
  ],
  "totalDuration": 23.456
}
```

## Resiliency Features

### HTTP Client Resiliency

The application includes Polly policies for external HTTP calls:

- **Retry Policy**: 3 attempts with exponential backoff
- **Circuit Breaker**: Opens after 5 consecutive failures, 30-second break duration

### Database Resiliency

- Connection retry logic through Entity Framework
- Health checks for database connectivity
- Graceful degradation when database is unavailable

## Monitoring and Logging

### Structured Logging

- **Primary**: SQL Server sink (configurable via `ConnectionStrings:LogDb`)
- **Fallback**: File logging (`Logs/fallback-log-.log`)
- **Format**: Structured logging with Serilog

### Performance Monitoring

Built-in query performance monitoring:
- Slow query threshold: 2 seconds (configurable)
- Query duration tracking
- Performance statistics available via health endpoints

## Security Best Practices

### Authentication

- JWT Bearer token authentication
- Configurable token expiration
- Role-based authorization policies

### Authorization Policies

- `RequireUser`: Authenticated users only
- `RequireAdmin`: Admin or SuperAdmin roles
- `RequireManager`: Manager, Admin, or SuperAdmin roles
- `RequireSuperAdmin`: SuperAdmin role only

### Multi-tenancy

- Tenant context isolation
- Session-based tenant switching
- Tenant validation in business controllers

## Testing

### Running Tests

```bash
# Unit tests
dotnet test EventForge.Tests

# Integration tests
dotnet test EventForge.IntegrationTests

# All tests
dotnet test
```

### Test Configuration

Tests use in-memory databases and mock external dependencies.

## Docker Deployment (Optional)

Create a `Dockerfile` for containerized deployment:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EventForge.Server/EventForge.Server.csproj", "EventForge.Server/"]
COPY ["EventForge.DTOs/EventForge.DTOs.csproj", "EventForge.DTOs/"]
RUN dotnet restore "EventForge.Server/EventForge.Server.csproj"
COPY . .
WORKDIR "/src/EventForge.Server"
RUN dotnet build "EventForge.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventForge.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EventForge.Server.dll"]
```

## Troubleshooting

### Common Issues

1. **JWT Configuration Error**: Ensure `JWT_SECRET_KEY` environment variable is set
2. **Database Connection**: Verify connection strings and database server accessibility
3. **Health Check Failures**: Check database connectivity and dependencies

### Debug Information

Access detailed health information at `/api/v1/health/detailed` for debugging deployment issues.