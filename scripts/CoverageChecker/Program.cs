using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoverageChecker;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var fileOption = new Option<FileInfo?>(
            name: "--file",
            description: "Path to ReportGenerator Summary.json file")
        {
            IsRequired = true
        };

        var minLineCoverageOption = new Option<double>(
            name: "--min-line-coverage",
            description: "Minimum line coverage percentage required",
            getDefaultValue: () => 80.0);

        var minBranchCoverageOption = new Option<double>(
            name: "--min-branch-coverage",
            description: "Minimum branch coverage percentage required",
            getDefaultValue: () => 75.0);

        var maxMissingOption = new Option<double>(
            name: "--max-missing",
            description: "Maximum missing coverage percentage allowed",
            getDefaultValue: () => 10.0);

        var rootCommand = new RootCommand("Coverage Checker - Validates code coverage thresholds for Onda 4")
        {
            fileOption,
            minLineCoverageOption,
            minBranchCoverageOption,
            maxMissingOption
        };

        rootCommand.SetHandler(
            (file, minLine, minBranch, maxMiss) =>
            {
                var exitCode = CheckCoverage(file, minLine, minBranch, maxMiss);
                Environment.ExitCode = exitCode;
            },
            fileOption,
            minLineCoverageOption,
            minBranchCoverageOption,
            maxMissingOption);

        return await rootCommand.InvokeAsync(args);
    }

    static int CheckCoverage(FileInfo? file, double minLineCoverage, double minBranchCoverage, double maxMissing)
    {
        if (file == null || !file.Exists)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: File not found: {file?.FullName ?? "null"}");
            Console.ResetColor();
            return 1;
        }

        try
        {
            var jsonContent = File.ReadAllText(file.FullName);
            var summary = JsonSerializer.Deserialize<CoverageSummary>(jsonContent);

            if (summary == null || summary.Summary == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Error: Invalid or empty coverage summary");
                Console.ResetColor();
                return 1;
            }

            var lineCoverage = summary.Summary.Linecoverage;
            var branchCoverage = summary.Summary.Branchcoverage;

            // Calculate missing percentage
            var coveredLines = summary.Summary.Coveredlines;
            var coverableLines = summary.Summary.Coverablelines;
            var missingLines = coverableLines - coveredLines;
            var missingPercentage = coverableLines > 0 ? (double)missingLines / coverableLines * 100.0 : 0.0;

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("  Coverage Report - Onda 4 Quality Gate");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();

            PrintMetric("Line Coverage", lineCoverage, minLineCoverage, "≥");
            PrintMetric("Branch Coverage", branchCoverage, minBranchCoverage, "≥");
            PrintMetric("Missing Coverage", missingPercentage, maxMissing, "≤");

            Console.WriteLine();
            Console.WriteLine($"  Covered Lines:    {coveredLines}/{coverableLines}");
            Console.WriteLine($"  Covered Branches: {summary.Summary.Coveredbranches}/{summary.Summary.Totalbranches}");
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine();

            bool linePassed = lineCoverage >= minLineCoverage;
            bool branchPassed = branchCoverage >= minBranchCoverage;
            bool missingPassed = missingPercentage <= maxMissing;

            if (linePassed && branchPassed && missingPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All coverage thresholds passed!");
                Console.ResetColor();
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Coverage thresholds not met!");
                if (!linePassed)
                    Console.WriteLine($"  - Line coverage {lineCoverage:F2}% is below minimum {minLineCoverage}%");
                if (!branchPassed)
                    Console.WriteLine($"  - Branch coverage {branchCoverage:F2}% is below minimum {minBranchCoverage}%");
                if (!missingPassed)
                    Console.WriteLine($"  - Missing coverage {missingPercentage:F2}% exceeds maximum {maxMissing}%");
                Console.ResetColor();
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error processing coverage file: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static void PrintMetric(string name, double value, double threshold, string comparison)
    {
        bool passed = comparison == "≥" ? value >= threshold : value <= threshold;

        Console.ForegroundColor = passed ? ConsoleColor.Green : ConsoleColor.Red;
        string status = passed ? "✓" : "✗";
        Console.Write($"  {status} ");
        Console.ResetColor();

        Console.Write($"{name,-20} ");
        Console.ForegroundColor = passed ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write($"{value,6:F2}%");
        Console.ResetColor();
        Console.WriteLine($"  (threshold: {comparison} {threshold}%)");
    }
}

class CoverageSummary
{
    [JsonPropertyName("summary")]
    public SummaryData? Summary { get; set; }
}

class SummaryData
{
    [JsonPropertyName("coveredlines")]
    public int Coveredlines { get; set; }

    [JsonPropertyName("coverablelines")]
    public int Coverablelines { get; set; }

    [JsonPropertyName("totallines")]
    public int Totallines { get; set; }

    [JsonPropertyName("linecoverage")]
    public double Linecoverage { get; set; }

    [JsonPropertyName("coveredbranches")]
    public int Coveredbranches { get; set; }

    [JsonPropertyName("totalbranches")]
    public int Totalbranches { get; set; }

    [JsonPropertyName("branchcoverage")]
    public double Branchcoverage { get; set; }
}
