# CoverageChecker - Onda 4 Quality Gate Tool

## Purpose

CoverageChecker is a standalone .NET console tool that enforces code coverage thresholds as required by Issue #705 (Onda 4 - Obiettivo 3: Testing Avanzato & Quality Gate).

This tool validates that:
- ✅ Line coverage is ≥ 80%
- ✅ Branch coverage is ≥ 75%
- ✅ Missing coverage is ≤ 10%

## Requirements from Issue #705

**Objective 3: Testing Avanzato & Quality Gate**
- Obbligatorietà test coverage >80% sulle nuove feature
- Script CI per report metriche e coverage, fail >10% missing
- Benchmark dei tempi di caricamento e pipeline logging
- Automated quality metrics reporting on PRs

## Usage

### In CI/CD (Automated)

The tool is automatically invoked by the `quality-gate.yml` workflow on every PR and push to master. No manual intervention required.

```yaml
- name: Check Coverage Thresholds
  run: |
    dotnet run --project scripts/CoverageChecker/CoverageChecker.csproj -- \
      --file TestResults/coverage/Summary.json \
      --min-line-coverage 80 \
      --min-branch-coverage 75 \
      --max-missing 10
```

### Locally (Manual Testing)

1. **Run tests with coverage:**
   ```bash
   dotnet test --configuration Release --collect:"XPlat Code Coverage"
   ```

2. **Generate coverage report:**
   ```bash
   dotnet tool install -g dotnet-reportgenerator-globaltool
   reportgenerator \
     -reports:"**/coverage.cobertura.xml" \
     -targetdir:"TestResults/coverage" \
     -reporttypes:"Html;JsonSummary"
   ```

3. **Check coverage thresholds:**
   ```bash
   dotnet run --project scripts/CoverageChecker/CoverageChecker.csproj -- \
     --file TestResults/coverage/Summary.json
   ```

### Using the Local Script

For convenience, use the provided script:

```bash
./scripts/coverage-report.sh
```

This script will:
1. Run all tests with coverage collection
2. Generate HTML and JSON reports with ReportGenerator
3. Run CoverageChecker to validate thresholds
4. Open the HTML report in your browser

## Configuration Options

### Command-Line Arguments

| Argument | Default | Description |
|----------|---------|-------------|
| `--file` | *Required* | Path to ReportGenerator Summary.json file |
| `--min-line-coverage` | 80.0 | Minimum line coverage percentage (0-100) |
| `--min-branch-coverage` | 75.0 | Minimum branch coverage percentage (0-100) |
| `--max-missing` | 10.0 | Maximum missing coverage percentage (0-100) |

### Examples

**Default thresholds (Onda 4 requirements):**
```bash
dotnet run --project scripts/CoverageChecker/CoverageChecker.csproj -- \
  --file TestResults/coverage/Summary.json
```

**Custom thresholds:**
```bash
dotnet run --project scripts/CoverageChecker/CoverageChecker.csproj -- \
  --file TestResults/coverage/Summary.json \
  --min-line-coverage 85 \
  --min-branch-coverage 80 \
  --max-missing 5
```

**Strict mode (no missing coverage allowed):**
```bash
dotnet run --project scripts/CoverageChecker/CoverageChecker.csproj -- \
  --file TestResults/coverage/Summary.json \
  --min-line-coverage 100 \
  --min-branch-coverage 100 \
  --max-missing 0
```

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | ✓ All thresholds passed - build should succeed |
| `1` | ✗ One or more thresholds failed - build should fail |

## Output Format

The tool provides colored console output with clear pass/fail indicators:

```
═══════════════════════════════════════════════════════
  Coverage Report - Onda 4 Quality Gate
═══════════════════════════════════════════════════════

  ✓ Line Coverage        85.23%  (threshold: ≥ 80%)
  ✓ Branch Coverage      78.45%  (threshold: ≥ 75%)
  ✓ Missing Coverage      8.12%  (threshold: ≤ 10%)

  Covered Lines:    1234/1448
  Covered Branches: 567/723

═══════════════════════════════════════════════════════

✓ All coverage thresholds passed!
```

## Integration with CI/CD

### quality-gate.yml Workflow

The tool is integrated into the `quality-gate.yml` workflow which:

1. **Builds** the entire solution
2. **Runs** all tests with XPlat Code Coverage
3. **Generates** coverage reports (HTML, JSON, Cobertura)
4. **Validates** thresholds with CoverageChecker (FAILS BUILD if not met)
5. **Uploads** coverage to Codecov for historical tracking
6. **Posts** coverage summary as PR comment
7. **Uploads** coverage artifacts for download

### Build Failure

If any threshold is not met, the CI build will fail with a clear error message:

```
✗ Coverage thresholds not met!
  - Line coverage 75.23% is below minimum 80%
  - Missing coverage 12.45% exceeds maximum 10%
Error: Process completed with exit code 1.
```

## Technical Details

### Technology Stack

- **Language**: C# / .NET 10.0
- **CLI Framework**: System.CommandLine (2.0.0-beta4)
- **Input Format**: ReportGenerator JSON Summary
- **Output**: Colored console text with Unicode symbols

### Input File Format

The tool expects a ReportGenerator `Summary.json` file with the following structure:

```json
{
  "summary": {
    "coveredlines": 1234,
    "coverablelines": 1448,
    "totallines": 2100,
    "linecoverage": 85.23,
    "coveredbranches": 567,
    "totalbranches": 723,
    "branchcoverage": 78.45
  }
}
```

### Calculation Logic

**Line Coverage**: Directly from `linecoverage` field (percentage)

**Branch Coverage**: Directly from `branchcoverage` field (percentage)

**Missing Coverage**: Calculated as:
```
missing_percentage = ((coverablelines - coveredlines) / coverablelines) * 100
```

## Development

### Building the Tool

```bash
cd scripts/CoverageChecker
dotnet build
```

### Running Tests (Future Enhancement)

```bash
cd scripts/CoverageChecker
dotnet test
```

## Related Files

- `.github/workflows/quality-gate.yml` - CI workflow using this tool
- `.github/workflows/benchmark.yml` - Performance benchmarking workflow
- `scripts/coverage-report.sh` - Local coverage report script
- `EventForge.Benchmarks/` - BenchmarkDotNet project

## References

- **Issue #705**: Onda 4 - Obiettivo 3 (Testing Avanzato & Quality Gate)
- **ReportGenerator**: https://github.com/danielpalme/ReportGenerator
- **Coverlet**: https://github.com/coverlet-coverage/coverlet
- **System.CommandLine**: https://github.com/dotnet/command-line-api

## Support

For issues or questions about coverage requirements, refer to Issue #705 or contact the development team.
