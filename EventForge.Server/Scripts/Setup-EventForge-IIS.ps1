#Requires -RunAsAdministrator
# ==============================================================================
#  EventForge Server - IIS Full Setup Script
#  Deploy path : C:\Prym\Server
#  Port        : 7242
#  App Pool    : EventForge
#  IIS Site    : EventForge
#  .NET Target : 10
# ==============================================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

# ------------------------------------------------------------------------------
# Config
# ------------------------------------------------------------------------------
$DEPLOY_PATH = "C:\Prym\Server"
$LOG_DIR     = "C:\Prym\SetupLogs"
$TRANSCRIPT  = "$LOG_DIR\setup_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
$TEMP_DIR    = "C:\Prym\_tmp"
$SITE_NAME   = "EventForge"
$POOL_NAME   = "EventForge"
$SITE_PORT   = 7242
$APP_DLL     = "EventForge.Server.dll"

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
Write-Host "  EventForge IIS Setup - $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')" -ForegroundColor Magenta
Write-Host "  Transcript: $TRANSCRIPT" -ForegroundColor Magenta
Write-Host "================================================================" -ForegroundColor Magenta

Write-Step "CONFIGURAZIONE"
Write-INFO "Deploy path : $DEPLOY_PATH"
Write-INFO "Pool        : $POOL_NAME"
Write-INFO "Sito        : $SITE_NAME"
Write-INFO "Porta       : $SITE_PORT"
Write-INFO "DLL         : $APP_DLL"
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
    "$DEPLOY_PATH\Logs"
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
# STEP 10 - Sito IIS
# ==============================================================================
Write-Step "STEP 10 - Sito IIS: $SITE_NAME (porta $SITE_PORT)"

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

try {
    New-WebSite -Name $SITE_NAME -PhysicalPath $DEPLOY_PATH -ApplicationPool $POOL_NAME -Port $SITE_PORT -IPAddress "*" -Force | Out-Null
    Write-OK "Sito '$SITE_NAME' creato su porta $SITE_PORT"
} catch {
    Write-FAIL "Impossibile creare il sito: $_"
    Stop-Transcript
    exit 1
}

# Fix binding: assicura che sia *:PORT: (wildcard hostname) per accettare localhost e qualsiasi hostname
try {
    $existingBindings = @(Get-WebBinding -Name $SITE_NAME -Protocol "http")
    $correctBinding   = "*:${SITE_PORT}:"
    $hasCorrect       = $existingBindings | Where-Object { $_.bindingInformation -eq $correctBinding }
    if (-not $hasCorrect) {
        Write-INFO "Binding attuale: '$($existingBindings.bindingInformation)' - correzione a '$correctBinding'..."
        foreach ($b in $existingBindings) { Remove-WebBinding -Name $SITE_NAME -Protocol "http" -IPAddress $b.IPAddress -Port $SITE_PORT -HostHeader $b.HostHeader -ErrorAction SilentlyContinue }
        New-WebBinding -Name $SITE_NAME -Protocol "http" -IPAddress "*" -Port $SITE_PORT -HostHeader "" | Out-Null
        Write-OK "Binding corretto a: $correctBinding"
    } else {
        Write-OK "Binding corretto: $correctBinding"
    }
} catch {
    Write-WARN "Impossibile verificare/correggere il binding: $_"
}

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

try {
    icacls $DEPLOY_PATH /grant "${poolIdentity}:(OI)(CI)RX" /T /Q
    Write-OK "RX concessi a '$poolIdentity' su $DEPLOY_PATH"
} catch {
    Write-WARN "Errore permessi RX: $_"
}

$logFolders = @("$DEPLOY_PATH\logs", "$DEPLOY_PATH\Logs")
foreach ($logFolder in $logFolders) {
    if (!(Test-Path $logFolder)) { New-Item -ItemType Directory -Force -Path $logFolder | Out-Null }
    try {
        icacls $logFolder /grant "${poolIdentity}:(OI)(CI)M" /T /Q
        Write-OK "Scrittura concessa a '$poolIdentity' su $logFolder"
    } catch {
        Write-WARN "Errore permessi scrittura $logFolder : $_"
    }
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

$ruleName     = "EventForge Server HTTP $SITE_PORT"
$existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue

if ($existingRule) {
    Write-INFO "Regola firewall gia presente: '$ruleName'"
} else {
    try {
        New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $SITE_PORT -Action Allow -Profile Any -Enabled True | Out-Null
        Write-OK "Regola firewall creata: porta $SITE_PORT TCP inbound"
    } catch {
        Write-WARN "Impossibile creare regola firewall: $_"
    }
}

# ==============================================================================
# STEP 13 - Impostazioni Windows avanzate
# ==============================================================================
Write-Step "STEP 13 - Impostazioni Windows avanzate"

try {
    Set-WebConfigurationProperty -pspath "MACHINE/WEBROOT/APPHOST" -filter "system.webServer/urlCompression" -name "doDynamicCompression" -value $true
    Write-OK "Compressione dinamica server abilitata"
} catch {
    Write-WARN "Impossibile abilitare compressione dinamica: $_"
}

try {
    $appcmd = "$env:SystemRoot\system32\inetsrv\appcmd.exe"
    & $appcmd set config /section:system.webServer/httpCompression "/+dynamicTypes.[@mimeType='application/json',enabled='true']" 2>&1 | Out-Null
    Write-OK "Compressione JSON abilitata"
} catch {
    Write-WARN "Compressione JSON gia presente o non applicabile"
}

try {
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\HTTP\Parameters"
    if (!(Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }
    Set-ItemProperty -Path $regPath -Name "MaxConnections" -Value 65536 -Type DWord
    Write-OK "HTTP.sys MaxConnections = 65536"
} catch {
    Write-WARN "Impossibile impostare MaxConnections: $_"
}

$netFx = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -ErrorAction SilentlyContinue
if ($netFx -and $netFx.Release -ge 533320) {
    Write-OK ".NET Framework 4.8.1+ installato (Release: $($netFx.Release))"
} else {
    Write-WARN ".NET Framework 4.8.1 non trovato (consigliato come base di sistema)"
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
    Write-WARN "  dotnet publish -c Release -o `"$DEPLOY_PATH`""
    Write-WARN "======================================================"
}

$totalFiles = (Get-ChildItem $DEPLOY_PATH -Recurse -File -ErrorAction SilentlyContinue).Count
Write-INFO "File totali nella cartella deploy: $totalFiles"

# ==============================================================================
# STEP 15 - Verifica SQL Server
# ==============================================================================
Write-Step "STEP 15 - Verifica SQL Server"

$sqlServices = Get-Service -Name "*SQL*" -ErrorAction SilentlyContinue
if ($sqlServices) {
    $sqlOptional = @('SQLAgent$SQLEXPRESS', 'SQLBrowser')
    foreach ($svc in $sqlServices) {
        if ($svc.Status -eq "Running") {
            Write-OK "$($svc.DisplayName) - RUNNING"
        } elseif ($sqlOptional -contains $svc.Name) {
            Write-INFO "$($svc.DisplayName) - Stopped (normale su Express/Home)"
        } else {
            Write-WARN "$($svc.DisplayName) - $($svc.Status)"
        }
    }
    $sqlStopped = $sqlServices | Where-Object { $_.Name -eq "MSSQL`$SQLEXPRESS" -and $_.Status -ne "Running" }
    foreach ($svc in $sqlStopped) {
        try {
            Start-Service $svc.Name
            Write-OK "Avviato: $($svc.DisplayName)"
        } catch {
            Write-WARN "Impossibile avviare $($svc.Name): $_"
        }
    }
} else {
    Write-WARN "Nessun servizio SQL Server trovato."
}

# ==============================================================================
# STEP 16 - Verifica web.config
# ==============================================================================
Write-Step "STEP 16 - Verifica web.config"

$webConfigPath = Join-Path $DEPLOY_PATH "web.config"
if (Test-Path $webConfigPath) {
    try {
        [xml]$wc = Get-Content $webConfigPath
        Write-OK "web.config e XML valido"
        $aspNetCore = $wc.configuration.location."system.webServer".aspNetCore
        if ($aspNetCore) {
            Write-INFO "processPath  : $($aspNetCore.processPath)"
            Write-INFO "arguments    : $($aspNetCore.arguments)"
            Write-INFO "hostingModel : $($aspNetCore.hostingModel)"
            Write-INFO "stdoutLog    : $($aspNetCore.stdoutLogEnabled)"
        } else {
            Write-WARN "Elemento aspNetCore non trovato nel web.config"
        }
    } catch {
        Write-FAIL "web.config NON e XML valido: $_"
    }
} else {
    Write-WARN "web.config non trovato in $DEPLOY_PATH"
}

# ==============================================================================
# STEP 17 - Test HTTP
# ==============================================================================
Write-Step "STEP 17 - Test connessione HTTP"

try {
    Restart-WebAppPool -Name $POOL_NAME -ErrorAction SilentlyContinue
    Write-OK "App Pool '$POOL_NAME' riciclato (carica nuovi file deploy)"
} catch {
    Write-WARN "Impossibile riciclare il pool: $_"
}

Start-Sleep -Seconds 5

$testUrl    = "http://localhost:$SITE_PORT"
$httpStatus = $null

try {
    $response   = Invoke-WebRequest -Uri $testUrl -UseBasicParsing -TimeoutSec 15 -ErrorAction Stop
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

if ($httpStatus -and $httpStatus -lt 500) {
    if ($httpStatus -ge 200 -and $httpStatus -lt 300) {
        Write-OK "HTTP $httpStatus - Sito raggiungibile su $testUrl"
    } elseif ($httpStatus -ge 300 -and $httpStatus -lt 400) {
        Write-OK "HTTP $httpStatus - Sito ATTIVO (redirect, es. HTTP->HTTPS - normale)"
    } else {
        Write-OK "HTTP $httpStatus - Sito ATTIVO"
    }
} elseif ($httpStatus -eq 500) {
    Write-FAIL "HTTP 500 - L'app va in errore. Controlla: $DEPLOY_PATH\logs\"
    Write-FAIL "Per debug: imposta stdoutLogEnabled='true' in web.config"
} elseif ($httpStatus -eq 503) {
    Write-FAIL "HTTP 503 - App Pool non risponde o crash all avvio"
} elseif ($httpStatus) {
    Write-WARN "HTTP $httpStatus - Risposta inattesa"
} else {
    Write-WARN "Nessuna risposta da $testUrl"
    Write-WARN "Verifica che il sito sia avviato e la porta $SITE_PORT sia libera"
}

# ==============================================================================
# STEP 18 - Riepilogo finale
# ==============================================================================
Write-Step "STEP 18 - Riepilogo stato IIS"

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
Write-Host "  URL sito    : http://localhost:$SITE_PORT" -ForegroundColor Yellow
Write-Host "  Log app IIS : $DEPLOY_PATH\logs\" -ForegroundColor Yellow
Write-Host "  Log setup   : $TRANSCRIPT" -ForegroundColor Yellow
Write-Host ""

if ($needsReboot) {
    Write-Host "  [!] RIAVVIO CONSIGLIATO per completare l'installazione dei moduli" -ForegroundColor Red
    Write-Host ""
}

Stop-Transcript
