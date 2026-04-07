#Requires -Version 5.1
<#
.SYNOPSIS
    Builds a Prym update package (zip) ready for upload to the Hub.

.DESCRIPTION
    This script:
      1. Reads the component version via Nerdbank.GitVersioning (nbgv)
      2. Runs dotnet publish (unless -SkipPublish or -PublishDir is specified)
      3. Collects pending SQL migration scripts from Migrations/Pending/
      4. Assembles the canonical package zip structure:
           binaries/        <- dotnet publish output
           migrations/pre/  <- SQL scripts run BEFORE deploy
           migrations/post/ <- SQL scripts run AFTER service restart
           rollback/        <- SQL scripts run on rollback
           manifest.json    <- version, checksum, script list
      5. Calculates SHA-256 of the final zip
      6. Optionally uploads the package to the Hub via HTTP
      7. Archives the consumed pending scripts to Migrations/Applied/<component>-<version>/

.PARAMETER Component
    Which component to package. Must be 'Server' or 'Client'.

.PARAMETER Configuration
    Build configuration. Defaults to 'Release'.

.PARAMETER OutDir
    Directory where the final .zip is saved. Defaults to './artifacts'.
    To have the Hub auto-ingest the package, point this to the Hub's
    IncomingPackagesPath (e.g. a network share or \\hubserver\packages\incoming).

.PARAMETER IncomingPath
    Shorthand for pointing OutDir directly at the Hub's IncomingPackagesPath.
    When specified, it overrides -OutDir.
    Example: -IncomingPath \\\\hubserver\\packages\\incoming

.PARAMETER SkipPublish
    If set, skips dotnet publish and uses -PublishDir as the binaries source.

.PARAMETER PublishDir
    Path to an existing publish folder. Used with -SkipPublish or as explicit override.

.PARAMETER HubUrl
    Base URL of the Hub (e.g. https://updatehub.company.com).
    If specified, the package is uploaded automatically after building.

.PARAMETER AdminKey
    Admin API key for the Hub. Required when -HubUrl is specified.

.PARAMETER ReleaseNotes
    Optional release notes string embedded in manifest.json.

.PARAMETER PreserveFiles
    List of file names (relative to binaries root) that the Agent must NOT
    overwrite if they already exist on the target machine.
    Defaults to: appsettings.json, appsettings.Production.json

.PARAMETER RuntimeIdentifier
    RID passed to dotnet publish. Defaults to 'win-x64'.

.EXAMPLE
    # Build Server package and upload to Hub
    .\New-UpdatePackage.ps1 -Component Server -HubUrl https://hub.mycompany.com -AdminKey abc123

.EXAMPLE
    # Build Client package, save zip locally only
    .\New-UpdatePackage.ps1 -Component Client -OutDir C:\Releases

.EXAMPLE
    # Use an existing publish folder (e.g. from Visual Studio Publish)
    .\New-UpdatePackage.ps1 -Component Server -SkipPublish -PublishDir "C:\MyPublish\Server"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("Server", "Client")]
    [string] $Component,

    [string] $Configuration   = "Release",
    [string] $OutDir          = "./artifacts",
    [string] $IncomingPath    = "",
    [switch] $SkipPublish,
    [string] $PublishDir      = "",
    [string] $HubUrl          = "",
    [string] $AdminKey        = "",
    [string] $ReleaseNotes    = "",
    [string[]] $PreserveFiles = @("appsettings.json", "appsettings.Production.json"),
    [string] $RuntimeIdentifier = "win-x64"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ──────────────────────────────────────────────────────────────────────────────
# Helpers
# ──────────────────────────────────────────────────────────────────────────────
function Write-Step { param($msg) Write-Host "`n== $msg ==" -ForegroundColor Cyan }
function Write-OK   { param($msg) Write-Host "  [OK]   $msg" -ForegroundColor Green }
function Write-WARN { param($msg) Write-Host "  [WARN] $msg" -ForegroundColor Yellow }
function Write-INFO { param($msg) Write-Host "  [    ] $msg" -ForegroundColor White }
function Write-FAIL { param($msg) Write-Host "  [FAIL] $msg" -ForegroundColor Red; throw $msg }

# ──────────────────────────────────────────────────────────────────────────────
# Resolve repo root (script lives in <repo>/scripts/)
# ──────────────────────────────────────────────────────────────────────────────
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RepoRoot  = (Resolve-Path (Join-Path $ScriptDir "..")).Path

# If IncomingPath is specified it takes priority over OutDir
if (![string]::IsNullOrWhiteSpace($IncomingPath)) {
    $OutDir = $IncomingPath
    Write-INFO "IncomingPath specificato: il pacchetto sara' consegnato direttamente all'Hub watcher."
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Magenta
Write-Host "  Prym Package Builder - Component: $Component" -ForegroundColor Magenta
Write-Host "  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Magenta
Write-Host "================================================================" -ForegroundColor Magenta

# ──────────────────────────────────────────────────────────────────────────────
# STEP 1 — Read version via nbgv
# ──────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 1 - Lettura versione (nbgv)"

$ProjectDir = Join-Path $RepoRoot "Prym.$Component"
if (!(Test-Path $ProjectDir)) {
    Write-FAIL "Cartella progetto non trovata: $ProjectDir"
}

$nbgvAvailable = $null -ne (Get-Command nbgv -ErrorAction SilentlyContinue)
$Version = $null

if ($nbgvAvailable) {
    try {
        $nbgvJson = & nbgv get-version -p $ProjectDir --format json 2>$null | ConvertFrom-Json
        $Version  = $nbgvJson.SimpleVersion
        $GitCommit = $nbgvJson.GitCommitId.Substring(0, [Math]::Min(8, $nbgvJson.GitCommitId.Length))
        Write-OK "Versione rilevata da nbgv: $Version (commit: $GitCommit)"
    } catch {
        Write-WARN "nbgv fallito: $_"
    }
}

# Fallback: try dotnet build to get InformationalVersion
if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-WARN "nbgv non disponibile o fallito. Tentativo via dotnet build..."
    $csprojFile = Get-ChildItem $ProjectDir -Filter "*.csproj" | Select-Object -First 1
    if ($csprojFile) {
        try {
            $buildOutput = & dotnet build $csprojFile.FullName -c $Configuration /p:GeneratePackageOnBuild=false --verbosity minimal 2>&1
            $versionLine = $buildOutput | Where-Object { $_ -match "InformationalVersion|AssemblyVersion" } | Select-Object -First 1
            if ($versionLine -match '(\d+\.\d+\.\d+)') { $Version = $Matches[1] }
        } catch { }
    }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    Write-FAIL "Impossibile determinare la versione. Installa nbgv: dotnet tool install -g Nerdbank.GitVersioning"
}

$GitCommit = if ($null -ne $GitCommit) { $GitCommit } else { (& git -C $RepoRoot rev-parse --short HEAD 2>$null) }
Write-OK "Versione: $Version | Commit: $GitCommit"

# ──────────────────────────────────────────────────────────────────────────────
# STEP 2 — dotnet publish
# ──────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 2 - dotnet publish"

$TempRoot   = Join-Path ([System.IO.Path]::GetTempPath()) "ef-pkg-build-$(New-Guid)"
$BinDir     = Join-Path $TempRoot "binaries"

if ($SkipPublish -and ![string]::IsNullOrWhiteSpace($PublishDir)) {
    if (!(Test-Path $PublishDir)) { Write-FAIL "PublishDir non trovata: $PublishDir" }
    Write-INFO "SkipPublish attivo: uso cartella esistente $PublishDir"
    $BinDir = $PublishDir
    Write-OK "Binaries da: $BinDir"
} else {
    $CsprojPath = Get-ChildItem $ProjectDir -Filter "*.csproj" | Select-Object -First 1
    if ($null -eq $CsprojPath) { Write-FAIL "Nessun .csproj trovato in $ProjectDir" }

    New-Item -ItemType Directory -Force -Path $BinDir | Out-Null

    $publishArgs = @(
        "publish", $CsprojPath.FullName,
        "-c", $Configuration,
        "-o", $BinDir,
        "--verbosity", "minimal"
    )

    # Server is win-x64 framework-dependent; Client (WASM) has no RID
    if ($Component -eq "Server") {
        $publishArgs += "-r", $RuntimeIdentifier, "--no-self-contained"
    }

    Write-INFO "Esecuzione: dotnet $($publishArgs -join ' ')"
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { Write-FAIL "dotnet publish fallito (exit $LASTEXITCODE)" }
    Write-OK "Publish completato in: $BinDir"
}

# ──────────────────────────────────────────────────────────────────────────────
# STEP 3 — Raccoglie migration scripts pending
# ──────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 3 - Raccolta migration scripts"

$PendingRoot  = Join-Path $RepoRoot "Migrations" "Pending"
$PreDir       = Join-Path $PendingRoot "pre"
$PostDir      = Join-Path $PendingRoot "post"
$RollbackDir  = Join-Path $PendingRoot "rollback"

function Get-SqlFiles { param($dir)
    if (Test-Path $dir) {
        Get-ChildItem $dir -Filter "*.sql" | Sort-Object Name
    } else {
        @()
    }
}

$PreScripts      = @(Get-SqlFiles $PreDir)
$PostScripts     = @(Get-SqlFiles $PostDir)
$RollbackScripts = @(Get-SqlFiles $RollbackDir)

Write-INFO "Pre-migration scripts  : $($PreScripts.Count)"
Write-INFO "Post-migration scripts : $($PostScripts.Count)"
Write-INFO "Rollback scripts       : $($RollbackScripts.Count)"

# ──────────────────────────────────────────────────────────────────────────────
# STEP 4 — Assembla struttura zip in temp
# ──────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 4 - Assemblaggio struttura pacchetto"

$PkgRoot = Join-Path $TempRoot "package"
New-Item -ItemType Directory -Force -Path $PkgRoot | Out-Null

# binaries/
$PkgBinDir = Join-Path $PkgRoot "binaries"
Write-INFO "Copia binaries..."
Copy-Item -Path $BinDir -Destination $PkgBinDir -Recurse -Force

# migrations/pre/ and post/
function Copy-Scripts {
    param($files, $destDir, $label)
    if ($files.Count -gt 0) {
        New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        foreach ($f in $files) {
            Copy-Item $f.FullName $destDir
            Write-INFO "  $label : $($f.Name)"
        }
    }
}

Copy-Scripts $PreScripts      (Join-Path $PkgRoot "migrations" "pre")  "pre"
Copy-Scripts $PostScripts     (Join-Path $PkgRoot "migrations" "post") "post"
Copy-Scripts $RollbackScripts (Join-Path $PkgRoot "rollback")          "rollback"

# Build relative script path lists for manifest
function Get-RelativePaths {
    param($files, $prefix)
    $files | ForEach-Object { "$prefix/$($_.Name)" }
}

$PrePaths      = @(Get-RelativePaths $PreScripts      "migrations/pre")
$PostPaths     = @(Get-RelativePaths $PostScripts     "migrations/post")
$RollbackPaths = @(Get-RelativePaths $RollbackScripts "rollback")

# manifest.json (checksum filled in after zipping)
$ManifestObj = [ordered]@{
    version              = $Version
    component            = $Component
    checksum             = ""
    releaseNotes         = $ReleaseNotes
    preserveFiles        = $PreserveFiles
    preMigrationScripts  = $PrePaths
    postMigrationScripts = $PostPaths
    rollbackScripts      = $RollbackPaths
    builtAt              = (Get-Date -Format "o")
    gitCommit            = $GitCommit
}

$ManifestPath = Join-Path $PkgRoot "manifest.json"
$ManifestObj | ConvertTo-Json -Depth 5 | Set-Content $ManifestPath -Encoding UTF8
Write-OK "manifest.json creato (checksum da aggiornare)"

# ──────────────────────────────────────────────────────────────────────────────
# STEP 5 — Zip
# ──────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 5 - Creazione zip"

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir   = (Resolve-Path $OutDir).Path
$ZipName  = "$($Component.ToLower())-$Version.zip"
$ZipPath  = Join-Path $OutDir $ZipName

if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
    Write-WARN "Zip precedente rimosso: $ZipPath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($PkgRoot, $ZipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
Write-OK "Zip creato: $ZipPath ($([Math]::Round((Get-Item $ZipPath).Length / 1MB, 2)) MB)"

# ──────────────────────────────────────────────────────────────────────────────
# STEP 6 — Calcola SHA-256, aggiorna manifest nel zip
# ──────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 6 - Calcolo checksum SHA-256"

$sha256   = [System.Security.Cryptography.SHA256]::Create()
$zipBytes = [System.IO.File]::ReadAllBytes($ZipPath)
$hashBytes = $sha256.ComputeHash($zipBytes)
$Checksum = [System.BitConverter]::ToString($hashBytes).Replace("-", "").ToLower()
Write-OK "SHA-256: $Checksum"

# Patch manifest.json inside the zip with the real checksum
$zipArchive = [System.IO.Compression.ZipFile]::Open($ZipPath, [System.IO.Compression.ZipArchiveMode]::Update)
try {
    $manifestEntry = $zipArchive.GetEntry("manifest.json")
    if ($manifestEntry) { $manifestEntry.Delete() }

    $ManifestObj["checksum"] = $Checksum
    $newManifestJson = $ManifestObj | ConvertTo-Json -Depth 5

    $newEntry = $zipArchive.CreateEntry("manifest.json", [System.IO.Compression.CompressionLevel]::Optimal)
    $entryStream = $newEntry.Open()
    $writer = [System.IO.StreamWriter]::new($entryStream, [System.Text.Encoding]::UTF8)
    $writer.Write($newManifestJson)
    $writer.Flush()
    $entryStream.Close()
    Write-OK "manifest.json aggiornato nel zip con checksum reale"
} finally {
    $zipArchive.Dispose()
}

# ──────────────────────────────────────────────────────────────────────────────
# STEP 7 — Upload opzionale all'Hub
# ──────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 7 - Upload all'Hub"

$UploadedId = $null

if (![string]::IsNullOrWhiteSpace($HubUrl) -and ![string]::IsNullOrWhiteSpace($AdminKey)) {
    $UploadUrl = "$($HubUrl.TrimEnd('/'))/api/v1/packages?version=$([Uri]::EscapeDataString($Version))&component=$Component"
    if (![string]::IsNullOrWhiteSpace($ReleaseNotes)) {
        $UploadUrl += "&releaseNotes=$([Uri]::EscapeDataString($ReleaseNotes))"
    }

    Write-INFO "Upload verso: $UploadUrl"

    try {
        $fileStream = [System.IO.File]::OpenRead($ZipPath)
        try {
            $multipart = [System.Net.Http.MultipartFormDataContent]::new()
            $fileContent = [System.Net.Http.StreamContent]::new($fileStream)
            $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new("application/zip")
            $multipart.Add($fileContent, "file", $ZipName)

            $httpClient = [System.Net.Http.HttpClient]::new()
            $httpClient.DefaultRequestHeaders.Add("X-Admin-Key", $AdminKey)

            $response = $httpClient.PostAsync($UploadUrl, $multipart).GetAwaiter().GetResult()
            $responseBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

            if ($response.IsSuccessStatusCode) {
                $responseJson = $responseBody | ConvertFrom-Json -ErrorAction SilentlyContinue
                $UploadedId = $responseJson.id
                Write-OK "Package caricato sull'Hub. ID: $UploadedId"
            } else {
                Write-WARN "Upload fallito: HTTP $([int]$response.StatusCode) - $responseBody"
            }
        } finally {
            $fileStream.Dispose()
        }
    } catch {
        Write-WARN "Errore durante l'upload: $_"
        Write-WARN "Il pacchetto e' stato salvato localmente: $ZipPath"
    }
} else {
    Write-INFO "HubUrl/AdminKey non specificati: upload saltato."
    Write-INFO "Per caricare manualmente: POST $ZipPath verso <HubUrl>/api/v1/packages"
}

# ──────────────────────────────────────────────────────────────────────────────
# STEP 8 — Archivia script pending in Applied/
# ──────────────────────────────────────────────────────────────────────────────
Write-Step "STEP 8 - Archiviazione migration scripts"

$AllPending = @($PreScripts) + @($PostScripts) + @($RollbackScripts)
if ($AllPending.Count -gt 0) {
    $AppliedVersionDir = Join-Path $RepoRoot "Migrations" "Applied" "$($Component.ToLower())-$Version"

    foreach ($f in $PreScripts) {
        $destDir = Join-Path $AppliedVersionDir "pre"
        New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        Move-Item -Path $f.FullName -Destination $destDir -Force
        Write-OK "Archiviato (pre): $($f.Name)"
    }
    foreach ($f in $PostScripts) {
        $destDir = Join-Path $AppliedVersionDir "post"
        New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        Move-Item -Path $f.FullName -Destination $destDir -Force
        Write-OK "Archiviato (post): $($f.Name)"
    }
    foreach ($f in $RollbackScripts) {
        $destDir = Join-Path $AppliedVersionDir "rollback"
        New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        Move-Item -Path $f.FullName -Destination $destDir -Force
        Write-OK "Archiviato (rollback): $($f.Name)"
    }
    Write-OK "$($AllPending.Count) script archiviati in Migrations/Applied/$($Component.ToLower())-$Version/"
} else {
    Write-INFO "Nessuno script pending da archiviare."
}

# ──────────────────────────────────────────────────────────────────────────────
# STEP 9 — Cleanup temp
# ──────────────────────────────────────────────────────────────────────────────
if (Test-Path $TempRoot) {
    Remove-Item $TempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

# ──────────────────────────────────────────────────────────────────────────────
# Riepilogo
# ──────────────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "================================================================" -ForegroundColor Magenta
Write-Host "  Package creato con successo!" -ForegroundColor Magenta
Write-Host "================================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "  Componente   : $Component" -ForegroundColor Yellow
Write-Host "  Versione     : $Version" -ForegroundColor Yellow
Write-Host "  File zip     : $ZipPath" -ForegroundColor Yellow
Write-Host "  SHA-256      : $Checksum" -ForegroundColor Yellow
if ($UploadedId) {
    Write-Host "  Hub ID       : $UploadedId" -ForegroundColor Green
}
Write-Host ""
Write-Host "  Per caricare manualmente sull'Hub:" -ForegroundColor DarkCyan
Write-Host "    .\New-UpdatePackage.ps1 -Component $Component -HubUrl <url> -AdminKey <key>" -ForegroundColor White
Write-Host ""
