# EventForge E2E Tests

This project contains End-to-End (E2E) tests for EventForge using Playwright and NUnit.

## Overview

The E2E test suite validates the functionality and performance of key EventForge pages:
- **Products Management** (`/product-management/products`)
- **Warehouse Management** (`/warehouse/facilities`)
- **Business Party Management** (`/business/suppliers`)

## Technology Stack

- **Framework**: .NET 10.0
- **Test Runner**: NUnit 4.3+
- **Browser Automation**: Playwright 1.51+
- **Browser**: Chromium (configurable)

## Prerequisites

1. .NET 10 SDK installed
2. EventForge application running (Server + Client)
3. Playwright browsers installed

## Installation

### 1. Install Playwright Browsers

Run this command once before running tests:

```bash
pwsh bin/Debug/net10.0/playwright.ps1 install
```

Or on Linux/macOS:

```bash
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

## Configuration

Tests can be configured using environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `E2E_BASE_URL` | Base URL of the Blazor WASM client | `http://localhost:5050` |
| `E2E_API_URL` | Base URL of the API server | `http://localhost:5000` |

### Setting Environment Variables

**Windows (PowerShell):**
```powershell
$env:E2E_BASE_URL="http://localhost:5050"
$env:E2E_API_URL="http://localhost:5000"
```

**Linux/macOS:**
```bash
export E2E_BASE_URL="http://localhost:5050"
export E2E_API_URL="http://localhost:5000"
```

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Categories

**Smoke Tests:**
```bash
dotnet test --filter "Category=Smoke"
```

**Performance Tests:**
```bash
dotnet test --filter "Category=Performance"
```

**E2E Tests:**
```bash
dotnet test --filter "Category=E2E"
```

### Run Tests for a Specific Page

**Products Page:**
```bash
dotnet test --filter "FullyQualifiedName~ProductsPageTests"
```

**Warehouse Page:**
```bash
dotnet test --filter "FullyQualifiedName~WarehousePageTests"
```

**Business Party Page:**
```bash
dotnet test --filter "FullyQualifiedName~BusinessPartyPageTests"
```

### Run Tests in Headed Mode (See Browser)

Set the `HEADED` environment variable:

```bash
# Windows PowerShell
$env:HEADED="1"
dotnet test

# Linux/macOS
HEADED=1 dotnet test
```

### Run Tests with Specific Browser

Playwright supports Chromium, Firefox, and WebKit:

```bash
# Configure in runsettings or use environment variables
# See: https://playwright.dev/dotnet/docs/test-runners
```

## Test Structure

```
EventForge.Tests.E2E/
├── EventForge.Tests.E2E.csproj    # Project file with dependencies
├── PlaywrightSetup.cs              # Base test class with utilities
├── README.md                       # This file
└── Pages/                          # Page-specific tests
    ├── ProductsPageTests.cs        # Tests for Products page
    ├── WarehousePageTests.cs       # Tests for Warehouse page
    └── BusinessPartyPageTests.cs   # Tests for Business Party page
```

## Test Categories

- **Smoke**: Basic functionality tests that verify pages load
- **Performance**: Tests that verify page load times (<3s requirement)
- **E2E**: Comprehensive end-to-end user journey tests

## Features

### Automatic Screenshot Capture

Tests automatically capture full-page screenshots on failure. Screenshots are saved to:
```
TestResults/screenshots/{TestName}_{Timestamp}.png
```

### Performance Benchmarking

Performance tests verify that pages load within 3 seconds:
- Products page: <3s
- Warehouse page: <3s
- Business Party page: <3s

### Responsive Design Testing

Tests verify that pages render correctly on:
- Mobile viewports (375x667)
- Desktop viewports (1920x1080)

## Test Coverage

| Page | Tests | Categories |
|------|-------|-----------|
| Products | 6 | Smoke, Performance, E2E, Responsive |
| Warehouse | 4 | Smoke, Performance, E2E |
| Business Party | 4 | Smoke, Performance, E2E |
| **Total** | **14** | |

## Continuous Integration

Tests are automatically run in CI/CD via GitHub Actions workflow: `.github/workflows/e2e-tests.yml`

The workflow:
- Runs on pull requests and pushes to main
- Sets up .NET 10
- Starts EventForge application
- Installs Playwright browsers
- Runs all E2E tests
- Uploads screenshots on failure

## Troubleshooting

### Playwright Browsers Not Installed

**Error**: `Executable doesn't exist at ...`

**Solution**: Run the Playwright install command:
```bash
pwsh bin/Debug/net10.0/playwright.ps1 install
```

### Application Not Running

**Error**: Tests timeout or fail to connect

**Solution**: Ensure EventForge Server and Client are running:
```bash
# Terminal 1 - Start Server
cd EventForge.Server
dotnet run

# Terminal 2 - Start Client
cd EventForge.Client
dotnet run
```

### Port Conflicts

**Error**: Connection refused or timeout

**Solution**: Check if ports 5000 (API) and 5050 (Client) are in use and update environment variables if needed.

### Timeout Errors

**Error**: Test times out waiting for elements

**Solution**: Increase timeout in `PlaywrightSetup.cs` or check if the application is responding slowly.

## Best Practices

1. **Run tests in headless mode** in CI/CD (default)
2. **Use headed mode** for debugging locally
3. **Check screenshots** when tests fail
4. **Ensure application is running** before running tests
5. **Use specific test categories** to run targeted test suites
6. **Review performance metrics** in test output

## Contributing

When adding new tests:

1. Inherit from `PlaywrightSetup` base class
2. Use appropriate test categories (`[Category("...")]`)
3. Add descriptive test names and descriptions
4. Follow the Arrange-Act-Assert pattern
5. Use the utility methods from `PlaywrightSetup`
6. Ensure tests are idempotent (can run multiple times)

## Resources

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [NUnit Documentation](https://docs.nunit.org/)
- [EventForge Documentation](../README.md)

## Support

For issues or questions, please:
1. Check the troubleshooting section above
2. Review Playwright documentation
3. Open an issue in the EventForge repository
