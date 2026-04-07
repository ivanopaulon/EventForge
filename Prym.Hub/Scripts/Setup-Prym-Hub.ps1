#Requires -RunAsAdministrator
# ==============================================================================
#  Prym Hub - IIS Full Setup Script
#  Deploy path : C:\Prym\Hub
#  Port        : 7244
#  App Pool    : PrymHub
#  IIS Site    : Prym Hub
#  .NET Target : 10
# ==============================================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

# ------------------------------------------------------------------------------
# Config
# ------------------------------------------------------------------------------
$DEPLOY_PATH        = "C:\Prym\Hub"
$LOG_DIR            = "C:\Prym\SetupLogs"
$TRANSCRIPT         = "$LOG_DIR\setup_hub_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
$TEMP_DIR           = "C:\Prym\_tmp"
$SITE_NAME          = "Prym Hub"
$POOL_NAME          = "PrymHub"
$SITE_PORT          = 7244   # HTTPS IIS port; overridden below from UpdateHub.UI.HttpsPort in appsettings.json
$SITE_CERT_FRIENDLY = "Prym Hub IIS"
$APP_DLL            = "Prym.Hub.dll"
$PACKAGES_PATH      = "$DEPLOY_PATH\packages"
$DB_FILE            = "$DEPLOY_PATH\hub.db"

# ------------------------------------------------------------------------------
# Helpers
# ------------------------------------------------------------------------------
function Write-Step { param($msg) Write-Host "" ; Write-Host "== $msg ==" -ForegroundColor Cyan }
function Write-OK   { param($msg) Write-Host "  [OK]   $msg" -ForegroundColor Green }
function Write-WARN { param($msg) Write-Host "  [WARN] $msg" -ForegroundColor Yellow }
function Write-FAIL { param($msg) Write-Host "  [FAIL] $msg" -ForegroundColor Red }
function Write-INFO { param($msg) Write-Host "  [    ] $msg" -ForegroundColor White }

# ------------------------------------------------------------------------------
# Init log
# ------------------------------------------------------------------------------
New-Item -ItemType Directory -Force -Path $LOG_DIR  | Out-Null
New-Item -ItemType Directory -Force -Path $TEMP_DIR | Out-Null
Start-Transcript -Path $TRANSCRIPT -Append

Write-Host "================================================================" -ForegroundColor Magenta
Write-Host "  Prym Hub IIS Setup - $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')" -ForegroundColor Magenta
Write-Host "  Transcript: $TRANSCRIPT" -ForegroundColor Magenta
Write-Host "================================================================" -ForegroundColor Magenta

# ------------------------------------------------------------------------------
# Read HTTPS port from appsettings.json if already present at deploy path.
# Keeps the IIS binding in sync with UpdateHub.UI.HttpsPort without manual edits.
# ------------------------------------------------------------------------------
$appSettingsEarly = Join-Path $DEPLOY_PATH "appsettings.json"
if (Test-Path $appSettingsEarly) {
    try {
        $earlyJson = Get-Content $appSettingsEarly -Raw -Encoding UTF8 | ConvertFrom-Json
        $cfgHttpsPort = $earlyJson.UpdateHub.UI.HttpsPort
        if ($cfgHttpsPort -and [int]$cfgHttpsPort -gt 0) {
            $SITE_PORT = [int]$cfgHttpsPort
            Write-Host "  [    ] HttpsPort letto da appsettings.json: $SITE_PORT" -ForegroundColor White
        }
    } catch {
        Write-Host "  [WARN] Impossibile leggere HttpsPort da appsettings.json, uso default: $SITE_PORT" -ForegroundColor Yellow
    }
}

Write-Step "CONFIGURAZIONE"
Write-INFO "Deploy path : $DEPLOY_PATH"
Write-INFO "Pool        : $POOL_NAME"
Write-INFO "Sito        : $SITE_NAME"
Write-INFO "Porta       : $SITE_PORT (HTTPS)"
Write-INFO "DLL         : $APP_DLL"
Write-INFO "Packages    : $PACKAGES_PATH"
Write-INFO "OS          : $((Get-CimInstance Win32_OperatingSystem).Caption)"
Write-INFO "PowerShell  : $($PSVersionTable.PSVersion)"

# ==============================================================================
# STEP 1 - Verifica Admin
# ==============================================================================
Write-Step "STEP 1 - Verifica privilegi Administrator"
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if ($isAdmin) {
    Write-OK "Script eseguito come Administrator"
} else {
    Write-FAIL "Non sei Administrator. Riavvia PowerShell come Admin."
    Stop-Transcript
    exit 1
}

# ==============================================================================
# STEP 2 - Cartelle necessarie
# ==============================================================================
Write-Step "STEP 2 - Creazione cartelle"

$folders = @(
    $DEPLOY_PATH,
    "$DEPLOY_PATH\logs",
    $PACKAGES_PATH
)

foreach ($f in $folders) {
    if (!(Test-Path $f)) {
        New-Item -ItemType Directory -Force -Path $f | Out-Null
        Write-OK "Creata: $f"
    } else {
        Write-INFO "Gia esistente: $f"
    }
}

# ==============================================================================
# STEP 3 - Feature Windows / IIS
# ==============================================================================
Write-Step "STEP 3 - Abilitazione Feature Windows IIS"

$features = @(
    "IIS-WebServerRole",
    "IIS-WebServer",
    "IIS-CommonHttpFeatures",
    "IIS-StaticContent",
    "IIS-DefaultDocument",
    "IIS-DirectoryBrowsing",
    "IIS-HttpErrors",
    "IIS-HttpRedirect",
    "IIS-ApplicationDevelopment",
    "IIS-ASPNET45",
    "IIS-NetFxExtensibility45",
    "NetFx4Extended-ASPNET45",
    "IIS-ISAPIExtensions",
    "IIS-ISAPIFilter",
    "IIS-HealthAndDiagnostics",
    "IIS-HttpLogging",
    "IIS-LoggingLibraries",
    "IIS-RequestMonitor",
    "IIS-HttpTracing",
    "IIS-Security",
    "IIS-RequestFiltering",
    "IIS-BasicAuthentication",
    "IIS-Performance",
    "IIS-HttpCompressionStatic",
    "IIS-HttpCompressionDynamic",
    "IIS-WebServerManagementTools",
    "IIS-ManagementConsole",
    "IIS-ManagementScriptingTools"
)

$needsReboot = $false
foreach ($feature in $features) {
    try {
        $featureInfo = Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue
        $state = if ($featureInfo) { $featureInfo.State } else { "NotAvailable" }
        if ($state -eq "Enabled") {
            Write-INFO "Gia abilitata: $feature"
        } elseif ($state -eq "NotAvailable") {
            Write-INFO "Non disponibile su questa edizione Windows: $feature"
        } else {
            $result = Enable-WindowsOptionalFeature -Online -FeatureName $feature -NoRestart -ErrorAction Stop
            if ($result -and $result.RestartNeeded) { $needsReboot = $true }
            Write-OK "Abilitata: $feature"
        }
    } catch {
        Write-WARN "Impossibile abilitare ${feature}: $_"
    }
}

if ($needsReboot) {
    Write-WARN "Alcune feature richiedono un riavvio. Continuiamo ma sara necessario riavviare."
}

# ==============================================================================
# STEP 4 - Importa modulo WebAdministration
# ==============================================================================
Write-Step "STEP 4 - Modulo WebAdministration"
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-OK "Modulo WebAdministration caricato"
} catch {
    Write-FAIL "Impossibile caricare WebAdministration. IIS non e installato correttamente."
    Write-FAIL "$_"
    Stop-Transcript
    exit 1
}

# ==============================================================================
# STEP 5 - .NET 10 Hosting Bundle
# ==============================================================================
Write-Step "STEP 5 - .NET 10 ASP.NET Core Hosting Bundle"

$aspNetCoreModuleInstalled = $false
try {
    $module = Get-WebConfiguration "system.webServer/globalModules/*" | Where-Object { $_.name -like "*AspNetCore*" }
    if ($module) {
        Write-OK "AspNetCoreModuleV2 gia registrato in IIS: $($module.image)"
        $aspNetCoreModuleInstalled = $true
    }
} catch {
    Write-WARN "Impossibile verificare modulo IIS: $_"
}

$dotnetOutput    = & dotnet --list-runtimes 2>$null
$dotnetRuntimes  = $dotnetOutput | Where-Object { $_ -like "*Microsoft.AspNetCore.App 10.*" }
$dotnetInstalled = ($dotnetRuntimes | Measure-Object).Count -gt 0

if ($dotnetInstalled) {
    Write-OK ".NET 10 ASP.NET Core Runtime trovato:"
    $dotnetRuntimes | ForEach-Object { Write-INFO "  $_" }
} else {
    Write-WARN ".NET 10 ASP.NET Core Runtime NON trovato"
}

if (!$aspNetCoreModuleInstalled -or !$dotnetInstalled) {
    Write-INFO "Tentativo installazione tramite winget..."
    $wingetAvailable = $null -ne (Get-Command winget -ErrorAction SilentlyContinue)

    if ($wingetAvailable) {
        Write-INFO "Installazione .NET 10 Hosting Bundle via winget..."
        try {
            winget install Microsoft.DotNet.HostingBundle.10 --accept-source-agreements --accept-package-agreements --silent
            Write-OK "Hosting Bundle .NET 10 installato via winget"
            $needsReboot = $true
        } catch {
            Write-WARN "Installazione winget fallita: $_"
        }
    } else {
        Write-WARN "winget non disponibile"
    }

    $hostingBundlePath = "$TEMP_DIR\dotnet-hosting-win.exe"

    if (!$aspNetCoreModuleInstalled) {
        Write-WARN "==========================================================="
        Write-WARN "AZIONE MANUALE SE L'INSTALLAZIONE AUTOMATICA FALLISCE:"
        Write-WARN "Scarica .NET 10 Hosting Bundle da:"
        Write-WARN "https://dotnet.microsoft.com/en-us/download/dotnet/10.0"
        Write-WARN "Scegli: Hosting Bundle (sezione Windows)"
        Write-WARN "==========================================================="

        try {
            Write-INFO "Tentativo download automatico Hosting Bundle..."
            $bundleUri = "https://download.visualstudio.microsoft.com/download/pr/b7c1a532-3c15-4a38-a47c-ad3d62b6bcdc/a7a0f5bc10f1d4e9e6e6b2e37c19e1e9/dotnet-hosting-10.0-win.exe"
            $ProgressPreference = "SilentlyContinue"
            Invoke-WebRequest -Uri $bundleUri -OutFile $hostingBundlePath -UseBasicParsing
            if (Test-Path $hostingBundlePath) {
                Write-OK "Download completato. Installazione in corso..."
                Start-Process -FilePath $hostingBundlePath -ArgumentList "/install /quiet /norestart OPT_NO_SHAREDFX=0" -Wait
                Write-OK "Hosting Bundle installato"
                $needsReboot = $true
            }
        } catch {
            Write-WARN "Download automatico fallito: $_"
            Write-WARN "Installa manualmente il Hosting Bundle e riesegui lo script"
        }
    }
}

# ==============================================================================
# STEP 6 - IIS URL Rewrite Module
# ==============================================================================
Write-Step "STEP 6 - IIS URL Rewrite Module 2.1"

$rewriteModule = Get-WebConfiguration "system.webServer/globalModules/*" | Where-Object { $_.name -like "*Rewrite*" }
if ($rewriteModule) {
    Write-OK "URL Rewrite Module gia installato"
} else {
    Write-INFO "URL Rewrite Module non trovato. Installazione..."
    $wingetAvailable  = $null -ne (Get-Command winget -ErrorAction SilentlyContinue)
    $rewriteInstalled = $false

    if ($wingetAvailable) {
        try {
            winget install Microsoft.IISUrlRewrite --accept-source-agreements --accept-package-agreements --silent 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                $verifyRewrite = Get-WebGlobalModule -Name "RewriteModule" -ErrorAction SilentlyContinue
                if ($verifyRewrite) {
                    Write-OK "URL Rewrite installato via winget"
                    $rewriteInstalled = $true
                } else {
                    Write-WARN "winget exit 0 ma RewriteModule non verificato in IIS - uso fallback MSI"
                }
            } else {
                Write-WARN "winget non ha trovato il pacchetto (exit $LASTEXITCODE) - uso fallback MSI"
            }
        } catch {
            Write-WARN "winget fallito per URL Rewrite: $_"
        }
    }

    if (!$rewriteInstalled) {
        $rewriteUrl  = "https://download.microsoft.com/download/1/2/8/128E2E22-C1B9-44A4-BE2A-5859ED1D4592/rewrite_amd64_en-US.msi"
        $rewritePath = "$TEMP_DIR\rewrite_amd64.msi"
        try {
            Write-INFO "Download URL Rewrite MSI..."
            $ProgressPreference = "SilentlyContinue"
            Invoke-WebRequest -Uri $rewriteUrl -OutFile $rewritePath -UseBasicParsing
            Write-INFO "Installazione URL Rewrite..."
            Start-Process msiexec.exe -ArgumentList "/i `"$rewritePath`" /quiet /norestart" -Wait
            Write-OK "URL Rewrite Module installato"
        } catch {
            Write-FAIL "Impossibile installare URL Rewrite: $_"
            Write-WARN "Download manuale: https://www.iis.net/downloads/microsoft/url-rewrite"
        }
    }
}

# ==============================================================================
# STEP 7 - Riavvio IIS
# ==============================================================================
Write-Step "STEP 7 - Riavvio IIS (caricamento moduli)"
try {
    & iisreset /stop  2>&1 | Out-Null
    Start-Sleep -Seconds 2
    & iisreset /start 2>&1 | Out-Null
    Write-OK "IIS riavviato"
} catch {
    Write-WARN "iisreset fallito: $_"
}

Start-Sleep -Seconds 3
Import-Module WebAdministration -Force -ErrorAction SilentlyContinue

# ==============================================================================
# STEP 8 - Verifica moduli IIS critici
# ==============================================================================
Write-Step "STEP 8 - Verifica moduli IIS critici"

$criticalModules = @("AspNetCoreModuleV2", "RewriteModule", "StaticCompressionModule", "DynamicCompressionModule")
foreach ($mod in $criticalModules) {
    $found = Get-WebConfiguration "system.webServer/globalModules/*" | Where-Object { $_.name -eq $mod }
    if ($found) {
        Write-OK "${mod}: PRESENTE -> $($found.image)"
    } else {
        Write-WARN "${mod}: NON TROVATO"
    }
}

# ==============================================================================
# STEP 9 - Application Pool
# ==============================================================================
Write-Step "STEP 9 - Application Pool: $POOL_NAME"

if (Test-Path "IIS:\AppPools\$POOL_NAME") {
    Write-INFO "Application Pool '$POOL_NAME' gia esistente. Aggiornamento impostazioni..."
} else {
    New-WebAppPool -Name $POOL_NAME
    Write-OK "Application Pool '$POOL_NAME' creato"
}

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "managedRuntimeVersion"         -Value ""
Write-OK "managedRuntimeVersion = '' (No Managed Code)"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "managedPipelineMode"            -Value 0
Write-OK "managedPipelineMode = Integrated"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "enable32BitAppOnWin64"          -Value $false
Write-OK "enable32BitAppOnWin64 = False"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "processModel.identityType"      -Value 4
Write-OK "identityType = ApplicationPoolIdentity"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "autoStart"                      -Value $true
Write-OK "autoStart = True"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "processModel.idleTimeout"       -Value "00:00:00"
Write-OK "idleTimeout = 0 (mai sospeso)"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "recycling.periodicRestart.time" -Value "00:00:00"
Write-OK "periodicRestart.time = 0 (disabilitato)"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "processModel.maxProcesses"      -Value 1
Write-OK "maxProcesses = 1 (inprocess hosting)"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "processModel.startupTimeLimit"  -Value ([TimeSpan]::FromSeconds(120))
Write-OK "startupTimeLimit = 120s"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "processModel.shutdownTimeLimit" -Value ([TimeSpan]::FromSeconds(90))
Write-OK "shutdownTimeLimit = 90s"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "failure.rapidFailProtection"    -Value $false
Write-OK "rapidFailProtection = False"

# ==============================================================================
# STEP 10 - Certificato SSL + Sito IIS: $SITE_NAME (porta $SITE_PORT HTTPS)
# ==============================================================================
Write-Step "STEP 10 - Certificato SSL + Sito IIS: $SITE_NAME (porta $SITE_PORT HTTPS)"

$appcmd         = "$env:windir\system32\inetsrv\appcmd.exe"
$certThumbprint = $null

# ── Certificato SSL self-signed ──────────────────────────────────────────────
$existingCert = Get-ChildItem "Cert:\LocalMachine\My" -ErrorAction SilentlyContinue |
    Where-Object { $_.FriendlyName -eq $SITE_CERT_FRIENDLY -and $_.NotAfter -gt (Get-Date).AddDays(30) } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if ($existingCert) {
    $certThumbprint = $existingCert.Thumbprint
    Write-OK "Certificato '$SITE_CERT_FRIENDLY' gia presente (scade $($existingCert.NotAfter.ToString('dd/MM/yyyy')))"
    Write-INFO "Thumbprint: $certThumbprint"
} else {
    try {
        $newCert = New-SelfSignedCertificate `
            -DnsName "localhost" `
            -CertStoreLocation "cert:\LocalMachine\My" `
            -NotAfter (Get-Date).AddYears(10) `
            -FriendlyName $SITE_CERT_FRIENDLY `
            -KeyExportPolicy Exportable `
            -ErrorAction Stop
        $certThumbprint = $newCert.Thumbprint
        Write-OK "Certificato self-signed creato (valido 10 anni)"
        Write-INFO "Thumbprint: $certThumbprint"
        $rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine")
        $rootStore.Open("ReadWrite")
        $rootStore.Add($newCert)
        $rootStore.Close()
        Write-OK "Certificato aggiunto a 'Trusted Root CA' - nessun warning browser"
    } catch {
        Write-FAIL "Impossibile creare certificato SSL: $_"
        Write-WARN "Configurare manualmente un certificato HTTPS per la porta $SITE_PORT"
    }
}

# ── Binding conflict check ──────────────────────────────────────────────────
Write-INFO "Scansione binding conflittuali su porta $SITE_PORT in tutti i siti IIS..."
$allSites      = @(Get-Website -ErrorAction SilentlyContinue)
$conflictFound = $false
foreach ($other in ($allSites | Where-Object { $_.Name -ne $SITE_NAME })) {
    $conflicts = @(Get-WebBinding -Name $other.Name -ErrorAction SilentlyContinue |
                   Where-Object { $_.bindingInformation -match ":${SITE_PORT}:" })
    foreach ($cb in $conflicts) {
        $conflictFound = $true
        Write-WARN "CONFLITTO: sito '$($other.Name)' usa porta $SITE_PORT con '$($cb.bindingInformation)' (proto: $($cb.protocol))"
        $appcmdResult = & $appcmd set site "$($other.Name)" "/-bindings.[protocol='$($cb.protocol)',bindingInformation='$($cb.bindingInformation)']" 2>&1
        Write-OK "Binding rimosso da '$($other.Name)': $appcmdResult"
    }
}
if (-not $conflictFound) { Write-OK "Nessun conflitto trovato su porta $SITE_PORT" }

# ── Rimozione sito esistente ────────────────────────────────────────────────
if (Test-Path "IIS:\Sites\$SITE_NAME") {
    Write-INFO "Sito '$SITE_NAME' gia esistente. Rimozione e ricreazione..."
    Remove-WebSite -Name $SITE_NAME
    Write-OK "Sito precedente rimosso"
}

if (!(Test-Path $DEPLOY_PATH)) {
    Write-FAIL "Percorso deploy non trovato: $DEPLOY_PATH"
    Write-FAIL "Esegui prima il publish del progetto su questa cartella."
    Stop-Transcript
    exit 1
}

# ── Creazione sito ─────────────────────────────────────────────────────────
try {
    New-WebSite -Name $SITE_NAME -PhysicalPath $DEPLOY_PATH -ApplicationPool $POOL_NAME -Port $SITE_PORT -IPAddress "*" -Force | Out-Null
    Write-OK "Sito '$SITE_NAME' creato"
} catch {
    Write-FAIL "Impossibile creare il sito: $_"
    Stop-Transcript
    exit 1
}

# ── Sostituzione binding HTTP → HTTPS ──────────────────────────────────────
& $appcmd set site "$SITE_NAME" "/-bindings.[protocol='http',bindingInformation='*:${SITE_PORT}:']" 2>&1 | Out-Null
$addHttps = & $appcmd set site "$SITE_NAME" "/+bindings.[protocol='https',bindingInformation='*:${SITE_PORT}:']" 2>&1
Write-OK "Binding HTTPS aggiunto: $addHttps"

# ── Associazione certificato SSL ────────────────────────────────────────────
if ($certThumbprint) {
    try {
        Remove-Item "IIS:\SslBindings\0.0.0.0!$SITE_PORT" -Force -ErrorAction SilentlyContinue
        $sslCert = Get-Item "Cert:\LocalMachine\My\$certThumbprint" -ErrorAction Stop
        New-Item "IIS:\SslBindings\0.0.0.0!$SITE_PORT" -Value $sslCert -Force -ErrorAction Stop | Out-Null
        Write-OK "Certificato SSL associato al binding HTTPS:$SITE_PORT (IIS SslBindings)"
    } catch {
        Write-WARN "IIS SslBindings fallito: $_. Tentativo via netsh..."
        try {
            netsh http delete sslcert ipport=0.0.0.0:$SITE_PORT 2>&1 | Out-Null
            $appGuid  = [System.Guid]::NewGuid().ToString("B")
            $netshOut = netsh http add sslcert ipport=0.0.0.0:$SITE_PORT certhash=$certThumbprint appid="$appGuid" certstorename=MY 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-OK "Certificato SSL associato via netsh"
            } else {
                Write-FAIL "Associazione SSL fallita: $netshOut"
            }
        } catch {
            Write-FAIL "Impossibile associare certificato SSL: $_"
        }
    }
} else {
    Write-WARN "Thumbprint non disponibile - associazione SSL saltata. HTTPS non funzionera'."
}

$finalBinding = (Get-WebBinding -Name $SITE_NAME -Protocol "https" -ErrorAction SilentlyContinue).bindingInformation
Write-OK "Binding finale: 'https $finalBinding'"

# ── Avvio sito ─────────────────────────────────────────────────────────────
try {
    Start-WebSite -Name $SITE_NAME
    Write-OK "Sito avviato"
} catch {
    Write-WARN "Impossibile avviare il sito ora: $_"
}

# ==============================================================================
# STEP 11 - Permessi cartella
# ==============================================================================
Write-Step "STEP 11 - Permessi filesystem"

$poolIdentity = "IIS AppPool\$POOL_NAME"

# Permessi RX sulla cartella deploy
try {
    icacls $DEPLOY_PATH /grant "${poolIdentity}:(OI)(CI)RX" /T /Q
    Write-OK "RX concessi a '$poolIdentity' su $DEPLOY_PATH"
} catch {
    Write-WARN "Errore permessi RX: $_"
}

# Permessi scrittura su logs
$logFolder = "$DEPLOY_PATH\logs"
if (!(Test-Path $logFolder)) { New-Item -ItemType Directory -Force -Path $logFolder | Out-Null }
try {
    icacls $logFolder /grant "${poolIdentity}:(OI)(CI)M" /T /Q
    Write-OK "Scrittura concessa a '$poolIdentity' su $logFolder"
} catch {
    Write-WARN "Errore permessi scrittura $logFolder : $_"
}

# Permessi scrittura su packages (upload pacchetti di aggiornamento)
if (!(Test-Path $PACKAGES_PATH)) { New-Item -ItemType Directory -Force -Path $PACKAGES_PATH | Out-Null }
try {
    icacls $PACKAGES_PATH /grant "${poolIdentity}:(OI)(CI)M" /T /Q
    Write-OK "Scrittura concessa a '$poolIdentity' su $PACKAGES_PATH"
} catch {
    Write-WARN "Errore permessi scrittura packages: $_"
}

# Permessi scrittura sulla root (SQLite updatehub.db viene creato da EF Core all'avvio)
try {
    icacls $DEPLOY_PATH /grant "${poolIdentity}:(M)" /Q
    Write-OK "Modifica concessa a '$poolIdentity' su root $DEPLOY_PATH (per SQLite updatehub.db)"
} catch {
    Write-WARN "Errore permessi M su root: $_"
}

try {
    icacls $DEPLOY_PATH /grant "IUSR:(OI)(CI)R" /T /Q
    Write-OK "Lettura concessa a IUSR su $DEPLOY_PATH"
} catch {
    Write-WARN "Errore permessi IUSR: $_"
}

# ==============================================================================
# STEP 12 - Windows Firewall
# ==============================================================================
Write-Step "STEP 12 - Windows Firewall (porta $SITE_PORT)"

$oldHttpRule = Get-NetFirewallRule -DisplayName "Prym Hub HTTP $SITE_PORT" -ErrorAction SilentlyContinue
if ($oldHttpRule) {
    Remove-NetFirewallRule -DisplayName "Prym Hub HTTP $SITE_PORT" -ErrorAction SilentlyContinue
    Write-OK "Vecchia regola 'HTTP $SITE_PORT' rimossa"
}

$ruleName     = "Prym Hub HTTPS $SITE_PORT"
$existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue

if ($existingRule) {
    Write-INFO "Regola firewall gia presente: '$ruleName'"
} else {
    try {
        New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $SITE_PORT -Action Allow -Profile Any -Enabled True | Out-Null
        Write-OK "Regola firewall creata: porta $SITE_PORT TCP inbound (HTTPS)"
    } catch {
        Write-WARN "Impossibile creare regola firewall: $_"
    }
}

# Standalone HTTP port (UpdateHub.UI.HttpPort) — open only if configured and different from HTTPS port
$standaloneHttpPort = 0
if (Test-Path $appSettingsEarly) {
    try {
        $earlyJsonFw = Get-Content $appSettingsEarly -Raw -Encoding UTF8 | ConvertFrom-Json
        $cfgHttpPort = $earlyJsonFw.UpdateHub.UI.HttpPort
        if ($cfgHttpPort -and [int]$cfgHttpPort -gt 0 -and [int]$cfgHttpPort -ne $SITE_PORT) {
            $standaloneHttpPort = [int]$cfgHttpPort
        }
    } catch { }
}

if ($standaloneHttpPort -gt 0) {
    $httpRuleName     = "Prym Hub HTTP $standaloneHttpPort"
    $existingHttpRule = Get-NetFirewallRule -DisplayName $httpRuleName -ErrorAction SilentlyContinue
    if ($existingHttpRule) {
        Write-INFO "Regola firewall HTTP gia presente: '$httpRuleName'"
    } else {
        try {
            New-NetFirewallRule -DisplayName $httpRuleName -Direction Inbound -Protocol TCP -LocalPort $standaloneHttpPort -Action Allow -Profile Any -Enabled True | Out-Null
            Write-OK "Regola firewall creata: porta $standaloneHttpPort TCP inbound (HTTP standalone)"
        } catch {
            Write-WARN "Impossibile creare regola firewall HTTP: $_"
        }
    }
}

# ==============================================================================
# STEP 13 - Configurazione AdminApiKey in appsettings.json
# ==============================================================================
Write-Step "STEP 13 - Configurazione AdminApiKey"

$appSettingsPath = Join-Path $DEPLOY_PATH "appsettings.json"
if (Test-Path $appSettingsPath) {
    try {
        $json = Get-Content $appSettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json

        # Segnala se AdminApiKey e ancora il placeholder
        $currentKey = $json.UpdateHub.AdminApiKey
        if ([string]::IsNullOrWhiteSpace($currentKey) -or $currentKey -eq "CHANGE-ME-IN-PRODUCTION") {
            Write-WARN "============================================================"
            Write-WARN "ATTENZIONE: AdminApiKey non configurata!"
            Write-WARN "Imposta un valore sicuro in: $appSettingsPath"
            Write-WARN "  Sezione: UpdateHub -> AdminApiKey"
            Write-WARN "Genera una chiave con: [System.Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))"
            Write-WARN "============================================================"
        } else {
            Write-OK "AdminApiKey configurata"
        }

        # Configura PackageStorePath assoluto se ancora relativo
        if ($json.UpdateHub.PSObject.Properties['PackageStorePath']) {
            $currentPkgPath = $json.UpdateHub.PackageStorePath
            if ($currentPkgPath -eq "packages") {
                $json.UpdateHub.PackageStorePath = $PACKAGES_PATH
                $json | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath -Encoding UTF8
                Write-OK "PackageStorePath aggiornato a: $PACKAGES_PATH"
            } else {
                Write-INFO "PackageStorePath gia configurato: $currentPkgPath"
            }
        }
    } catch {
        Write-WARN "Impossibile leggere appsettings.json: $_"
    }
} else {
    Write-WARN "appsettings.json non trovato in $DEPLOY_PATH"
}

# ==============================================================================
# STEP 14 - Verifica file deploy
# ==============================================================================
Write-Step "STEP 14 - Verifica file deploy in $DEPLOY_PATH"

$requiredFiles = @($APP_DLL, "appsettings.json", "web.config")
$deployOk      = $true

foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $DEPLOY_PATH $file
    if (Test-Path $fullPath) {
        $size = (Get-Item $fullPath).Length
        Write-OK "PRESENTE  -> $file ($([math]::Round($size/1KB,1)) KB)"
    } else {
        Write-FAIL "MANCANTE  -> $file"
        $deployOk = $false
    }
}

if (!$deployOk) {
    Write-WARN "======================================================"
    Write-WARN "Alcuni file mancano. Esegui prima il publish:"
    Write-WARN "  dotnet publish Prym.Hub -c Release -o `"$DEPLOY_PATH`""
    Write-WARN "======================================================"
}

$totalFiles = (Get-ChildItem $DEPLOY_PATH -Recurse -File -ErrorAction SilentlyContinue).Count
Write-INFO "File totali nella cartella deploy: $totalFiles"

$wwwrootWebConfig = Join-Path $DEPLOY_PATH "wwwroot\web.config"
if (Test-Path $wwwrootWebConfig) {
    Remove-Item $wwwrootWebConfig -Force -ErrorAction SilentlyContinue
    Write-OK "wwwroot\web.config rimosso (non necessario con inprocess hosting)"
} else {
    Write-OK "wwwroot\web.config assente (corretto)"
}

# ==============================================================================
# STEP 15 - Test connessione HTTPS
# ==============================================================================
Write-Step "STEP 15 - Test connessione HTTPS"

try {
    Restart-WebAppPool -Name $POOL_NAME -ErrorAction SilentlyContinue
    Write-OK "App Pool '$POOL_NAME' riciclato (carica nuovi file deploy)"
} catch {
    Write-WARN "Impossibile riciclare il pool: $_"
}

Start-Sleep -Seconds 5

$testUrl    = "https://localhost:$SITE_PORT/health"
$httpStatus = $null

try {
    if ($PSVersionTable.PSVersion.Major -ge 7) {
        $response = Invoke-WebRequest -Uri $testUrl -UseBasicParsing -TimeoutSec 15 -SkipCertificateCheck -ErrorAction Stop
    } else {
        [Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
        $response = Invoke-WebRequest -Uri $testUrl -UseBasicParsing -TimeoutSec 15 -ErrorAction Stop
    }
    $httpStatus = [int]$response.StatusCode
} catch {
    try {
        if ($_.Exception.Response) {
            $httpStatus = [int]$_.Exception.Response.StatusCode
        } elseif ($_.Exception.Message -match "reindirizzament|redirect|Redirect|TooManyRedirect") {
            $httpStatus = 302
        }
    } catch { }
}

if ($httpStatus -ge 200 -and $httpStatus -lt 300) {
    Write-OK "HTTPS $httpStatus - Sito raggiungibile su $testUrl"
} elseif ($httpStatus -ge 300 -and $httpStatus -lt 400) {
    Write-OK "HTTPS $httpStatus - Sito ATTIVO (redirect - normale)"
} elseif ($httpStatus -eq 400) {
    Write-FAIL "HTTPS 400 - Bad Request"
    Write-FAIL "Verifica binding IIS e certificato SSL. Rilancia lo script."
} elseif ($httpStatus -eq 401 -or $httpStatus -eq 403) {
    Write-OK "HTTPS $httpStatus - Sito ATTIVO (autenticazione richiesta - normale)"
} elseif ($httpStatus -eq 500) {
    Write-FAIL "HTTPS 500 - L'app va in errore. Controlla: $DEPLOY_PATH\logs\"
    Write-FAIL "Per debug: imposta stdoutLogEnabled='true' in web.config"
} elseif ($httpStatus -eq 503) {
    Write-FAIL "HTTPS 503 - App Pool non risponde o crash all avvio"
} elseif ($httpStatus) {
    Write-WARN "HTTPS $httpStatus - Risposta inattesa"
} else {
    Write-WARN "Nessuna risposta da $testUrl"
    Write-WARN "Verifica: sito avviato, porta $SITE_PORT libera, cert SSL associato"
}

# ==============================================================================
# STEP 16 - Riepilogo finale
# ==============================================================================
Write-Step "STEP 16 - Riepilogo stato IIS"

Write-Host ""
Write-Host "  -- Application Pool --" -ForegroundColor DarkCyan
try {
    $poolInfo = Get-Item "IIS:\AppPools\$POOL_NAME"
    Write-INFO "Nome   : $POOL_NAME"
    Write-INFO "Stato  : $($poolInfo.State)"
    Write-INFO ".NET   : '$($poolInfo.managedRuntimeVersion)' (vuoto = No Managed Code OK)"
    Write-INFO "32bit  : $($poolInfo.enable32BitAppOnWin64)"
} catch {
    Write-WARN "Impossibile leggere info pool: $_"
}

Write-Host ""
Write-Host "  -- Sito Web --" -ForegroundColor DarkCyan
try {
    $siteInfo = Get-Item "IIS:\Sites\$SITE_NAME"
    Write-INFO "Nome     : $SITE_NAME"
    Write-INFO "Stato    : $($siteInfo.State)"
    Write-INFO "Binding  : $($siteInfo.Bindings.Collection | ForEach-Object { $_.bindingInformation })"
    Write-INFO "Percorso : $($siteInfo.PhysicalPath)"
} catch {
    Write-WARN "Impossibile leggere info sito: $_"
}

Write-Host ""
Write-Host "  -- .NET Runtimes --" -ForegroundColor DarkCyan
& dotnet --list-runtimes 2>$null | Where-Object { $_ -like "Microsoft.AspNetCore*" } | ForEach-Object { Write-INFO $_ }

Write-Host ""
Write-Host "  -- Moduli IIS --" -ForegroundColor DarkCyan
try {
    Get-WebConfiguration "system.webServer/globalModules/*" |
        Where-Object { $_.name -match "AspNetCore|Rewrite|Compression|StaticFile" } |
        ForEach-Object { Write-INFO "$($_.name)" }
} catch {
    Write-WARN "Impossibile elencare moduli: $_"
}

# ==============================================================================
# FINE
# ==============================================================================
Write-Host ""
Write-Host "================================================================" -ForegroundColor Magenta
Write-Host "  Setup completato: $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')" -ForegroundColor Magenta
Write-Host "  Log salvato in  : $TRANSCRIPT" -ForegroundColor Magenta
Write-Host "================================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "  URL Hub      : https://localhost:$SITE_PORT" -ForegroundColor Yellow
Write-Host "  Dashboard    : https://localhost:$SITE_PORT/" -ForegroundColor Yellow
Write-Host "  Swagger      : https://localhost:$SITE_PORT/swagger" -ForegroundColor Yellow
Write-Host "  SignalR Hub  : https://localhost:$SITE_PORT/hubs/update" -ForegroundColor Yellow
Write-Host "  Packages     : $PACKAGES_PATH" -ForegroundColor Yellow
Write-Host "  Database     : $DB_FILE (creato automaticamente all'avvio)" -ForegroundColor Yellow
Write-Host "  Log app IIS  : $DEPLOY_PATH\logs\" -ForegroundColor Yellow
Write-Host "  Log setup    : $TRANSCRIPT" -ForegroundColor Yellow
Write-Host ""
Write-Host "  IMPORTANTE: Configura AdminApiKey in appsettings.json prima di usare l'hub." -ForegroundColor Red
Write-Host ""

if ($needsReboot) {
    Write-Host "  [!] RIAVVIO CONSIGLIATO per completare l'installazione dei moduli" -ForegroundColor Red
    Write-Host ""
}

Stop-Transcript
