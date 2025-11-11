# EventForge Client Deployment Guide

## Overview

When publishing `EventForge.Server`, the Blazor WebAssembly client (`EventForge.Client`) is automatically published and its output is copied into the server's `wwwroot` directory. This allows the server to host the client as static files, enabling a unified deployment to IIS or other web servers.

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

## IIS Deployment

### Basic Deployment

1. Copy the contents of the publish output directory to your IIS site directory
2. Ensure the IIS application pool is configured to use "No Managed Code"
3. Install the [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet) on the server
4. The included `web.config` handles:
   - Correct MIME types for `.wasm`, `.json`, `.br`, and `.gz` files
   - SPA fallback routing (all non-API, non-file requests route to index.html)
   - API route exclusion (requests to `/api/*` are handled by the server)

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

## Troubleshooting

### Client not loading
- Verify `wwwroot/index.html` exists in the published output
- Check browser console for 404 errors
- Ensure base href is correct for your deployment path

### WASM file not loading with correct MIME type
- Verify `web.config` is present in wwwroot
- Check IIS has the URL Rewrite module installed
- Confirm the `.wasm` MIME type mapping in IIS

### API calls failing
- Ensure API calls use relative paths (e.g., `/api/products`)
- Check that the API endpoints are correctly configured in the server
- Verify the SPA fallback rule excludes `/api` paths

## Development vs Production

In development, the client and server run separately (typically on different ports). The client proxies API requests to the server.

In production (after publishing), both client and server are served from the same origin, eliminating CORS concerns and simplifying deployment.
