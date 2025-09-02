using System;
using System.IO;
using System.Text;
using EventForge.Audit;

namespace EventForge.Audit
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("EventForge Automated Audit Tool");
                Console.WriteLine("=================================");
                Console.WriteLine();

                // Get project root directory
                var projectRoot = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
                
                if (!Directory.Exists(Path.Combine(projectRoot, "EventForge.Server")))
                {
                    // Try going up one level if we're in the audit directory
                    if (Path.GetFileName(projectRoot) == "audit")
                    {
                        projectRoot = Path.GetDirectoryName(projectRoot) ?? projectRoot;
                    }
                }

                Console.WriteLine($"Auditing project at: {projectRoot}");
                Console.WriteLine();

                // Run the audit
                var auditor = new CodebaseAuditor(projectRoot);
                var report = auditor.RunFullAudit();

                // Generate report
                var reportGenerator = new MarkdownReportGenerator();
                var markdownReport = reportGenerator.GenerateReport(report);

                // Save report to file
                var reportPath = Path.Combine(projectRoot, "audit", "AUDIT_REPORT.md");
                File.WriteAllText(reportPath, markdownReport, Encoding.UTF8);

                Console.WriteLine();
                Console.WriteLine($"Audit completed! Report saved to: {reportPath}");
                Console.WriteLine();
                Console.WriteLine("Summary:");
                Console.WriteLine($"- Total Issues Found: {report.Issues.Count}");
                Console.WriteLine($"- Critical Issues: {report.Issues.Count(i => i.Severity == IssueSeverity.Critical)}");
                Console.WriteLine($"- High Priority Issues: {report.Issues.Count(i => i.Severity == IssueSeverity.High)}");
                Console.WriteLine($"- Medium Priority Issues: {report.Issues.Count(i => i.Severity == IssueSeverity.Medium)}");
                Console.WriteLine($"- Low Priority Issues: {report.Issues.Count(i => i.Severity == IssueSeverity.Low)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running audit: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.ExitCode = 1;
            }
        }
    }
}

public static class ListExtensions
{
    public static int Count<T>(this System.Collections.Generic.List<T> list, Func<T, bool> predicate)
    {
        int count = 0;
        foreach (var item in list)
        {
            if (predicate(item))
                count++;
        }
        return count;
    }
}