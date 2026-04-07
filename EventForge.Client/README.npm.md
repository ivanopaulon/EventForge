# EventForge.Client - npm Dependencies

This Blazor WebAssembly client uses npm to manage JavaScript dependencies.

## Setup

When you first clone the repository or update dependencies:

```bash
cd EventForge.Client
npm install
```

### Build Process

The .NET build process will automatically include any `wwwroot/js/` files in the output.

### Local Development

- `node_modules/` is gitignored and should not be committed
- Run `npm install` after cloning or pulling changes that update `package.json`
