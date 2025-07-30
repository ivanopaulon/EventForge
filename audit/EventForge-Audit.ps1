# EventForge Audit PowerShell Script
# This script provides a PowerShell-based audit tool for EventForge backend refactoring verification

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectRoot = $PSScriptRoot,
    [Parameter(Mandatory=$false)]
    [switch]$Detailed = $false
)

Write-Host "EventForge Backend Refactoring Audit (PowerShell)" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""

# Adjust project root if we're in the audit directory
if ((Split-Path $ProjectRoot -Leaf) -eq "audit") {
    $ProjectRoot = Split-Path $ProjectRoot -Parent
}

Write-Host "Project Root: $ProjectRoot" -ForegroundColor Yellow
Write-Host ""

# Verify project structure
$serverPath = Join-Path $ProjectRoot "EventForge.Server"
$dtosPath = Join-Path $ProjectRoot "EventForge.DTOs"

if (-not (Test-Path $serverPath)) {
    Write-Error "EventForge.Server directory not found at $serverPath"
    exit 1
}

if (-not (Test-Path $dtosPath)) {
    Write-Error "EventForge.DTOs directory not found at $dtosPath"
    exit 1
}

Write-Host "✅ Project structure verified" -ForegroundColor Green
Write-Host ""

# Initialize results
$auditResults = @{
    TotalIssues = 0
    CriticalIssues = 0
    HighIssues = 0
    MediumIssues = 0
    LowIssues = 0
    Issues = @()
}

function Add-AuditIssue {
    param(
        [string]$Category,
        [string]$Severity,
        [string]$File,
        [string]$Description,
        [string]$Details
    )
    
    $issue = @{
        Category = $Category
        Severity = $Severity
        File = $File
        Description = $Description
        Details = $Details
    }
    
    $auditResults.Issues += $issue
    $auditResults.TotalIssues++
    
    switch ($Severity) {
        "Critical" { $auditResults.CriticalIssues++ }
        "High" { $auditResults.HighIssues++ }
        "Medium" { $auditResults.MediumIssues++ }
        "Low" { $auditResults.LowIssues++ }
    }
}

# PR1: DTO Consolidation Audit
Write-Host "🔍 Auditing DTO Consolidation (PR1)..." -ForegroundColor Cyan

# Check for legacy DTO references
$serverFiles = Get-ChildItem -Path $serverPath -Filter "*.cs" -Recurse
$legacyDTOReferences = 0

foreach ($file in $serverFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "EventForge\.Server\.DTOs") {
        Add-AuditIssue -Category "DTO Consolidation" -Severity "High" -File $file.FullName.Replace($ProjectRoot, "") -Description "Legacy DTO namespace reference found" -Details "File contains reference to 'EventForge.Server.DTOs' which should be 'EventForge.DTOs'"
        $legacyDTOReferences++
    }
}

# Count DTO files and folders
$dtoFiles = Get-ChildItem -Path $dtosPath -Filter "*.cs" -Recurse | Where-Object { $_.Name -ne "GlobalUsings.cs" }
$dtoDomains = Get-ChildItem -Path $dtosPath -Directory | Where-Object { $_.Name -notmatch "bin|obj" }

Write-Host "  ✅ DTO Files: $($dtoFiles.Count)" -ForegroundColor Green
Write-Host "  ✅ Domain Folders: $($dtoDomains.Count)" -ForegroundColor Green
Write-Host "  ❌ Legacy References: $legacyDTOReferences" -ForegroundColor $(if ($legacyDTOReferences -eq 0) { "Green" } else { "Red" })

# PR2: Services Refactoring Audit
Write-Host ""
Write-Host "🔍 Auditing Services Refactoring (PR2)..." -ForegroundColor Cyan

$serviceFiles = Get-ChildItem -Path (Join-Path $serverPath "Services") -Filter "*.cs" -Recurse
$syncOverAsyncIssues = 0
$missingConfigureAwait = 0

foreach ($file in $serviceFiles) {
    $content = Get-Content $file.FullName -Raw
    
    # Check for sync over async patterns
    if ($content -match "\.Result|\.Wait\(\)") {
        Add-AuditIssue -Category "Services Refactoring" -Severity "High" -File $file.FullName.Replace($ProjectRoot, "") -Description "Sync over async anti-pattern detected" -Details "Usage of .Result or .Wait() found - should use await instead"
        $syncOverAsyncIssues++
    }
    
    # Check for missing ConfigureAwait
    $awaitMatches = [regex]::Matches($content, "await\s+\w+.*\(.*\)")
    $configureAwaitMatches = [regex]::Matches($content, "ConfigureAwait")
    
    if ($awaitMatches.Count -gt 0 -and $configureAwaitMatches.Count -eq 0) {
        Add-AuditIssue -Category "Services Refactoring" -Severity "Low" -File $file.FullName.Replace($ProjectRoot, "") -Description "Missing ConfigureAwait(false) in library code" -Details "Consider using ConfigureAwait(false) for better performance"
        $missingConfigureAwait++
    }
}

Write-Host "  ❌ Sync-over-Async Issues: $syncOverAsyncIssues" -ForegroundColor $(if ($syncOverAsyncIssues -eq 0) { "Green" } else { "Red" })
Write-Host "  ⚠️ Missing ConfigureAwait: $missingConfigureAwait" -ForegroundColor Yellow

# PR3: Controllers Refactoring Audit
Write-Host ""
Write-Host "🔍 Auditing Controllers Refactoring (PR3)..." -ForegroundColor Cyan

$controllerFiles = Get-ChildItem -Path (Join-Path $serverPath "Controllers") -Filter "*.cs" -Recurse
$controllersNotInheritingBase = 0
$directStatusCodeUsage = 0
$unversionedRoutes = 0
$missingTenantValidation = 0

foreach ($file in $controllerFiles) {
    $content = Get-Content $file.FullName -Raw
    $fileName = $file.Name
    
    # Check BaseApiController inheritance
    if (-not $content.Contains(": BaseApiController") -and $fileName -ne "BaseApiController.cs") {
        Add-AuditIssue -Category "Controllers Refactoring" -Severity "High" -File $file.FullName.Replace($ProjectRoot, "") -Description "Controller not inheriting from BaseApiController" -Details "All controllers should inherit from BaseApiController for RFC7807 compliance"
        $controllersNotInheritingBase++
    }
    
    # Check for direct StatusCode usage
    $statusCodeMatches = [regex]::Matches($content, "StatusCode\(\d+")
    if ($statusCodeMatches.Count -gt 0 -and -not $content.Contains("CreateValidationProblemDetails")) {
        Add-AuditIssue -Category "Controllers Refactoring" -Severity "Medium" -File $file.FullName.Replace($ProjectRoot, "") -Description "Direct StatusCode usage instead of RFC7807 methods" -Details "Should use RFC7807 compliant methods from BaseApiController"
        $directStatusCodeUsage += $statusCodeMatches.Count
    }
    
    # Check for versioned routes
    if (-not $content.Contains("api/v1/") -and $content.Contains("[Route(")) {
        Add-AuditIssue -Category "Controllers Refactoring" -Severity "Medium" -File $file.FullName.Replace($ProjectRoot, "") -Description "Controller not using versioned API routes" -Details "Should use 'api/v1/[controller]' pattern for consistency"
        $unversionedRoutes++
    }
    
    # Check for tenant validation (business controllers)
    if ($fileName.Contains("Controller") -and 
        -not $fileName.Contains("Base") -and 
        -not $fileName.Contains("Auth") -and 
        -not $fileName.Contains("Health") -and
        -not $content.Contains("ValidateTenantAccessAsync")) {
        
        Add-AuditIssue -Category "Controllers Refactoring" -Severity "Medium" -File $file.FullName.Replace($ProjectRoot, "") -Description "Business controller missing multi-tenant validation" -Details "Business controllers should implement tenant access validation"
        $missingTenantValidation++
    }
}

Write-Host "  ❌ Controllers Not Inheriting Base: $controllersNotInheritingBase" -ForegroundColor $(if ($controllersNotInheritingBase -eq 0) { "Green" } else { "Red" })
Write-Host "  ❌ Direct StatusCode Usage: $directStatusCodeUsage" -ForegroundColor $(if ($directStatusCodeUsage -eq 0) { "Green" } else { "Red" })
Write-Host "  ❌ Unversioned Routes: $unversionedRoutes" -ForegroundColor $(if ($unversionedRoutes -eq 0) { "Green" } else { "Red" })
Write-Host "  ❌ Missing Tenant Validation: $missingTenantValidation" -ForegroundColor $(if ($missingTenantValidation -eq 0) { "Green" } else { "Red" })

# Summary
Write-Host ""
Write-Host "📊 Audit Summary" -ForegroundColor Green
Write-Host "================" -ForegroundColor Green
Write-Host "Total Issues Found: $($auditResults.TotalIssues)" -ForegroundColor White
Write-Host "🔴 Critical: $($auditResults.CriticalIssues)" -ForegroundColor Red
Write-Host "🟠 High: $($auditResults.HighIssues)" -ForegroundColor DarkYellow
Write-Host "🟡 Medium: $($auditResults.MediumIssues)" -ForegroundColor Yellow
Write-Host "🟢 Low: $($auditResults.LowIssues)" -ForegroundColor Green
Write-Host ""

# PR Compliance Status
Write-Host "📋 PR Compliance Status" -ForegroundColor Green
Write-Host "========================" -ForegroundColor Green

# PR1 Status
$pr1Score = if ($legacyDTOReferences -eq 0 -and $dtoFiles.Count -gt 80) { 100 } elseif ($legacyDTOReferences -eq 0) { 90 } else { 75 }
$pr1Status = if ($pr1Score -eq 100) { "✅ COMPLETE" } elseif ($pr1Score -ge 90) { "🟡 MOSTLY COMPLETE" } else { "🟠 NEEDS WORK" }
Write-Host "PR1 (DTO Consolidation): $pr1Status ($pr1Score%)" -ForegroundColor $(if ($pr1Score -eq 100) { "Green" } elseif ($pr1Score -ge 90) { "Yellow" } else { "DarkYellow" })

# PR2 Status
$pr2Score = if ($syncOverAsyncIssues -eq 0) { 90 } else { 75 }
$pr2Status = if ($pr2Score -ge 90) { "🟡 MOSTLY COMPLETE" } else { "🟠 NEEDS WORK" }
Write-Host "PR2 (Services Refactoring): $pr2Status ($pr2Score%)" -ForegroundColor $(if ($pr2Score -ge 90) { "Yellow" } else { "DarkYellow" })

# PR3 Status
$pr3Issues = $controllersNotInheritingBase + $directStatusCodeUsage + $unversionedRoutes
$pr3Score = if ($pr3Issues -eq 0) { 100 } elseif ($pr3Issues -le 5) { 85 } elseif ($pr3Issues -le 20) { 70 } else { 50 }
$pr3Status = if ($pr3Score -eq 100) { "✅ COMPLETE" } elseif ($pr3Score -ge 85) { "🟡 MOSTLY COMPLETE" } else { "🟠 NEEDS WORK" }
Write-Host "PR3 (Controllers Refactoring): $pr3Status ($pr3Score%)" -ForegroundColor $(if ($pr3Score -eq 100) { "Green" } elseif ($pr3Score -ge 85) { "Yellow" } else { "DarkYellow" })

Write-Host ""

# Recommendations
Write-Host "💡 Immediate Recommendations" -ForegroundColor Green
Write-Host "============================" -ForegroundColor Green

if ($auditResults.HighIssues -gt 0 -or $auditResults.CriticalIssues -gt 0) {
    Write-Host "1. Address all Critical and High priority issues first" -ForegroundColor Red
}

if ($syncOverAsyncIssues -gt 0) {
    Write-Host "2. Fix sync-over-async patterns (potential deadlock risk)" -ForegroundColor Red
}

if ($directStatusCodeUsage -gt 0) {
    Write-Host "3. Replace direct StatusCode usage with RFC7807 methods" -ForegroundColor Yellow
}

if ($missingTenantValidation -gt 0) {
    Write-Host "4. Review and add tenant validation where appropriate" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "📄 For detailed analysis, run the C# audit tool or check AUDIT_REPORT.md" -ForegroundColor Cyan
Write-Host "🔧 For Swagger diagnosis, check SWAGGER_DIAGNOSTIC.md" -ForegroundColor Cyan
Write-Host "📋 For manual verification steps, check MANUAL_VERIFICATION_CHECKLIST.md" -ForegroundColor Cyan

# Detailed output if requested
if ($Detailed) {
    Write-Host ""
    Write-Host "📋 Detailed Issues List" -ForegroundColor Green
    Write-Host "======================" -ForegroundColor Green
    
    $groupedIssues = $auditResults.Issues | Group-Object Category
    
    foreach ($group in $groupedIssues) {
        Write-Host ""
        Write-Host "Category: $($group.Name)" -ForegroundColor Cyan
        Write-Host "$(('-' * ($group.Name.Length + 10)))" -ForegroundColor Cyan
        
        foreach ($issue in $group.Group) {
            $severityColor = switch ($issue.Severity) {
                "Critical" { "Red" }
                "High" { "DarkRed" }
                "Medium" { "Yellow" }
                "Low" { "Green" }
                default { "White" }
            }
            
            Write-Host "  $($issue.Severity): $($issue.Description)" -ForegroundColor $severityColor
            Write-Host "    File: $($issue.File)" -ForegroundColor Gray
            Write-Host "    Details: $($issue.Details)" -ForegroundColor Gray
            Write-Host ""
        }
    }
}

Write-Host ""
Write-Host "✅ PowerShell audit completed!" -ForegroundColor Green