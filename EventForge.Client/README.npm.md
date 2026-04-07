# EventForge.Client - npm Dependencies

This Blazor WebAssembly client uses npm to manage JavaScript dependencies.

## QZ Tray Integration

QZ Tray is installed as an npm dependency and copied to `wwwroot/js/` for use in the application.

### Setup

When you first clone the repository or update dependencies:

```bash
cd EventForge.Client
npm install
```

The `postinstall` script automatically copies `qz-tray.js` from `node_modules` to `wwwroot/js/`.

### Updating QZ Tray

To update QZ Tray to a new version:

1. Update the version in `package.json`
2. Run `npm install`
3. Test the printing functionality
4. Commit both `package.json` and `package-lock.json`

### Build Process

The .NET build process will automatically include `wwwroot/js/qz-tray.js` in the output.
The file is marked as `CopyToOutputDirectory: Always` in `EventForge.Client.csproj`.

### Local Development

- `node_modules/` is gitignored and should not be committed
- `wwwroot/js/qz-tray.js` is committed to the repository for build convenience
- Run `npm install` after cloning or pulling changes that update `package.json`
