# EventForge Deployment Guide - IIS Configuration

## Overview

This guide provides comprehensive instructions for deploying EventForge to Internet Information Services (IIS) with automatic startup and proper configuration. When publishing `EventForge.Server`, the Blazor WebAssembly client (`EventForge.Client`) is automatically published and its output is copied into the server's `wwwroot` directory. This allows the server to host the client as static files, enabling a unified deployment to IIS or other web servers.

## How It Works

The `EventForge.Server.csproj` file contains an MSBuild target (`PublishBlazorClient`) that runs before the server publish process. This target:

1. Publishes the `EventForge.Client` project using the same configuration (Debug/Release) as the server
2. Copies the published client files (index.html, _framework, etc.) into the server's publish output wwwroot directory
3. Ensures the client and server are deployed together as a single package

## Publishing the Application

To publish the application with the client included:

```bash
dotnet publish EventForge.Server -c Release -o ./publish-output
```

After publishing, the `./publish-output/wwwroot` directory will contain:
- `index.html` - The Blazor WASM entry point
- `_framework/` - Blazor framework files and assemblies
- Other client static assets

## Prerequisites for IIS Deployment

### Required Components

Before deploying to IIS, ensure the following components are installed on the server:

#### 1. ASP.NET Core Hosting Bundle

The ASP.NET Core Hosting Bundle is **REQUIRED** for running ASP.NET Core applications on IIS.

**Installation Steps:**

1. Download the latest .NET 9.0 Hosting Bundle from: https://dotnet.microsoft.com/download/dotnet/9.0
2. Run the installer (dotnet-hosting-9.0.x-win.exe)
3. **Important**: After installation, restart IIS or reboot the server
   ```powershell
   # Restart IIS
   net stop was /y
   net start w3svc
   ```
4. Verify installation:
   ```powershell
   # Check if ASP.NET Core Module is installed
   Get-WindowsFeature -Name Web-ASPNET*
   
   # Verify dotnet runtime
   dotnet --list-runtimes
   ```

**What the Hosting Bundle Provides:**
- ASP.NET Core Runtime
- .NET Runtime
- ASP.NET Core Module (ANCM) for IIS
- Enables IIS to host ASP.NET Core applications

**Troubleshooting:**
- If applications fail to start after installing the bundle, ensure you restarted IIS
- Check Windows Event Viewer > Application logs for ASP.NET Core Module errors
- Verify the module is registered: `%windir%\system32\inetsrv\config\applicationHost.config` should contain `AspNetCoreModuleV2`

## IIS Deployment Step-by-Step

### Step 1: Publish the Application

Publish the EventForge.Server project in Release configuration:

```bash
dotnet publish EventForge.Server -c Release -o ./publish-output
```

This command will:
- Build the server project in Release mode
- Automatically publish the Blazor WebAssembly client
- Copy all files to the `./publish-output` directory
- Include the configured `web.config` file

### Step 2: Prepare the IIS Server Directory

1. Create a directory on the IIS server (e.g., `C:\inetpub\EventForge`)
2. Copy all contents from `./publish-output` to the server directory
3. Ensure the IIS Application Pool identity has read/write permissions:
   ```powershell
   # Grant permissions to IIS_IUSRS group
   icacls "C:\inetpub\EventForge" /grant "IIS_IUSRS:(OI)(CI)M" /T
   ```
4. Create a `logs` subdirectory for stdout logging:
   ```powershell
   mkdir "C:\inetpub\EventForge\logs"
   icacls "C:\inetpub\EventForge\logs" /grant "IIS_IUSRS:(OI)(CI)M" /T
   ```

### Step 3: Create and Configure Application Pool

**IMPORTANT**: Proper Application Pool configuration is critical for automatic startup and optimal performance.

1. **Open IIS Manager** (inetmgr.exe)

2. **Create a new Application Pool:**
   - Right-click on "Application Pools" → "Add Application Pool"
   - Name: `EventForgeAppPool`
   - .NET CLR version: **No Managed Code** (critical - ASP.NET Core runs out-of-process)
   - Managed pipeline mode: Integrated
   - Click OK

3. **Configure Application Pool Advanced Settings:**
   
   Right-click on `EventForgeAppPool` → "Advanced Settings" and configure:

   **General Section:**
   - **.NET CLR Version**: No Managed Code ✓ (REQUIRED)
   - **Managed Pipeline Mode**: Integrated
   - **Queue Length**: 1000 (default)
   - **Start Mode**: **AlwaysRunning** ✓ (for automatic startup)
   
   **Process Model Section:**
   - **Identity**: ApplicationPoolIdentity (or a custom account with appropriate database permissions)
   - **Idle Time-out (minutes)**: 0 ✓ (disable idle timeout to prevent shutdown)
   - **Load User Profile**: True (if using user certificates or profile-dependent features)
   - **Maximum Worker Processes**: 1 (or more for web garden configuration)
   - **Ping Enabled**: True
   - **Ping Maximum Response Time**: 90 seconds
   
   **Recycling Section:**
   - **Regular Time Interval (minutes)**: 1740 (29 hours - default, adjust as needed)
   - **Specific Times**: Configure if you prefer scheduled recycling
   - Disable unnecessary recycling conditions to maintain stability
   
   **Rapid-Fail Protection Section:**
   - **Enabled**: True
   - **Maximum Failures**: 5
   - **Failure Interval (minutes)**: 5
   
   **CPU Section (Optional):**
   - **Limit**: 0 (no limit) or set based on your requirements
   - **Limit Action**: NoAction or Throttle

4. **Important Notes:**
   - **No Managed Code** is essential - ASP.NET Core applications don't use the .NET Framework CLR
   - **AlwaysRunning** ensures the application starts automatically when IIS starts
   - **Idle Time-out = 0** prevents the application from shutting down during inactivity

### Step 4: Create IIS Website or Application

#### Option A: Create a New Website

1. Right-click "Sites" → "Add Website"
2. Configure:
   - **Site name**: EventForge
   - **Application pool**: EventForgeAppPool (select the one created above)
   - **Physical path**: `C:\inetpub\EventForge`
   - **Binding**:
     - Type: http or https
     - IP address: All Unassigned (or specific IP)
     - Port: 80 (http) or 443 (https) or custom port
     - Host name: (leave empty for IP-based access or enter domain name)
3. Click OK

#### Option B: Create as Application under Default Web Site

1. Right-click "Default Web Site" → "Add Application"
2. Configure:
   - **Alias**: EventForge
   - **Application pool**: EventForgeAppPool
   - **Physical path**: `C:\inetpub\EventForge`
3. Click OK
4. **Important**: If deploying as a virtual directory, update `<base href="/EventForge/" />` in the published `wwwroot/index.html`

### Step 5: Configure Port Binding (IIS Manages Ports - Not Code)

**CRITICAL PRINCIPLE**: Never configure ports in application code for IIS deployments. IIS manages all port binding through site bindings.

**How Port Configuration Works in IIS:**

1. IIS binds to the configured port(s) in the site bindings
2. IIS's HTTP.SYS listener receives requests on those ports
3. IIS forwards requests to the ASP.NET Core application through the ASP.NET Core Module
4. The application receives requests without needing to know the port
5. **DO NOT** set `ASPNETCORE_URLS` environment variable in IIS deployments

**Configuring Site Bindings:**

1. In IIS Manager, select your site (e.g., EventForge)
2. Click "Bindings" in the Actions pane
3. Add/edit bindings:
   
   **HTTP Binding:**
   - Type: http
   - IP address: All Unassigned (or specific IP)
   - Port: 80 (or custom port)
   
   **HTTPS Binding (Recommended for Production):**
   - Type: https
   - IP address: All Unassigned
   - Port: 443
   - SSL Certificate: Select installed certificate (see SSL Configuration below)

4. Click OK to save

**Multiple Port Binding:**
You can add multiple bindings for different scenarios:
- Port 80 for HTTP
- Port 443 for HTTPS
- Custom port like 8080 for development/testing
- Different host names on same port

**Example Binding Configurations:**

- **Development/Internal**: http on port 8080
- **Production**: https on port 443 with valid SSL certificate
- **Load Balancer**: http on port 80 (if SSL termination at load balancer)

### Step 6: Enable Automatic Startup (Preload)

To ensure the application starts automatically when IIS starts (without waiting for the first request):

**Method 1: Using IIS Manager**

1. Select your site → "Manage Website" → "Advanced Settings"
2. Under "General" section:
   - **Preload Enabled**: True ✓

**Method 2: Using PowerShell**

```powershell
# Enable preload for the site
Set-ItemProperty "IIS:\Sites\EventForge" -Name applicationDefaults.preloadEnabled -Value $true

# Verify
Get-ItemProperty "IIS:\Sites\EventForge" -Name applicationDefaults.preloadEnabled
```

**Method 3: Using appcmd.exe**

```cmd
%windir%\system32\inetsrv\appcmd.exe set app "EventForge/" /preloadEnabled:true
```

**What Preload Does:**
- Application pool starts immediately when IIS starts (or after recycling)
- Application is initialized before the first request
- Users don't experience the "cold start" delay
- Combined with `AlwaysRunning` start mode for full automatic startup

**Verify Automatic Startup:**
1. Restart IIS: `iisreset`
2. Check Windows Task Manager - w3wp.exe process should be running for EventForgeAppPool
3. Check Application logs at `C:\inetpub\EventForge\logs\stdout_*.log`
4. Navigate to your site URL - should respond immediately

### Step 7: Configure Environment Variables

Environment variables control application behavior without code changes.

**Where to Configure:**

#### Option 1: web.config (Application-Specific)

Edit `C:\inetpub\EventForge\web.config`:

```xml
<aspNetCore processPath="dotnet" 
            arguments=".\EventForge.Server.dll" 
            stdoutLogEnabled="true" 
            stdoutLogFile=".\logs\stdout" 
            hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="ConnectionStrings__SqlServer" value="Server=YOUR_SERVER;Database=EventData;..." />
    <environmentVariable name="ConnectionStrings__LogDb" value="Server=YOUR_SERVER;Database=EventLogger;..." />
    <environmentVariable name="Authentication__Jwt__SecretKey" value="YOUR_SECURE_SECRET_KEY" />
  </environmentVariables>
</aspNetCore>
```

#### Option 2: IIS Application Settings (Recommended for Secrets)

1. In IIS Manager, select your site
2. Double-click "Configuration Editor"
3. Section: `system.webServer/aspNetCore`
4. Click on `environmentVariables` (Collection)
5. Add each variable:
   - Name: `ASPNETCORE_ENVIRONMENT`
   - Value: `Production`

#### Option 3: Machine or User Environment Variables (System-Wide)

```powershell
# Set machine-level environment variable
[System.Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")

# Restart IIS after setting
iisreset
```

**Common Environment Variables:**

| Variable | Description | Development | Production |
|----------|-------------|-------------|------------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | Development | Production |
| `ASPNETCORE_URLS` | **DO NOT SET in IIS** | - | - |
| `ConnectionStrings__SqlServer` | Main database connection | LocalDB/SQL Express | Production SQL Server |
| `ConnectionStrings__LogDb` | Logging database connection | LocalDB/SQL Express | Production SQL Server |
| `Authentication__Jwt__SecretKey` | JWT signing key | Dev key | Secure production key |
| `ASPNETCORE_DETAILEDERRORS` | Show detailed errors | true | false |

**Security Best Practices:**
- Never commit secrets to source control
- Use IIS Application Settings for sensitive values
- Consider Azure Key Vault or similar secret management
- Rotate JWT keys regularly
- Use different database credentials per environment

### Step 8: Configure web.config Settings

The `web.config` file is automatically created during publish and configures the ASP.NET Core Module.

**Key Configuration Elements:**

```xml
<aspNetCore processPath="dotnet" 
            arguments=".\EventForge.Server.dll" 
            stdoutLogEnabled="true" 
            stdoutLogFile=".\logs\stdout" 
            hostingModel="inprocess"
            forwardWindowsAuthToken="false">
```

**Configuration Attributes Explained:**

- **`processPath="dotnet"`**: Path to dotnet.exe (or can be the app executable for self-contained deployments)
- **`arguments=".\EventForge.Server.dll"`**: The application DLL to execute
- **`stdoutLogEnabled="true"`**: Enable stdout/stderr logging for debugging
  - Set to `false` in production after confirming stability (performance optimization)
  - Logs are written to the `stdoutLogFile` location
- **`stdoutLogFile=".\logs\stdout"`**: Where to write stdout logs
  - Creates files like `stdout_20250112_123456_12345.log`
  - Useful for diagnosing startup issues
- **`hostingModel="inprocess"`**: **IMPORTANT** - Run inside IIS worker process (w3wp.exe)
  - **Better performance** than out-of-process hosting
  - Lower memory footprint
  - Faster request processing
  - Recommended for production
- **`forwardWindowsAuthToken="false"`**: Set to true only if using Windows Authentication

**Best Practices:**
- Use `hostingModel="inprocess"` for production (default in our config)
- Enable `stdoutLogEnabled` during initial deployment and troubleshooting
- Disable `stdoutLogEnabled` in stable production for better performance
- Monitor log file size and implement log rotation if needed

### Step 9: SSL/TLS Configuration (Production)

For production deployments, always use HTTPS.

**Obtaining an SSL Certificate:**

1. **Purchase from Certificate Authority**: Digicert, Sectigo, Let's Encrypt, etc.
2. **Free Certificate**: Let's Encrypt (can use win-acme for automatic renewal)
3. **Internal PKI**: Use your organization's certificate authority
4. **Self-Signed (Development Only)**: Create using PowerShell

**Installing SSL Certificate in IIS:**

1. **Import Certificate:**
   - IIS Manager → Server node → "Server Certificates"
   - "Import" or "Complete Certificate Request"
   - Select .pfx file and enter password

2. **Bind Certificate to Site:**
   - Select your site → "Bindings"
   - Add binding: Type=https, Port=443
   - Select SSL Certificate from dropdown
   - Click OK

3. **Configure HTTPS Redirect (Optional):**
   
   Add to web.config:
   ```xml
   <rewrite>
     <rules>
       <rule name="HTTPS Redirect" stopProcessing="true">
         <match url="(.*)" />
         <conditions>
           <add input="{HTTPS}" pattern="off" />
         </conditions>
         <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
       </rule>
     </rules>
   </rewrite>
   ```

### Step 10: Verify Deployment

After completing the configuration, verify the deployment:

1. **Check Application Pool Status:**
   ```powershell
   Get-IISAppPool -Name "EventForgeAppPool"
   # Should show State: Started
   ```

2. **Check Worker Process:**
   ```powershell
   # Check if w3wp.exe is running for your app pool
   Get-Process w3wp | Select-Object Id, ProcessName, @{Name="AppPool";Expression={$_.Modules[0].ModuleName}}
   ```

3. **Check stdout Logs:**
   ```powershell
   # View recent stdout log
   Get-Content "C:\inetpub\EventForge\logs\stdout_*.log" -Tail 50
   ```

4. **Test HTTP Endpoints:**
   ```powershell
   # Test health endpoint
   Invoke-WebRequest -Uri "http://localhost/health" -UseBasicParsing
   
   # Test API documentation
   Invoke-WebRequest -Uri "http://localhost/swagger" -UseBasicParsing
   ```

5. **Check Windows Event Logs:**
   ```powershell
   # Check for ASP.NET Core Module errors
   Get-EventLog -LogName Application -Source "IIS AspNetCore Module V2" -Newest 20
   ```

6. **Access the Application:**
   - Open browser and navigate to your site URL
   - Check browser console for errors (F12)
   - Verify API calls are working (check Network tab)

### Basic Deployment Summary

1. Copy the contents of the publish output directory to your IIS site directory
2. Ensure the IIS application pool is configured to use "No Managed Code"
3. Install the [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet) on the server
4. The included `web.config` handles:
   - ASP.NET Core Module configuration with in-process hosting
   - Correct MIME types for `.wasm`, `.json`, `.br`, and `.gz` files
   - SPA fallback routing (all non-API, non-file requests route to index.html)
   - API route exclusion (requests to `/api/*`, `/swagger/*`, `/health/*` are handled by the server)

### Virtual Directory Deployment

If deploying to a virtual directory (e.g., `https://example.com/myapp/`), you need to adjust the base href:

1. Edit `wwwroot/index.html` in the published output
2. Change `<base href="/" />` to `<base href="/myapp/" />` (use your virtual directory path)

### Precompressed Files (.br and .gz)

The Blazor publish process generates `.br` (Brotli) and `.gz` (Gzip) compressed versions of static assets to improve performance. However, these require additional IIS configuration to serve properly:

**Option 1: Remove precompressed files** (simpler, but less optimal)
```bash
# After publishing, remove compressed files
Remove-Item ./publish-output/wwwroot/_framework/*.br
Remove-Item ./publish-output/wwwroot/_framework/*.gz
```

**Option 2: Configure IIS to serve precompressed files** (recommended for production)

Add to your `web.config` inside `<system.webServer>`:

```xml
<rewrite>
  <outboundRules>
    <rule name="Add Vary Accept-Encoding" preCondition="PreCompressedFile">
      <match serverVariable="RESPONSE_Vary" pattern=".*" />
      <action type="Rewrite" value="Accept-Encoding" />
    </rule>
    <preConditions>
      <preCondition name="PreCompressedFile">
        <add input="{REQUEST_FILENAME}" pattern="\.(br|gz)$" />
      </preCondition>
    </preConditions>
  </outboundRules>
  <rules>
    <rule name="Serve precompressed Brotli" stopProcessing="true">
      <match url="(.*)"/>
      <conditions>
        <add input="{HTTP_ACCEPT_ENCODING}" pattern="br" />
        <add input="{REQUEST_FILENAME}.br" matchType="IsFile" />
      </conditions>
      <action type="Rewrite" url="{R:1}.br" />
      <serverVariables>
        <set name="RESPONSE_Content_Encoding" value="br" />
      </serverVariables>
    </rule>
    <rule name="Serve precompressed Gzip" stopProcessing="true">
      <match url="(.*)"/>
      <conditions>
        <add input="{HTTP_ACCEPT_ENCODING}" pattern="gzip" />
        <add input="{REQUEST_FILENAME}.gz" matchType="IsFile" />
      </conditions>
      <action type="Rewrite" url="{R:1}.gz" />
      <serverVariables>
        <set name="RESPONSE_Content_Encoding" value="gzip" />
      </serverVariables>
    </rule>
  </rules>
</rewrite>
```

## Development vs Production Configuration Differences

Understanding the differences between development and production environments is crucial for proper deployment.

### Port Configuration

| Aspect | Development | Production (IIS) |
|--------|-------------|------------------|
| **Port Configuration** | `launchSettings.json` or `ASPNETCORE_URLS` environment variable | IIS Site Bindings (configured in IIS Manager) |
| **Configuration Location** | `EventForge.Server/Properties/launchSettings.json` | IIS Manager → Site → Bindings |
| **Typical Ports** | 7241 (HTTPS), 7240 (HTTP) | 443 (HTTPS), 80 (HTTP) |
| **Multiple Ports** | Semicolon-separated: `https://localhost:7241;http://localhost:7240` | Multiple bindings in IIS (add each separately) |
| **Environment Variable** | `ASPNETCORE_URLS` can be set | **DO NOT SET** `ASPNETCORE_URLS` in IIS |
| **Who Manages** | Kestrel web server | IIS (HTTP.SYS) → ANCM → Application |

### Environment Variables

| Variable | Development | Production |
|----------|-------------|------------|
| `ASPNETCORE_ENVIRONMENT` | Development | Production |
| `ASPNETCORE_URLS` | Set in launchSettings.json | **Not used - IIS manages ports** |
| `ASPNETCORE_DETAILEDERRORS` | true (optional) | false (security) |
| Connection Strings | Local SQL Server/SQLite | Production SQL Server |
| JWT Secret Key | Development key | Secure, randomly generated key |
| Logging Level | Debug/Information | Warning/Error |

### Application Startup

| Aspect | Development | Production (IIS) |
|--------|-------------|------------------|
| **Startup Trigger** | `dotnet run` command | IIS worker process starts automatically |
| **Auto-start** | Manual start | Automatic with `AlwaysRunning` + `PreloadEnabled` |
| **Idle Shutdown** | App exits when stopped | Prevented with `IdleTimeout=0` |
| **Host** | Kestrel standalone | IIS worker process (in-process hosting) |
| **Process Name** | dotnet.exe | w3wp.exe |

### Client Configuration

| Aspect | Development | Production |
|--------|-------------|------------|
| **API Base URL** | `appsettings.Development.json` - `https://localhost:7241/` | `appsettings.Production.json` - Production domain/IP |
| **Client Hosting** | Separate dev server (Blazor dev server) | Static files served by ASP.NET Core |
| **CORS** | Required (different origins) | Not required (same origin) |

### Security Considerations

| Aspect | Development | Production |
|--------|-------------|------------|
| **HTTPS** | Optional (dev certificate) | **REQUIRED** (valid CA certificate) |
| **Error Details** | Detailed error pages | Generic error pages |
| **Stdout Logging** | Enabled | Disabled after initial deployment verification |
| **Swagger/OpenAPI** | Enabled at root | Disabled or protected endpoint |
| **Secret Management** | User Secrets, appsettings | Azure Key Vault, IIS Config, Environment Variables |

### File System

| Aspect | Development | Production |
|--------|-------------|------------|
| **Base Path** | Project directory | IIS site physical path (e.g., `C:\inetpub\EventForge`) |
| **Log Location** | Project `Logs` folder | IIS site `logs` folder |
| **Permissions** | Current user | IIS_IUSRS group |

### Example Scenarios

#### Development Startup:
```bash
cd EventForge.Server
dotnet run --urls "https://localhost:7241;http://localhost:7240"
```
- Kestrel listens on specified ports
- Application starts immediately
- Stops when Ctrl+C is pressed

#### Production Startup (IIS):
1. IIS starts (Windows boot or `iisreset`)
2. Application Pool `EventForgeAppPool` starts (Start Mode: AlwaysRunning)
3. Site is preloaded (Preload Enabled: true)
4. Application initializes automatically
5. IIS bindings (e.g., port 443) receive requests
6. Application processes requests without knowing the port

## Troubleshooting

### Application Not Starting

**Symptoms:** 502.5 error, site doesn't respond

**Solutions:**
1. **Check ASP.NET Core Hosting Bundle:**
   ```powershell
   dotnet --list-runtimes
   # Should show Microsoft.AspNetCore.App 9.0.x
   ```
   - If missing, install from https://dotnet.microsoft.com/download/dotnet/9.0
   - Restart IIS after installation

2. **Check Application Pool:**
   - Verify "No Managed Code" is set
   - Check pool is Started
   - Check Identity has permissions to application directory

3. **Check stdout Logs:**
   ```powershell
   Get-Content "C:\inetpub\EventForge\logs\stdout_*.log" -Tail 100
   ```
   - Look for startup errors
   - Check for missing dependencies
   - Verify database connection strings

4. **Check Event Viewer:**
   - Windows Logs → Application
   - Filter by Source: "IIS AspNetCore Module V2"
   - Look for error details

### Client Not Loading (Blank Page)

**Symptoms:** White/blank page, no Blazor UI

**Solutions:**
1. **Verify index.html exists:**
   ```powershell
   Test-Path "C:\inetpub\EventForge\wwwroot\index.html"
   ```
   
2. **Check browser console** (F12 → Console):
   - Look for 404 errors on _framework files
   - Verify base href is correct
   
3. **Check web.config SPA fallback:**
   - Ensure rewrite rules are present
   - Verify URL Rewrite module is installed in IIS

4. **Check MIME types:**
   ```powershell
   # Verify .wasm MIME type
   Get-WebConfigurationProperty -PSPath "IIS:\Sites\EventForge" -Filter "//staticContent/mimeMap[@fileExtension='.wasm']" -Name "mimeType"
   ```

### WASM File Not Loading with Correct MIME Type

**Symptoms:** 404 errors on .wasm files, MIME type errors in console

**Solutions:**
1. **Install URL Rewrite Module:**
   - Download: https://www.iis.net/downloads/microsoft/url-rewrite
   - Install and restart IIS

2. **Verify web.config MIME mappings:**
   - Check `<staticContent>` section has `.wasm` → `application/wasm`

3. **Clear IIS cache:**
   ```powershell
   Remove-Item "C:\inetpub\temp\apppools\*" -Recurse -Force
   iisreset
   ```

### API Calls Failing

**Symptoms:** 404 on /api/* endpoints, ERR_CONNECTION_REFUSED

**Solutions:**
1. **Verify application is running:**
   ```powershell
   Get-Process w3wp | Where-Object {$_.Modules.ModuleName -like "*EventForge*"}
   ```

2. **Check API routes in swagger:**
   - Navigate to `https://yoursite/swagger`
   - Verify endpoints are listed

3. **Verify SPA fallback excludes API:**
   - web.config should have `<add input="{REQUEST_URI}" pattern="^/api" negate="true" />`

4. **Check CORS (if accessing from different origin):**
   - For same-origin deployment, CORS is not needed
   - For cross-origin, verify CORS policy in Program.cs

### 500.30 Error - In-Process Startup Failure

**Symptoms:** 500.30 error page

**Solutions:**
1. **Enable detailed errors temporarily:**
   - Add to web.config: `<environmentVariable name="ASPNETCORE_DETAILEDERRORS" value="true" />`
   - Restart application pool
   - Refresh page to see detailed error

2. **Check dependencies:**
   - Verify all required DLLs are in application directory
   - Check for version mismatches

3. **Check database connectivity:**
   - Test connection string from server
   - Verify database exists
   - Check database user permissions

### Application Shuts Down Unexpectedly

**Symptoms:** Application stops responding, pool recycles frequently

**Solutions:**
1. **Check Idle Timeout:**
   - Set to 0 to prevent idle shutdown
   ```powershell
   Set-ItemProperty "IIS:\AppPools\EventForgeAppPool" -Name processModel.idleTimeout -Value "00:00:00"
   ```

2. **Check Rapid-Fail Protection:**
   - May be shutting down due to multiple failures
   - Check Event Viewer for crash details

3. **Check Application Pool Recycling Settings:**
   - Disable unwanted recycling triggers
   - Monitor memory usage if recycling due to memory limits

### QZ Tray Integration Issues

**Symptoms:** `qz.api.setCertificatePromise is not a function` or QZ Tray errors in browser console

**Root Cause:** This occurs when the QZ Tray JavaScript library version doesn't match the API usage in the application code.

**Solutions:**

1. **Verify QZ Tray is installed and running:**
   - Download latest version from: https://qz.io/download/
   - Install QZ Tray application on client machines
   - Ensure QZ Tray is running (system tray icon should be visible)

2. **Check QZ Tray library version in the client:**
   ```powershell
   # Check wwwroot for QZ Tray files
   Get-ChildItem "C:\inetpub\EventForge\wwwroot" -Recurse -Filter "*qz-tray*"
   ```

3. **Update QZ Tray JavaScript library:**
   - The application should use the npm package `qz-tray` version 2.2.x or later
   - Check `EventForge.Client/package.json` for qz-tray version
   - Update to compatible version:
     ```bash
     cd EventForge.Client
     npm update qz-tray
     npm run build
     ```
   - Republish the application

4. **Verify certificate setup in qz-setup.js:**
   - Ensure the certificate promise setup matches the QZ Tray API version
   - For QZ Tray 2.2.x, use:
     ```javascript
     qz.security.setCertificatePromise(function(resolve, reject) {
         // Certificate setup
     });
     ```

5. **Check browser console for QZ Tray connection:**
   - QZ Tray must be running on the client machine
   - WebSocket connection to localhost:8182 or localhost:8181 must succeed
   - Check firewall settings allow local QZ Tray connection

6. **Non-blocking error handling:**
   - If QZ Tray is optional functionality, ensure errors are caught and don't block application startup
   - The error message indicates this is "non-blocking" which is good practice

**Best Practice:** QZ Tray integration is primarily a client-side concern. For IIS deployment:
- Ensure the client JavaScript files are published correctly
- Document QZ Tray installation requirements for end users
- Provide fallback functionality if QZ Tray is unavailable

### Port Already in Use

**Symptoms:** Cannot start IIS, port binding error

**Solutions:**
1. **Identify process using the port:**
   ```powershell
   netstat -ano | findstr :80
   # Or for 443
   netstat -ano | findstr :443
   ```

2. **Stop conflicting service:**
   - If another application is using the port, stop it or change IIS binding

3. **Change IIS binding:**
   - Use a different port in site bindings
   - Update application configuration if needed (though IIS handles this transparently)

### Permissions Errors

**Symptoms:** Access denied, cannot write to database, cannot create log files

**Solutions:**
1. **Grant IIS_IUSRS permissions:**
   ```powershell
   icacls "C:\inetpub\EventForge" /grant "IIS_IUSRS:(OI)(CI)M" /T
   icacls "C:\inetpub\EventForge\logs" /grant "IIS_IUSRS:(OI)(CI)F" /T
   ```

2. **Grant database permissions:**
   - Ensure Application Pool Identity has access to SQL Server
   - Use SQL Server authentication in connection string, or
   - Grant permissions to `IIS APPPOOL\EventForgeAppPool`

3. **Check file system permissions:**
   ```powershell
   Get-Acl "C:\inetpub\EventForge" | Format-List
   ```

## Development vs Production

In **development**, the client and server run separately (typically on different ports). The client proxies API requests to the server, and ports are configured in code/configuration files.

In **production** (after publishing to IIS), both client and server are served from the same origin, eliminating CORS concerns. IIS manages all port binding through site bindings, and the application doesn't need to know which ports are being used. This simplifies deployment and security.

## Best Practices for IIS Deployment

### Security
✅ Always use HTTPS in production with valid SSL certificates  
✅ Never commit secrets to source control  
✅ Use environment variables or Azure Key Vault for sensitive configuration  
✅ Disable detailed error pages in production (`ASPNETCORE_DETAILEDERRORS=false`)  
✅ Disable stdout logging in stable production environments  
✅ Regularly update the ASP.NET Core Hosting Bundle  
✅ Keep Windows Server and IIS updated with security patches  
✅ Use strong JWT secret keys and rotate them periodically  
✅ Implement proper database access controls  

### Performance
✅ Use `hostingModel="inprocess"` for better performance  
✅ Set Application Pool Start Mode to `AlwaysRunning`  
✅ Enable Preload on the site  
✅ Set Idle Timeout to 0 to prevent application shutdown  
✅ Configure appropriate recycling schedules  
✅ Monitor application pool memory and CPU usage  
✅ Consider using IIS compression for static files  
✅ Implement caching strategies (response caching, distributed cache)  

### Configuration Management
✅ Never hardcode ports in application code  
✅ Use IIS bindings for all port configuration  
✅ Separate configuration by environment (Development, Staging, Production)  
✅ Document any custom environment variables  
✅ Use configuration transformation for different environments  
✅ Keep appsettings.json for defaults, override with environment variables  

### Monitoring and Maintenance
✅ Set up health check monitoring (`/health` endpoint)  
✅ Configure Application Insights or similar APM tool  
✅ Monitor stdout logs during initial deployment  
✅ Check Windows Event Viewer regularly  
✅ Set up alerts for application pool failures  
✅ Implement log rotation for stdout logs  
✅ Monitor disk space on log directories  
✅ Test deployments in staging before production  

### Deployment Process
✅ Test the application thoroughly before publishing  
✅ Create backups before updating production  
✅ Use staged deployments (deploy to staging, verify, then production)  
✅ Document the deployment process  
✅ Use deployment automation tools (CI/CD pipelines)  
✅ Verify web.config is correct for your environment  
✅ Test all functionality after deployment  

## Quick Reference Checklists

### Pre-Deployment Checklist

- [ ] ASP.NET Core Hosting Bundle 9.0 installed
- [ ] IIS and required features enabled
- [ ] URL Rewrite module installed (for SPA routing)
- [ ] SSL certificate obtained and ready (for production)
- [ ] Database server accessible from IIS server
- [ ] Database created with appropriate schema
- [ ] Database user credentials configured
- [ ] Firewall rules configured (if needed)
- [ ] Backup strategy in place

### Deployment Checklist

- [ ] Application published in Release mode
- [ ] All files copied to IIS directory
- [ ] Application Pool created with correct settings:
  - [ ] .NET CLR Version: No Managed Code
  - [ ] Start Mode: AlwaysRunning
  - [ ] Idle Timeout: 0
- [ ] IIS Site or Application created
- [ ] Physical path pointing to application directory
- [ ] Application Pool assigned to site
- [ ] Site bindings configured (ports)
- [ ] Preload Enabled set to True
- [ ] SSL certificate bound (for HTTPS)
- [ ] Environment variables configured
- [ ] Connection strings configured
- [ ] Logs directory created with permissions
- [ ] IIS_IUSRS permissions granted
- [ ] web.config reviewed and correct

### Post-Deployment Verification

- [ ] IIS site started successfully
- [ ] Application Pool is running
- [ ] w3wp.exe process visible in Task Manager
- [ ] No errors in stdout logs
- [ ] No errors in Windows Event Viewer
- [ ] Health endpoint responding: `/health`
- [ ] Swagger UI accessible (if enabled): `/swagger`
- [ ] Home page loads in browser
- [ ] Blazor WASM client initializes
- [ ] API calls successful (check browser network tab)
- [ ] Database connectivity confirmed
- [ ] Authentication/login working
- [ ] QZ Tray integration working (if applicable)
- [ ] All critical features tested

### Troubleshooting Checklist

If the application isn't working:

- [ ] Check stdout logs: `logs\stdout_*.log`
- [ ] Check Windows Event Viewer: Application log
- [ ] Check IIS worker process: Task Manager → w3wp.exe
- [ ] Verify Application Pool status: Should be Started
- [ ] Verify .NET runtime: `dotnet --list-runtimes`
- [ ] Check file permissions: IIS_IUSRS has access
- [ ] Test database connection from server
- [ ] Verify web.config is present and correct
- [ ] Check browser console for client errors (F12)
- [ ] Verify URL Rewrite module installed
- [ ] Check firewall/antivirus not blocking
- [ ] Review recent changes/deployments

## Maintenance and Updates

### Updating the Application

1. **Backup current deployment:**
   ```powershell
   # Stop the site
   Stop-WebSite -Name "EventForge"
   
   # Backup files
   Copy-Item "C:\inetpub\EventForge" -Destination "C:\inetpub\Backups\EventForge_$(Get-Date -Format 'yyyyMMdd_HHmmss')" -Recurse
   ```

2. **Publish new version:**
   ```bash
   dotnet publish EventForge.Server -c Release -o ./publish-output
   ```

3. **Deploy update:**
   ```powershell
   # Copy new files (excluding web.config if customized)
   Copy-Item ".\publish-output\*" -Destination "C:\inetpub\EventForge" -Recurse -Force -Exclude web.config
   ```

4. **Restart site:**
   ```powershell
   Start-WebSite -Name "EventForge"
   ```

5. **Verify deployment:**
   - Check health endpoint
   - Test critical functionality
   - Monitor logs for errors

### Updating ASP.NET Core Runtime

When new .NET versions are released:

1. Download latest Hosting Bundle
2. Run installer
3. Restart IIS: `iisreset`
4. Verify: `dotnet --list-runtimes`
5. Test application functionality
6. Monitor logs for compatibility issues

### Log Rotation

Stdout logs can grow large. Implement rotation:

```powershell
# Script to clean old stdout logs (older than 7 days)
$logPath = "C:\inetpub\EventForge\logs"
$daysToKeep = 7
Get-ChildItem $logPath -Filter "stdout_*.log" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$daysToKeep) } |
    Remove-Item -Force
```

Schedule this script in Windows Task Scheduler.

### Monitoring Application Health

Set up automated monitoring:

```powershell
# PowerShell script to check health endpoint
$url = "https://yoursite.com/health"
$response = Invoke-WebRequest -Uri $url -UseBasicParsing
if ($response.StatusCode -ne 200) {
    # Send alert (email, SMS, etc.)
    Write-Error "Application health check failed!"
}
```

## Advanced Topics

### Load Balancing and High Availability

For production environments requiring high availability:

1. **Web Farm Configuration:**
   - Deploy to multiple IIS servers
   - Use a load balancer (Azure Load Balancer, F5, etc.)
   - Share session state using Redis or SQL Server

2. **Session State:**
   - Configure distributed caching in Program.cs
   - Use Redis or SQL Server for session storage
   - Update connection strings appropriately

3. **Database High Availability:**
   - Use SQL Server Always On Availability Groups
   - Configure connection string with failover partner
   - Test failover scenarios

### Continuous Integration/Continuous Deployment (CI/CD)

Automate deployment using Azure DevOps, GitHub Actions, or Jenkins:

**Example GitHub Actions workflow:**
```yaml
name: Deploy to IIS
on:
  push:
    branches: [ main ]
jobs:
  deploy:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v2
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '9.0.x'
    - name: Publish
      run: dotnet publish EventForge.Server -c Release -o ./publish
    - name: Deploy to IIS
      run: |
        Stop-WebSite -Name "EventForge"
        Copy-Item .\publish\* C:\inetpub\EventForge -Recurse -Force
        Start-WebSite -Name "EventForge"
```

### Blue-Green Deployment

For zero-downtime deployments:

1. Set up two identical IIS sites (Blue and Green)
2. Deploy new version to inactive site
3. Test inactive site
4. Switch load balancer to point to new site
5. Keep old site as rollback option

### Docker Container Deployment (Alternative to IIS)

While this guide focuses on IIS, EventForge can also be deployed in Docker:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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

## Additional Resources

### Official Documentation
- [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Host ASP.NET Core on Windows with IIS](https://docs.microsoft.com/aspnet/core/host-and-deploy/iis/)
- [ASP.NET Core Module (ANCM)](https://docs.microsoft.com/aspnet/core/host-and-deploy/aspnet-core-module)
- [Blazor WebAssembly Deployment](https://docs.microsoft.com/aspnet/core/blazor/host-and-deploy/webassembly)
- [IIS Configuration Reference](https://docs.microsoft.com/iis/configuration/)

### Tools
- [IIS Manager](https://www.iis.net/downloads/microsoft/iis-manager)
- [URL Rewrite Module](https://www.iis.net/downloads/microsoft/url-rewrite)
- [Let's Encrypt with win-acme](https://www.win-acme.com/)
- [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/asp-net-core)

### Community Resources
- [ASP.NET Core GitHub](https://github.com/dotnet/aspnetcore)
- [Stack Overflow - ASP.NET Core](https://stackoverflow.com/questions/tagged/asp.net-core)
- [.NET Blog](https://devblogs.microsoft.com/dotnet/)

## Summary

This guide covers the complete process of deploying EventForge to IIS with automatic startup:

1. ✅ **Prerequisites**: Install ASP.NET Core Hosting Bundle
2. ✅ **Publishing**: Use `dotnet publish` to create deployment package
3. ✅ **IIS Configuration**: Create Application Pool with "No Managed Code" and "AlwaysRunning"
4. ✅ **Site Setup**: Create site with proper bindings (IIS manages ports)
5. ✅ **Automatic Startup**: Enable "Preload" and configure "AlwaysRunning" start mode
6. ✅ **Environment Variables**: Configure via web.config or IIS Application Settings
7. ✅ **web.config**: Use in-process hosting for optimal performance
8. ✅ **Security**: Use HTTPS, secure secrets, appropriate permissions
9. ✅ **Monitoring**: Check health endpoints, stdout logs, Event Viewer
10. ✅ **Maintenance**: Regular updates, log rotation, monitoring

**Key Principle**: In IIS deployments, ports are NEVER configured in application code. IIS manages all port binding through site bindings, providing flexibility and security.

---

**Document Version:** 2.0  
**Last Updated:** 2025-01-12  
**Author:** EventForge Team  
**For:** EventForge IIS Deployment
