using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventForge.Audit
{
    public class MarkdownReportGenerator
    {
        public string GenerateReport(AuditReport report)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("# EventForge Backend Audit Report");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();
            sb.AppendLine("This automated audit verifies the completion status of three major refactoring PRs:");
            sb.AppendLine("- **PR1**: DTO Consolidation");
            sb.AppendLine("- **PR2**: CRUD/Services Refactoring");
            sb.AppendLine("- **PR3**: Controllers/API Refactoring");
            sb.AppendLine();

            // Executive Summary
            GenerateExecutiveSummary(sb, report);

            // Detailed Statistics
            GenerateStatistics(sb, report);

            // Issues by Category
            GenerateIssuesByCategory(sb, report);

            // PR Compliance Status
            GeneratePRComplianceStatus(sb, report);

            // Recommendations
            GenerateRecommendations(sb, report);

            // Checklist
            GenerateActionableChecklist(sb, report);

            return sb.ToString();
        }

        private void GenerateExecutiveSummary(StringBuilder sb, AuditReport report)
        {
            sb.AppendLine("## Executive Summary");
            sb.AppendLine();

            var totalIssues = report.Issues.Count;
            var criticalIssues = report.Issues.Count(i => i.Severity == IssueSeverity.Critical);
            var highIssues = report.Issues.Count(i => i.Severity == IssueSeverity.High);
            var mediumIssues = report.Issues.Count(i => i.Severity == IssueSeverity.Medium);
            var lowIssues = report.Issues.Count(i => i.Severity == IssueSeverity.Low);

            sb.AppendLine($"📊 **Total Issues Found:** {totalIssues}");
            sb.AppendLine();
            sb.AppendLine("| Severity | Count | Description |");
            sb.AppendLine("|----------|--------|-------------|");
            sb.AppendLine($"| 🔴 Critical | {criticalIssues} | Issues that prevent proper functionality |");
            sb.AppendLine($"| 🟠 High | {highIssues} | Issues that should be addressed immediately |");
            sb.AppendLine($"| 🟡 Medium | {mediumIssues} | Issues that impact code quality |");
            sb.AppendLine($"| 🟢 Low | {lowIssues} | Minor improvements and best practices |");
            sb.AppendLine();

            // Overall compliance status
            var overallStatus = GetOverallComplianceStatus(report);
            sb.AppendLine($"**Overall Compliance Status:** {overallStatus}");
            sb.AppendLine();
        }

        private void GenerateStatistics(StringBuilder sb, AuditReport report)
        {
            sb.AppendLine("## Detailed Statistics");
            sb.AppendLine();

            // DTO Consolidation Stats
            sb.AppendLine("### PR1: DTO Consolidation Status");
            sb.AppendLine($"- ✅ Consolidated DTO Files: {report.Statistics.ConsolidatedDTOFiles}");
            sb.AppendLine($"- ✅ Domain Folders: {report.Statistics.DTODomainFolders}");
            sb.AppendLine($"- ❌ Legacy DTO References: {report.Statistics.LegacyDTOReferences}");
            sb.AppendLine($"- ❌ Inline DTOs in Controllers: {report.Statistics.InlineDTOs}");
            sb.AppendLine();

            // Services Refactoring Stats
            sb.AppendLine("### PR2: Services Refactoring Status");
            sb.AppendLine($"- ❌ Non-async Task Methods: {report.Statistics.NonAsyncTaskMethods}");
            sb.AppendLine($"- ❌ Redundant Status Assignments: {report.Statistics.RedundantStatusAssignments}");
            sb.AppendLine($"- ❌ Missing Exception Handling: {report.Statistics.MissingExceptionHandling}");
            sb.AppendLine($"- ❌ Sync-over-Async Patterns: {report.Statistics.SyncOverAsyncPatterns}");
            sb.AppendLine($"- ⚠️ Missing ConfigureAwait: {report.Statistics.MissingConfigureAwait}");
            sb.AppendLine();

            // Controllers Refactoring Stats
            sb.AppendLine("### PR3: Controllers Refactoring Status");
            sb.AppendLine($"- ❌ Controllers Not Inheriting BaseApiController: {report.Statistics.ControllersNotInheritingBase}");
            sb.AppendLine($"- ❌ Direct StatusCode Usage: {report.Statistics.DirectStatusCodeUsage}");
            sb.AppendLine($"- ❌ Unversioned API Routes: {report.Statistics.UnversionedAPIRoutes}");
            sb.AppendLine($"- ❌ Controllers Without Tenant Validation: {report.Statistics.ControllersWithoutTenantValidation}");
            sb.AppendLine($"- ⚠️ Controllers Without Swagger Docs: {report.Statistics.ControllersWithoutSwaggerDocs}");
            sb.AppendLine($"- ❌ Non-RFC7807 Error Responses: {report.Statistics.NonRFC7807ErrorResponses}");
            sb.AppendLine();

            // Additional Quality Stats
            sb.AppendLine("### Code Quality Statistics");
            sb.AppendLine($"- ⚠️ DTOs Without Validation: {report.Statistics.DTOsWithoutValidation}");
            sb.AppendLine();
        }

        private void GenerateIssuesByCategory(StringBuilder sb, AuditReport report)
        {
            var categories = report.Issues.GroupBy(i => i.Category).OrderBy(g => g.Key);

            sb.AppendLine("## Issues by Category");
            sb.AppendLine();

            foreach (var category in categories)
            {
                sb.AppendLine($"### {category.Key}");
                sb.AppendLine();

                var issuesBySeverity = category.GroupBy(i => i.Severity).OrderByDescending(g => (int)g.Key);

                foreach (var severityGroup in issuesBySeverity)
                {
                    sb.AppendLine($"#### {GetSeverityIcon(severityGroup.Key)} {severityGroup.Key} Priority");
                    sb.AppendLine();

                    foreach (var issue in severityGroup)
                    {
                        sb.AppendLine($"**File:** `{issue.File}`");
                        sb.AppendLine($"**Issue:** {issue.Description}");
                        sb.AppendLine($"**Details:** {issue.Details}");
                        sb.AppendLine();
                    }
                }
            }
        }

        private void GeneratePRComplianceStatus(StringBuilder sb, AuditReport report)
        {
            sb.AppendLine("## PR Compliance Status");
            sb.AppendLine();

            // PR1 Status
            var pr1Status = GetPR1ComplianceStatus(report);
            sb.AppendLine($"### PR1: DTO Consolidation - {pr1Status.status}");
            sb.AppendLine($"**Completion:** {pr1Status.percentage}%");
            sb.AppendLine();
            foreach (var item in pr1Status.items)
            {
                sb.AppendLine($"- {item}");
            }
            sb.AppendLine();

            // PR2 Status
            var pr2Status = GetPR2ComplianceStatus(report);
            sb.AppendLine($"### PR2: Services Refactoring - {pr2Status.status}");
            sb.AppendLine($"**Completion:** {pr2Status.percentage}%");
            sb.AppendLine();
            foreach (var item in pr2Status.items)
            {
                sb.AppendLine($"- {item}");
            }
            sb.AppendLine();

            // PR3 Status
            var pr3Status = GetPR3ComplianceStatus(report);
            sb.AppendLine($"### PR3: Controllers Refactoring - {pr3Status.status}");
            sb.AppendLine($"**Completion:** {pr3Status.percentage}%");
            sb.AppendLine();
            foreach (var item in pr3Status.items)
            {
                sb.AppendLine($"- {item}");
            }
            sb.AppendLine();
        }

        private void GenerateRecommendations(StringBuilder sb, AuditReport report)
        {
            sb.AppendLine("## Recommendations");
            sb.AppendLine();

            sb.AppendLine("### Immediate Actions Required");
            sb.AppendLine();

            var highPriorityIssues = report.Issues.Where(i => i.Severity >= IssueSeverity.High).ToList();
            if (highPriorityIssues.Any())
            {
                sb.AppendLine("1. **Address High/Critical Priority Issues**");
                foreach (var category in highPriorityIssues.GroupBy(i => i.Category))
                {
                    sb.AppendLine($"   - {category.Key}: {category.Count()} issues");
                }
                sb.AppendLine();
            }

            if (report.Statistics.LegacyDTOReferences > 0)
            {
                sb.AppendLine("2. **Complete DTO Consolidation**");
                sb.AppendLine("   - Remove all legacy DTO namespace references");
                sb.AppendLine("   - Move inline DTOs to Prym.DTOs project");
                sb.AppendLine();
            }

            if (report.Statistics.ControllersNotInheritingBase > 0)
            {
                sb.AppendLine("3. **Complete Controller Refactoring**");
                sb.AppendLine("   - Ensure all controllers inherit from BaseApiController");
                sb.AppendLine("   - Implement RFC7807 error handling");
                sb.AppendLine("   - Add multi-tenant validation where appropriate");
                sb.AppendLine();
            }

            sb.AppendLine("### Long-term Improvements");
            sb.AppendLine("- Implement comprehensive validation attributes on DTOs");
            sb.AppendLine("- Add ConfigureAwait(false) to library code for better performance");
            sb.AppendLine("- Complete Swagger documentation for all endpoints");
            sb.AppendLine("- Consider implementing integration tests for multi-tenant scenarios");
            sb.AppendLine();
        }

        private void GenerateActionableChecklist(StringBuilder sb, AuditReport report)
        {
            sb.AppendLine("## Actionable Checklist");
            sb.AppendLine();

            sb.AppendLine("### 🔴 Critical Tasks");
            var criticalTasks = GetCriticalTasks(report);
            foreach (var task in criticalTasks)
            {
                sb.AppendLine($"- [ ] {task}");
            }
            sb.AppendLine();

            sb.AppendLine("### 🟠 High Priority Tasks");
            var highTasks = GetHighPriorityTasks(report);
            foreach (var task in highTasks)
            {
                sb.AppendLine($"- [ ] {task}");
            }
            sb.AppendLine();

            sb.AppendLine("### 🟡 Medium Priority Tasks");
            var mediumTasks = GetMediumPriorityTasks(report);
            foreach (var task in mediumTasks)
            {
                sb.AppendLine($"- [ ] {task}");
            }
            sb.AppendLine();

            sb.AppendLine("### 🟢 Low Priority Tasks");
            var lowTasks = GetLowPriorityTasks(report);
            foreach (var task in lowTasks)
            {
                sb.AppendLine($"- [ ] {task}");
            }
            sb.AppendLine();
        }

        private string GetSeverityIcon(IssueSeverity severity)
        {
            return severity switch
            {
                IssueSeverity.Critical => "🔴",
                IssueSeverity.High => "🟠",
                IssueSeverity.Medium => "🟡",
                IssueSeverity.Low => "🟢",
                _ => "⚪"
            };
        }

        private string GetOverallComplianceStatus(AuditReport report)
        {
            var totalIssues = report.Issues.Count;
            var criticalIssues = report.Issues.Count(i => i.Severity == IssueSeverity.Critical);
            var highIssues = report.Issues.Count(i => i.Severity == IssueSeverity.High);

            if (criticalIssues > 0)
                return "🔴 **CRITICAL ISSUES PRESENT** - Immediate action required";
            if (highIssues > 5)
                return "🟠 **MULTIPLE HIGH PRIORITY ISSUES** - Should be addressed soon";
            if (totalIssues > 20)
                return "🟡 **GOOD WITH IMPROVEMENTS NEEDED** - Several items to address";
            if (totalIssues > 0)
                return "🟢 **MOSTLY COMPLIANT** - Minor improvements needed";
            
            return "✅ **FULLY COMPLIANT** - No issues found";
        }

        private (string status, int percentage, string[] items) GetPR1ComplianceStatus(AuditReport report)
        {
            var items = new[]
            {
                report.Statistics.ConsolidatedDTOFiles > 80 ? "✅ DTO project properly organized with 80+ DTO files" : "❌ DTO project organization incomplete",
                report.Statistics.DTODomainFolders >= 15 ? "✅ DTOs organized in domain folders" : "❌ DTO domain organization incomplete",
                report.Statistics.LegacyDTOReferences == 0 ? "✅ No legacy DTO references found" : $"❌ {report.Statistics.LegacyDTOReferences} legacy DTO references still exist",
                report.Statistics.InlineDTOs == 0 ? "✅ No inline DTOs in controllers" : $"❌ {report.Statistics.InlineDTOs} inline DTOs need to be moved"
            };

            var compliantItems = items.Count(i => i.StartsWith("✅"));
            var percentage = (compliantItems * 100) / items.Length;
            
            var status = percentage switch
            {
                100 => "✅ COMPLETE",
                >= 75 => "🟡 MOSTLY COMPLETE",
                >= 50 => "🟠 PARTIALLY COMPLETE",
                _ => "❌ NEEDS WORK"
            };

            return (status, percentage, items);
        }

        private (string status, int percentage, string[] items) GetPR2ComplianceStatus(AuditReport report)
        {
            var items = new[]
            {
                report.Statistics.NonAsyncTaskMethods == 0 ? "✅ All Task methods properly use async/await" : $"❌ {report.Statistics.NonAsyncTaskMethods} methods need async/await fixes",
                report.Statistics.RedundantStatusAssignments == 0 ? "✅ No redundant status assignments" : $"❌ {report.Statistics.RedundantStatusAssignments} redundant status assignments need cleanup",
                report.Statistics.SyncOverAsyncPatterns == 0 ? "✅ No sync-over-async anti-patterns" : $"❌ {report.Statistics.SyncOverAsyncPatterns} sync-over-async patterns need fixing",
                report.Statistics.MissingExceptionHandling < 5 ? "✅ Good exception handling coverage" : $"❌ {report.Statistics.MissingExceptionHandling} methods missing exception handling"
            };

            var compliantItems = items.Count(i => i.StartsWith("✅"));
            var percentage = (compliantItems * 100) / items.Length;
            
            var status = percentage switch
            {
                100 => "✅ COMPLETE",
                >= 75 => "🟡 MOSTLY COMPLETE", 
                >= 50 => "🟠 PARTIALLY COMPLETE",
                _ => "❌ NEEDS WORK"
            };

            return (status, percentage, items);
        }

        private (string status, int percentage, string[] items) GetPR3ComplianceStatus(AuditReport report)
        {
            var items = new[]
            {
                report.Statistics.ControllersNotInheritingBase == 0 ? "✅ All controllers inherit from BaseApiController" : $"❌ {report.Statistics.ControllersNotInheritingBase} controllers need BaseApiController inheritance",
                report.Statistics.DirectStatusCodeUsage == 0 ? "✅ No direct StatusCode usage" : $"❌ {report.Statistics.DirectStatusCodeUsage} instances of direct StatusCode usage",
                report.Statistics.UnversionedAPIRoutes == 0 ? "✅ All API routes properly versioned" : $"❌ {report.Statistics.UnversionedAPIRoutes} controllers with unversioned routes",
                report.Statistics.ControllersWithoutTenantValidation < 5 ? "✅ Good multi-tenant validation coverage" : $"❌ {report.Statistics.ControllersWithoutTenantValidation} controllers missing tenant validation",
                report.Statistics.NonRFC7807ErrorResponses == 0 ? "✅ All error responses RFC7807 compliant" : $"❌ {report.Statistics.NonRFC7807ErrorResponses} non-compliant error responses"
            };

            var compliantItems = items.Count(i => i.StartsWith("✅"));
            var percentage = (compliantItems * 100) / items.Length;
            
            var status = percentage switch
            {
                100 => "✅ COMPLETE",
                >= 80 => "🟡 MOSTLY COMPLETE",
                >= 60 => "🟠 PARTIALLY COMPLETE", 
                _ => "❌ NEEDS WORK"
            };

            return (status, percentage, items);
        }

        private string[] GetCriticalTasks(AuditReport report)
        {
            var tasks = new List<string>();
            
            var criticalIssues = report.Issues.Where(i => i.Severity == IssueSeverity.Critical).ToList();
            foreach (var issue in criticalIssues)
            {
                tasks.Add($"{issue.Description} in {issue.File}");
            }

            return tasks.ToArray();
        }

        private string[] GetHighPriorityTasks(AuditReport report)
        {
            var tasks = new List<string>();
            
            if (report.Statistics.LegacyDTOReferences > 0)
                tasks.Add($"Remove {report.Statistics.LegacyDTOReferences} legacy DTO namespace references");
            
            if (report.Statistics.ControllersNotInheritingBase > 0)
                tasks.Add($"Update {report.Statistics.ControllersNotInheritingBase} controllers to inherit from BaseApiController");
            
            if (report.Statistics.ControllersWithoutTenantValidation > 0)
                tasks.Add($"Add tenant validation to {report.Statistics.ControllersWithoutTenantValidation} business controllers");
            
            if (report.Statistics.SyncOverAsyncPatterns > 0)
                tasks.Add($"Fix {report.Statistics.SyncOverAsyncPatterns} sync-over-async anti-patterns");

            var highIssues = report.Issues.Where(i => i.Severity == IssueSeverity.High).ToList();
            foreach (var issue in highIssues)
            {
                if (!tasks.Any(t => t.Contains(issue.File)))
                    tasks.Add($"{issue.Description} in {issue.File}");
            }

            return tasks.ToArray();
        }

        private string[] GetMediumPriorityTasks(AuditReport report)
        {
            var tasks = new List<string>();
            
            if (report.Statistics.DirectStatusCodeUsage > 0)
                tasks.Add($"Replace {report.Statistics.DirectStatusCodeUsage} direct StatusCode usages with RFC7807 methods");
            
            if (report.Statistics.UnversionedAPIRoutes > 0)
                tasks.Add($"Update {report.Statistics.UnversionedAPIRoutes} controllers to use versioned API routes");
            
            if (report.Statistics.RedundantStatusAssignments > 0)
                tasks.Add($"Remove {report.Statistics.RedundantStatusAssignments} redundant status property assignments");
            
            if (report.Statistics.NonAsyncTaskMethods > 0)
                tasks.Add($"Fix {report.Statistics.NonAsyncTaskMethods} methods that return Task but are not async");

            return tasks.ToArray();
        }

        private string[] GetLowPriorityTasks(AuditReport report)
        {
            var tasks = new List<string>();
            
            if (report.Statistics.DTOsWithoutValidation > 0)
                tasks.Add($"Add validation attributes to {report.Statistics.DTOsWithoutValidation} DTOs");
            
            if (report.Statistics.ControllersWithoutSwaggerDocs > 0)
                tasks.Add($"Add Swagger documentation to {report.Statistics.ControllersWithoutSwaggerDocs} controllers");
            
            if (report.Statistics.MissingConfigureAwait > 0)
                tasks.Add($"Consider adding ConfigureAwait(false) to {report.Statistics.MissingConfigureAwait} await statements in library code");
            
            if (report.Statistics.MissingExceptionHandling > 0)
                tasks.Add($"Add exception handling to {report.Statistics.MissingExceptionHandling} service methods");

            return tasks.ToArray();
        }
    }
}