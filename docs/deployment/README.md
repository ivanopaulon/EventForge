# Deployment & Infrastructure Documentation

Documentazione completa per deployment e configurazione infrastrutturale di EventForge.

## 📋 Indice

### 🚀 Deployment
- [Deployment Guide](./deployment-guide.md) - Guida completa al deployment
- [Environment Setup](./environment.md) - Configurazione ambienti
- [Docker Configuration](./docker.md) - Configurazione Docker
- [Cloud Deployment](./cloud-deployment.md) - Deployment cloud

### ⚙️ Configuration
- [Configuration Guide](./configuration.md) - Guida configurazione completa
- [Environment Variables](./environment-variables.md) - Variabili ambiente
- [Database Setup](./database-setup.md) - Configurazione database
- [Security Configuration](./security-config.md) - Configurazione sicurezza

### 🔐 Licensing & Authentication
- [Licensing System](./licensing.md) - Sistema licenze e gestione
- [Authentication Setup](./authentication.md) - Configurazione autenticazione
- [Multi-Tenant Configuration](./multi-tenant-config.md) - Configurazione multi-tenant
- [Permission Management](./permissions.md) - Gestione permessi

### 📊 Monitoring & Maintenance
- [Monitoring Setup](./monitoring.md) - Configurazione monitoraggio
- [Logging Configuration](./logging.md) - Configurazione logging
- [Backup & Recovery](./backup-recovery.md) - Backup e ripristino
- [Performance Tuning](./performance-tuning.md) - Ottimizzazione performance

## 🚀 Quick Start Deployment

### Prerequisites
- .NET 8 Runtime
- SQL Server (o compatibile)
- IIS o server web compatibile
- SSL Certificate (production)

### Basic Deployment Steps
1. **Preparation**
   ```bash
   # Build application
   dotnet publish -c Release -o ./publish
   ```

2. **Environment Configuration**
   ```bash
   # Set required environment variables
   export JWT_SECRET_KEY="YourSecureRandomKeyAtLeast32CharactersLong!"
   export DB_CONNECTION_STRING="Server=...;Database=EventData;..."
   export LOG_DB_CONNECTION_STRING="Server=...;Database=EventLogger;..."
   ```

3. **Database Setup**
   ```bash
   # Run migrations
   dotnet ef database update --project EventForge.Server
   ```

4. **Deploy & Start**
   - Copy files to web server
   - Configure IIS/web server
   - Start application

## ⚙️ Configuration Overview

### Configuration Priority
1. **Environment Variables** (highest priority)
2. **appsettings.{Environment}.json**
3. **appsettings.json** (lowest priority)

### Required Environment Variables
```bash
# JWT Configuration
JWT_SECRET_KEY="YourSecureRandomKeyAtLeast32CharactersLong!"

# Database Configuration
DB_CONNECTION_STRING="Server=your-server;Database=EventData;..."
LOG_DB_CONNECTION_STRING="Server=your-server;Database=EventLogger;..."

# Application Environment
ASPNETCORE_ENVIRONMENT="Production"
```

### Optional Configuration
```bash
# Logging Level
LOGGING_LEVEL="Information"

# Performance Settings
MAX_REQUEST_SIZE="100MB"
REQUEST_TIMEOUT="300"

# Multi-Tenant Settings
DEFAULT_TENANT_ID="default-tenant-guid"
TENANT_ISOLATION_LEVEL="Database"
```

## 🔐 Security Configuration

### JWT Security
⚠️ **CRITICAL**: Never use default or weak JWT secret keys in production.

Recommended JWT secret key characteristics:
- Minimum 32 characters
- Mix of letters, numbers, and symbols
- Cryptographically random
- Unique per environment

### Database Security
- Use dedicated database users with minimal permissions
- Enable SSL/TLS connections
- Regular security updates
- Backup encryption

### Network Security
- HTTPS only in production
- Firewall configuration
- Load balancer SSL termination
- CDN configuration if applicable

## 🔧 Docker Deployment

### Dockerfile Example
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EventForge.Server/EventForge.Server.csproj", "EventForge.Server/"]
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

### Docker Compose
```yaml
version: '3.8'
services:
  eventforge:
    build: .
    ports:
      - "80:80"
      - "443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - DB_CONNECTION_STRING=${DB_CONNECTION_STRING}
    depends_on:
      - sqlserver
  
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=${SA_PASSWORD}
      - ACCEPT_EULA=Y
    volumes:
      - sqldata:/var/opt/mssql
```

## 📊 Monitoring & Health Checks

### Health Check Endpoints
- `/health` - Basic health check
- `/health/ready` - Readiness check
- `/health/live` - Liveness check

### Logging Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Performance Monitoring
- Application Insights integration
- Custom metrics collection
- Error tracking
- Performance counters

## 🔗 Collegamenti Utili

- [Backend Documentation](../backend/) - Configurazione backend
- [Testing Documentation](../testing/) - Testing deployment
- [Core Documentation](../core/) - Setup progetto
- [Migration Guides](../migration/) - Guide migrazione