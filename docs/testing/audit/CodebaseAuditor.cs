using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EventForge.Audit
{
    /// <summary>
    /// Automated audit tool to verify PR1 (DTO consolidation), PR2 (CRUD/Services refactoring), and PR3 (Controllers/API refactoring) compliance
    /// </summary>
    public class CodebaseAuditor
    {
        private readonly string _projectRoot;
        private readonly List<AuditIssue> _issues = new();
        private readonly AuditStatistics _stats = new();

        public CodebaseAuditor(string projectRoot)
        {
            _projectRoot = projectRoot ?? throw new ArgumentNullException(nameof(projectRoot));
        }

        public AuditReport RunFullAudit()
        {
            Console.WriteLine("Starting EventForge Codebase Audit...");
            
            // PR1: DTO Consolidation Audit
            AuditDTOConsolidation();
            
            // PR2: CRUD/Services Refactoring Audit
            AuditServicesRefactoring();
            
            // PR3: Controllers/API Refactoring Audit
            AuditControllersRefactoring();
            
            // Additional checks
            AuditAsyncAwaitPatterns();
            AuditValidationPatterns();
            AuditRFC7807Compliance();
            
            return new AuditReport
            {
                Issues = _issues,
                Statistics = _stats,
                GeneratedAt = DateTime.UtcNow
            };
        }

        private void AuditDTOConsolidation()
        {
            Console.WriteLine("Auditing DTO Consolidation (PR1)...");
            
            // Check for legacy DTO references in EventForge.Server
            var serverFiles = Directory.GetFiles(Path.Combine(_projectRoot, "EventForge.Server"), "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in serverFiles)
            {
                var content = File.ReadAllText(file);
                
                // Check for legacy DTO namespace references
                if (content.Contains("EventForge.Server.DTOs"))
                {
                    _issues.Add(new AuditIssue
                    {
                        Category = "DTO Consolidation",
                        Severity = IssueSeverity.High,
                        File = GetRelativePath(file),
                        Description = "Legacy DTO namespace reference found",
                        Details = "File contains reference to 'EventForge.Server.DTOs' which should be 'EventForge.DTOs'"
                    });
                    _stats.LegacyDTOReferences++;
                }
                
                // Check for inline DTO definitions in controllers
                if (file.Contains("Controller") && content.Contains("public class") && content.Contains("Dto"))
                {
                    var dtoMatches = Regex.Matches(content, @"public class \w*Dto\b");
                    if (dtoMatches.Count > 0)
                    {
                        _issues.Add(new AuditIssue
                        {
                            Category = "DTO Consolidation",
                            Severity = IssueSeverity.Medium,
                            File = GetRelativePath(file),
                            Description = "Inline DTO definition found in controller",
                            Details = $"Found {dtoMatches.Count} DTO class(es) defined inline. Should be moved to EventForge.DTOs project"
                        });
                        _stats.InlineDTOs += dtoMatches.Count;
                    }
                }
            }
            
            // Check DTO project organization
            var dtoProjectPath = Path.Combine(_projectRoot, "EventForge.DTOs");
            if (Directory.Exists(dtoProjectPath))
            {
                var dtoFiles = Directory.GetFiles(dtoProjectPath, "*.cs", SearchOption.AllDirectories);
                _stats.ConsolidatedDTOFiles = dtoFiles.Length;
                
                // Check for proper organization
                var domainFolders = Directory.GetDirectories(dtoProjectPath).Where(d => !d.Contains("bin") && !d.Contains("obj")).ToArray();
                _stats.DTODomainFolders = domainFolders.Length;
            }
        }

        private void AuditServicesRefactoring()
        {
            Console.WriteLine("Auditing Services Refactoring (PR2)...");
            
            var serviceFiles = Directory.GetFiles(Path.Combine(_projectRoot, "EventForge.Server", "Services"), "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in serviceFiles)
            {
                var content = File.ReadAllText(file);
                
                // Check for async/await patterns
                if (content.Contains("public") && content.Contains("Task") && !content.Contains("async"))
                {
                    var methodMatches = Regex.Matches(content, @"public.*Task.*\w+\(.*\)\s*{");
                    foreach (Match match in methodMatches)
                    {
                        if (!match.Value.Contains("async"))
                        {
                            _issues.Add(new AuditIssue
                            {
                                Category = "Services Refactoring",
                                Severity = IssueSeverity.Medium,
                                File = GetRelativePath(file),
                                Description = "Non-async method returning Task found",
                                Details = $"Method signature: {match.Value.Trim()}"
                            });
                            _stats.NonAsyncTaskMethods++;
                        }
                    }
                }
                
                // Check for redundant status properties usage
                if (content.Contains("Status") && (content.Contains("Active") || content.Contains("Deleted")))
                {
                    var statusMatches = Regex.Matches(content, @"\w*Status\s*=\s*\w*Status\.\w+");
                    if (statusMatches.Count > 0)
                    {
                        _issues.Add(new AuditIssue
                        {
                            Category = "Services Refactoring",
                            Severity = IssueSeverity.Medium,
                            File = GetRelativePath(file),
                            Description = "Redundant status property assignment found",
                            Details = "Should use IsDeleted/IsActive from AuditableEntity instead of custom Status enums"
                        });
                        _stats.RedundantStatusAssignments += statusMatches.Count;
                    }
                }
                
                // Check for proper exception handling
                if (!content.Contains("try") && content.Contains("await") && content.Contains("public"))
                {
                    _issues.Add(new AuditIssue
                    {
                        Category = "Services Refactoring",
                        Severity = IssueSeverity.Low,
                        File = GetRelativePath(file),
                        Description = "Service method without try-catch block",
                        Details = "Async service methods should have proper exception handling"
                    });
                    _stats.MissingExceptionHandling++;
                }
            }
        }

        private void AuditControllersRefactoring()
        {
            Console.WriteLine("Auditing Controllers Refactoring (PR3)...");
            
            var controllerFiles = Directory.GetFiles(Path.Combine(_projectRoot, "EventForge.Server", "Controllers"), "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in controllerFiles)
            {
                var content = File.ReadAllText(file);
                var fileName = Path.GetFileName(file);
                
                // Check for BaseApiController inheritance
                if (!content.Contains(": BaseApiController") && fileName != "BaseApiController.cs")
                {
                    _issues.Add(new AuditIssue
                    {
                        Category = "Controllers Refactoring",
                        Severity = IssueSeverity.High,
                        File = GetRelativePath(file),
                        Description = "Controller not inheriting from BaseApiController",
                        Details = "All controllers should inherit from BaseApiController for RFC7807 compliance"
                    });
                    _stats.ControllersNotInheritingBase++;
                }
                
                // Check for RFC7807 error handling
                if (content.Contains("StatusCode(") && !content.Contains("CreateValidationProblemDetails") && !content.Contains("CreateNotFoundProblem"))
                {
                    var statusCodeMatches = Regex.Matches(content, @"StatusCode\(\d+");
                    if (statusCodeMatches.Count > 0)
                    {
                        _issues.Add(new AuditIssue
                        {
                            Category = "Controllers Refactoring",
                            Severity = IssueSeverity.Medium,
                            File = GetRelativePath(file),
                            Description = "Direct StatusCode usage instead of RFC7807 methods",
                            Details = "Should use RFC7807 compliant methods from BaseApiController"
                        });
                        _stats.DirectStatusCodeUsage += statusCodeMatches.Count;
                    }
                }
                
                // Check for API route versioning
                if (!content.Contains("api/v1/") && content.Contains("[Route("))
                {
                    _issues.Add(new AuditIssue
                    {
                        Category = "Controllers Refactoring",
                        Severity = IssueSeverity.Medium,
                        File = GetRelativePath(file),
                        Description = "Controller not using versioned API routes",
                        Details = "Should use 'api/v1/[controller]' pattern for consistency"
                    });
                    _stats.UnversionedAPIRoutes++;
                }
                
                // Check for multi-tenant validation (for business controllers)
                if (fileName.Contains("Controller") && !fileName.Contains("Base") && !fileName.Contains("Auth") && !fileName.Contains("Health"))
                {
                    if (!content.Contains("ValidateTenantAccessAsync") && !content.Contains("ITenantContext"))
                    {
                        _issues.Add(new AuditIssue
                        {
                            Category = "Controllers Refactoring",
                            Severity = IssueSeverity.High,
                            File = GetRelativePath(file),
                            Description = "Business controller missing multi-tenant validation",
                            Details = "Business controllers should implement tenant access validation"
                        });
                        _stats.ControllersWithoutTenantValidation++;
                    }
                }
                
                // Check for Swagger documentation
                if (!content.Contains("ProducesResponseType") && content.Contains("[Http"))
                {
                    _issues.Add(new AuditIssue
                    {
                        Category = "Controllers Refactoring",
                        Severity = IssueSeverity.Low,
                        File = GetRelativePath(file),
                        Description = "Controller endpoints missing Swagger documentation",
                        Details = "Should include [ProducesResponseType] attributes for proper API documentation"
                    });
                    _stats.ControllersWithoutSwaggerDocs++;
                }
            }
        }

        private void AuditAsyncAwaitPatterns()
        {
            Console.WriteLine("Auditing Async/Await Patterns...");
            
            var allCsFiles = Directory.GetFiles(Path.Combine(_projectRoot, "EventForge.Server"), "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in allCsFiles)
            {
                var content = File.ReadAllText(file);
                
                // Check for sync over async anti-pattern
                if (content.Contains(".Result") || content.Contains(".Wait()"))
                {
                    _issues.Add(new AuditIssue
                    {
                        Category = "Async Patterns",
                        Severity = IssueSeverity.High,
                        File = GetRelativePath(file),
                        Description = "Sync over async anti-pattern detected",
                        Details = "Usage of .Result or .Wait() found - should use await instead"
                    });
                    _stats.SyncOverAsyncPatterns++;
                }
                
                // Check for missing ConfigureAwait(false) in library code
                if (content.Contains("await") && !content.Contains("ConfigureAwait") && !file.Contains("Controller"))
                {
                    var awaitMatches = Regex.Matches(content, @"await\s+\w+.*\(.*\)");
                    if (awaitMatches.Count > 0)
                    {
                        _issues.Add(new AuditIssue
                        {
                            Category = "Async Patterns",
                            Severity = IssueSeverity.Low,
                            File = GetRelativePath(file),
                            Description = "Missing ConfigureAwait(false) in library code",
                            Details = "Consider using ConfigureAwait(false) for better performance in library code"
                        });
                        _stats.MissingConfigureAwait++;
                    }
                }
            }
        }

        private void AuditValidationPatterns()
        {
            Console.WriteLine("Auditing Validation Patterns...");
            
            var dtoFiles = Directory.GetFiles(Path.Combine(_projectRoot, "EventForge.DTOs"), "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in dtoFiles)
            {
                var content = File.ReadAllText(file);
                
                // Check for DTOs without validation attributes
                if (content.Contains("public string") && !content.Contains("[Required]") && !content.Contains("[MaxLength]"))
                {
                    _issues.Add(new AuditIssue
                    {
                        Category = "Validation Patterns",
                        Severity = IssueSeverity.Low,
                        File = GetRelativePath(file),
                        Description = "DTO properties without validation attributes",
                        Details = "Consider adding [Required], [MaxLength], or other validation attributes"
                    });
                    _stats.DTOsWithoutValidation++;
                }
            }
        }

        private void AuditRFC7807Compliance()
        {
            Console.WriteLine("Auditing RFC7807 Compliance...");
            
            var controllerFiles = Directory.GetFiles(Path.Combine(_projectRoot, "EventForge.Server", "Controllers"), "*.cs", SearchOption.AllDirectories);
            
            foreach (var file in controllerFiles)
            {
                var content = File.ReadAllText(file);
                
                // Check for proper error response types
                if (content.Contains("BadRequest(") && !content.Contains("CreateValidationProblemDetails"))
                {
                    _issues.Add(new AuditIssue
                    {
                        Category = "RFC7807 Compliance",
                        Severity = IssueSeverity.Medium,
                        File = GetRelativePath(file),
                        Description = "Non-RFC7807 compliant error response",
                        Details = "Should use RFC7807 compliant error methods from BaseApiController"
                    });
                    _stats.NonRFC7807ErrorResponses++;
                }
            }
        }

        private string GetRelativePath(string fullPath)
        {
            return Path.GetRelativePath(_projectRoot, fullPath);
        }
    }

    public class AuditReport
    {
        public List<AuditIssue> Issues { get; set; } = new();
        public AuditStatistics Statistics { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class AuditIssue
    {
        public string Category { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; }
        public string File { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public enum IssueSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class AuditStatistics
    {
        // DTO Consolidation Stats
        public int LegacyDTOReferences { get; set; }
        public int InlineDTOs { get; set; }
        public int ConsolidatedDTOFiles { get; set; }
        public int DTODomainFolders { get; set; }

        // Services Refactoring Stats
        public int NonAsyncTaskMethods { get; set; }
        public int RedundantStatusAssignments { get; set; }
        public int MissingExceptionHandling { get; set; }

        // Controllers Refactoring Stats
        public int ControllersNotInheritingBase { get; set; }
        public int DirectStatusCodeUsage { get; set; }
        public int UnversionedAPIRoutes { get; set; }
        public int ControllersWithoutTenantValidation { get; set; }
        public int ControllersWithoutSwaggerDocs { get; set; }

        // Async Patterns Stats
        public int SyncOverAsyncPatterns { get; set; }
        public int MissingConfigureAwait { get; set; }

        // Validation Stats
        public int DTOsWithoutValidation { get; set; }

        // RFC7807 Stats
        public int NonRFC7807ErrorResponses { get; set; }
    }
}