#Requires -RunAsAdministrator
# ==============================================================================
#  EventForge Client - IIS Full Setup Script
#  Blazor WebAssembly (file statici — nessun runtime .NET richiesto sul server)
#  Deploy path : C:\Prym\Client
#  Port        : 5240  (HTTPS)
#  App Pool    : EventForgeClient
#  IIS Site    : EventForgeClient
# ==============================================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

# ------------------------------------------------------------------------------
# Config
# ------------------------------------------------------------------------------
$DEPLOY_PATH        = "C:\Prym\Client"
$SITE_PATH          = "$DEPLOY_PATH\wwwroot"
$LOG_DIR            = "C:\Prym\SetupLogs"
$TRANSCRIPT         = "$LOG_DIR\setup_client_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
$TEMP_DIR           = "C:\Prym\_tmp"
$SITE_NAME          = "EventForgeClient"
$POOL_NAME          = "EventForgeClient"
$SITE_PORT          = 5240
$SERVER_PORT        = 7242   # Porta HTTPS del Server; deve corrispondere a Environments:Production:ApiSettings:BaseUrl in appsettings.json
$SITE_CERT_FRIENDLY = "EventForge Client IIS"

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
Write-Host "  EventForge Client IIS Setup - $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')" -ForegroundColor Magenta
Write-Host "  Transcript: $TRANSCRIPT" -ForegroundColor Magenta
Write-Host "================================================================" -ForegroundColor Magenta

Write-Step "CONFIGURAZIONE"
Write-INFO "Publish dir : $DEPLOY_PATH"
Write-INFO "Sito IIS    : $SITE_PATH"
Write-INFO "Pool        : $POOL_NAME"
Write-INFO "Sito        : $SITE_NAME"
Write-INFO "Porta       : $SITE_PORT (HTTPS)"
Write-INFO "Tipo        : Blazor WebAssembly (file statici)"
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

$folders = @($DEPLOY_PATH, $SITE_PATH, "$DEPLOY_PATH\logs")

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
    "IIS-HealthAndDiagnostics",
    "IIS-HttpLogging",
    "IIS-LoggingLibraries",
    "IIS-RequestMonitor",
    "IIS-HttpTracing",
    "IIS-Security",
    "IIS-RequestFiltering",
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
# STEP 5 - IIS URL Rewrite Module
# (Necessario per il routing SPA di Blazor WebAssembly)
# ==============================================================================
Write-Step "STEP 5 - IIS URL Rewrite Module 2.1 (routing SPA Blazor WASM)"

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
# STEP 6 - Riavvio IIS (caricamento moduli)
# ==============================================================================
Write-Step "STEP 6 - Riavvio IIS (caricamento moduli)"

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
# STEP 7 - Verifica moduli IIS critici
# ==============================================================================
Write-Step "STEP 7 - Verifica moduli IIS critici"

$criticalModules = @("RewriteModule", "StaticCompressionModule", "DynamicCompressionModule", "StaticFileModule")
foreach ($mod in $criticalModules) {
    $found = Get-WebConfiguration "system.webServer/globalModules/*" | Where-Object { $_.name -eq $mod }
    if ($found) {
        Write-OK "${mod}: PRESENTE"
    } else {
        Write-WARN "${mod}: NON TROVATO"
    }
}

# ==============================================================================
# STEP 8 - Application Pool
# ==============================================================================
Write-Step "STEP 8 - Application Pool: $POOL_NAME"

if (Test-Path "IIS:\AppPools\$POOL_NAME") {
    Write-INFO "Application Pool '$POOL_NAME' gia esistente. Aggiornamento impostazioni..."
} else {
    New-WebAppPool -Name $POOL_NAME
    Write-OK "Application Pool '$POOL_NAME' creato"
}

# Blazor WASM e' solo file statici: No Managed Code
Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "managedRuntimeVersion"         -Value ""
Write-OK "managedRuntimeVersion = '' (No Managed Code - file statici)"

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
Write-OK "maxProcesses = 1"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "processModel.startupTimeLimit"  -Value ([TimeSpan]::FromSeconds(30))
Write-OK "startupTimeLimit = 30s"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "processModel.shutdownTimeLimit" -Value ([TimeSpan]::FromSeconds(30))
Write-OK "shutdownTimeLimit = 30s"

Set-ItemProperty "IIS:\AppPools\$POOL_NAME" -Name "failure.rapidFailProtection"    -Value $false
Write-OK "rapidFailProtection = False"

# ==============================================================================
# STEP 9 - Certificato SSL + Sito IIS (porta $SITE_PORT HTTPS)
# ==============================================================================
Write-Step "STEP 9 - Certificato SSL + Sito IIS: $SITE_NAME (porta $SITE_PORT HTTPS)"

$appcmd         = "$env:windir\system32\inetsrv\appcmd.exe"
$certThumbprint = $null

# Certificato SSL self-signed
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

# Binding conflict check
Write-INFO "Scansione binding conflittuali su porta $SITE_PORT in tutti i siti IIS..."
$allSites      = @(Get-Website -ErrorAction SilentlyContinue)
$conflictFound = $false
foreach ($other in ($allSites | Where-Object { $_.Name -ne $SITE_NAME })) {
    $conflicts = @(Get-WebBinding -Name $other.Name -ErrorAction SilentlyContinue |
                   Where-Object { $_.bindingInformation -match ":${SITE_PORT}:" })
    foreach ($cb in $conflicts) {
        $conflictFound = $true
        Write-WARN "CONFLITTO: sito '$($other.Name)' usa porta $SITE_PORT con '$($cb.bindingInformation)'"
        $appcmdResult = & $appcmd set site "$($other.Name)" "/-bindings.[protocol='$($cb.protocol)',bindingInformation='$($cb.bindingInformation)']" 2>&1
        Write-OK "Binding rimosso da '$($other.Name)': $appcmdResult"
    }
}
if (-not $conflictFound) { Write-OK "Nessun conflitto trovato su porta $SITE_PORT" }

# Rimozione sito esistente
if (Test-Path "IIS:\Sites\$SITE_NAME") {
    Write-INFO "Sito '$SITE_NAME' gia esistente. Rimozione e ricreazione..."
    Remove-WebSite -Name $SITE_NAME
    Write-OK "Sito precedente rimosso"
}

if (!(Test-Path $SITE_PATH)) {
    Write-FAIL "Cartella sito non trovata: $SITE_PATH"
    Write-FAIL "Esegui prima il publish del client:"
    Write-FAIL "  dotnet publish EventForge.Client -c Release -o $DEPLOY_PATH"
    Write-FAIL "Il publish crea automaticamente la cartella wwwroot con tutti i file statici."
    Stop-Transcript
    exit 1
}

# Creazione sito (binding HTTP temporaneo, sostituito da HTTPS sotto)
try {
    New-WebSite -Name $SITE_NAME -PhysicalPath $SITE_PATH -ApplicationPool $POOL_NAME -Port $SITE_PORT -IPAddress "*" -Force | Out-Null
    Write-OK "Sito '$SITE_NAME' creato"
} catch {
    Write-FAIL "Impossibile creare il sito: $_"
    Stop-Transcript
    exit 1
}

# Sostituzione binding HTTP → HTTPS
& $appcmd set site "$SITE_NAME" "/-bindings.[protocol='http',bindingInformation='*:${SITE_PORT}:']" 2>&1 | Out-Null
$addHttps = & $appcmd set site "$SITE_NAME" "/+bindings.[protocol='https',bindingInformation='*:${SITE_PORT}:']" 2>&1
Write-OK "Binding HTTPS aggiunto: $addHttps"

# Associazione certificato SSL
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

try {
    Start-WebSite -Name $SITE_NAME
    Write-OK "Sito avviato"
} catch {
    Write-WARN "Impossibile avviare il sito ora: $_"
}

# ==============================================================================
# STEP 10 - Permessi filesystem
# ==============================================================================
Write-Step "STEP 10 - Permessi filesystem"

$poolIdentity = "IIS AppPool\$POOL_NAME"

try {
    icacls $SITE_PATH /grant "${poolIdentity}:(OI)(CI)RX" /T /Q
    Write-OK "RX concessi a '$poolIdentity' su $SITE_PATH"
} catch {
    Write-WARN "Errore permessi RX: $_"
}

$logFolder = "$DEPLOY_PATH\logs"
if (!(Test-Path $logFolder)) { New-Item -ItemType Directory -Force -Path $logFolder | Out-Null }
try {
    icacls $logFolder /grant "${poolIdentity}:(OI)(CI)M" /T /Q
    Write-OK "Scrittura concessa a '$poolIdentity' su $logFolder"
} catch {
    Write-WARN "Errore permessi scrittura $logFolder : $_"
}

try {
    icacls $SITE_PATH /grant "IUSR:(OI)(CI)R" /T /Q
    Write-OK "Lettura concessa a IUSR su $SITE_PATH"
} catch {
    Write-WARN "Errore permessi IUSR: $_"
}

# ==============================================================================
# STEP 11 - Windows Firewall (porta $SITE_PORT)
# ==============================================================================
Write-Step "STEP 11 - Windows Firewall (porta $SITE_PORT)"

$oldHttpRule = Get-NetFirewallRule -DisplayName "EventForge Client HTTP $SITE_PORT" -ErrorAction SilentlyContinue
if ($oldHttpRule) {
    Remove-NetFirewallRule -DisplayName "EventForge Client HTTP $SITE_PORT" -ErrorAction SilentlyContinue
    Write-OK "Vecchia regola 'HTTP $SITE_PORT' rimossa"
}

$ruleName     = "EventForge Client HTTPS $SITE_PORT"
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

# ==============================================================================
# STEP 12 - Impostazioni Windows avanzate
# ==============================================================================
Write-Step "STEP 12 - Impostazioni Windows avanzate"

try {
    Set-WebConfigurationProperty -pspath "MACHINE/WEBROOT/APPHOST" -filter "system.webServer/urlCompression" -name "doStaticCompression" -value $true
    Write-OK "Compressione statica server abilitata"
} catch {
    Write-WARN "Impossibile abilitare compressione statica: $_"
}

try {
    $appcmdLocal = "$env:SystemRoot\system32\inetsrv\appcmd.exe"
    & $appcmdLocal set config /section:system.webServer/httpCompression "/+staticTypes.[@mimeType='application/wasm',enabled='true']" 2>&1 | Out-Null
    & $appcmdLocal set config /section:system.webServer/httpCompression "/+staticTypes.[@mimeType='application/octet-stream',enabled='true']" 2>&1 | Out-Null
    Write-OK "Compressione statica per WASM e octet-stream abilitata"
} catch {
    Write-WARN "Compressione WASM gia presente o non applicabile"
}

$netFx = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -ErrorAction SilentlyContinue
if ($netFx -and $netFx.Release -ge 533320) {
    Write-OK ".NET Framework 4.8.1+ installato (Release: $($netFx.Release))"
} else {
    Write-WARN ".NET Framework 4.8.1 non trovato (consigliato come base di sistema)"
}

# ==============================================================================
# STEP 13 - Verifica file deploy
# ==============================================================================
Write-Step "STEP 13 - Verifica file deploy in $SITE_PATH"

$requiredFiles = @("index.html", "web.config")
$deployOk      = $true

foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $SITE_PATH $file
    if (Test-Path $fullPath) {
        $size = (Get-Item $fullPath).Length
        Write-OK "PRESENTE  -> $file ($([math]::Round($size/1KB,1)) KB)"
    } else {
        Write-FAIL "MANCANTE  -> $file"
        $deployOk = $false
    }
}

# Verifica cartella _framework (contenuto core Blazor WASM)
$frameworkDir = Join-Path $SITE_PATH "_framework"
if (Test-Path $frameworkDir) {
    $frameworkFiles = (Get-ChildItem $frameworkDir -Recurse -File -ErrorAction SilentlyContinue).Count
    Write-OK "PRESENTE  -> _framework\ ($frameworkFiles file)"
} else {
    Write-FAIL "MANCANTE  -> _framework\ (cartella Blazor WASM assente)"
    $deployOk = $false
}

if (!$deployOk) {
    Write-WARN "======================================================"
    Write-WARN "Alcuni file mancano. Esegui il publish del client:"
    Write-WARN "  dotnet publish EventForge.Client -c Release -o $DEPLOY_PATH"
    Write-WARN "I file statici verranno creati automaticamente in $SITE_PATH"
    Write-WARN "======================================================"
}

$totalFiles = (Get-ChildItem $SITE_PATH -Recurse -File -ErrorAction SilentlyContinue).Count
Write-INFO "File totali nella cartella sito: $totalFiles"

# ==============================================================================
# STEP 13b - Verifica appsettings.json: Environments.Production.ApiSettings.BaseUrl
# ==============================================================================
Write-Step "STEP 13b - Verifica appsettings.json (ApiSettings.BaseUrl produzione)"

$clientAppSettingsPath = Join-Path $SITE_PATH "appsettings.json"
if (Test-Path $clientAppSettingsPath) {
    try {
        $csJson      = Get-Content $clientAppSettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $prodBaseUrl = $csJson.Environments.Production.ApiSettings.BaseUrl
        $expectedUrl = "https://localhost:$SERVER_PORT/"
        if ($prodBaseUrl -eq $expectedUrl) {
            Write-OK "Environments.Production.ApiSettings.BaseUrl corretto: $prodBaseUrl"
        } elseif (-not [string]::IsNullOrWhiteSpace($prodBaseUrl)) {
            Write-WARN "Environments.Production.ApiSettings.BaseUrl = '$prodBaseUrl'"
            Write-WARN "Atteso: '$expectedUrl' (porta server $SERVER_PORT)"
            Write-WARN "Aggiorna appsettings.json se il server e su una porta diversa."
        } else {
            Write-WARN "Environments.Production.ApiSettings.BaseUrl non trovato in appsettings.json"
            Write-WARN "Il client non sapra dove trovare le API in produzione."
        }
    } catch {
        Write-WARN "Impossibile leggere appsettings.json client: $_"
    }
} else {
    Write-WARN "appsettings.json non trovato in $SITE_PATH"
    Write-WARN "Assicurati che il publish includa wwwroot\appsettings.json"
}

# ==============================================================================
# STEP 14 - Verifica web.config Blazor WASM
# ==============================================================================
Write-Step "STEP 14 - Verifica web.config Blazor WASM"

$webConfigPath = Join-Path $SITE_PATH "web.config"
if (Test-Path $webConfigPath) {
    try {
        [xml]$wc = Get-Content $webConfigPath -Encoding UTF8
        Write-OK "web.config e XML valido"

        $staticContent = $wc.configuration."system.webServer".staticContent
        if ($staticContent) {
            $wasmMime = $staticContent.mimeMap | Where-Object { $_.fileExtension -eq ".wasm" }
            if ($wasmMime) {
                Write-OK "MIME .wasm presente: $($wasmMime.mimeType)"
            } else {
                Write-WARN "MIME .wasm non trovato nel web.config"
            }
        } else {
            Write-WARN "Sezione staticContent non trovata nel web.config"
        }

        $rewrite = $wc.configuration."system.webServer".rewrite
        if ($rewrite) {
            Write-OK "URL Rewrite rules presenti (SPA routing)"
        } else {
            Write-WARN "URL Rewrite rules non trovate - il routing SPA potrebbe non funzionare"
        }
    } catch {
        Write-FAIL "web.config NON e XML valido: $_"
    }
} else {
    Write-WARN "web.config non trovato in $DEPLOY_PATH"
    Write-WARN "Assicurati che il publish includa wwwroot\web.config del progetto client."
}

# ==============================================================================
# STEP 15 - Test connessione HTTPS
# ==============================================================================
Write-Step "STEP 15 - Test connessione HTTPS"

try {
    Restart-WebAppPool -Name $POOL_NAME -ErrorAction SilentlyContinue
    Write-OK "App Pool '$POOL_NAME' riciclato"
} catch {
    Write-WARN "Impossibile riciclare il pool: $_"
}

Start-Sleep -Seconds 3

$testUrl    = "https://localhost:$SITE_PORT/"
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
    Write-FAIL "HTTPS 400 - Bad Request. Verifica binding IIS e certificato SSL. Rilancia lo script."
} elseif ($httpStatus -eq 401 -or $httpStatus -eq 403) {
    Write-OK "HTTPS $httpStatus - Sito ATTIVO (autenticazione richiesta - normale)"
} elseif ($httpStatus -eq 404) {
    Write-WARN "HTTPS 404 - File non trovato. Verifica che index.html sia in $DEPLOY_PATH"
} elseif ($httpStatus -eq 503) {
    Write-FAIL "HTTPS 503 - App Pool non risponde"
} elseif ($httpStatus) {
    Write-WARN "HTTPS $httpStatus - Risposta inattesa"
} else {
    Write-WARN "Nessuna risposta da $testUrl"
    Write-WARN "Verifica: sito avviato, porta $SITE_PORT libera, cert SSL associato"
}

# ==============================================================================
# STEP 16 - Riepilogo stato IIS
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
Write-Host "  -- Moduli IIS --" -ForegroundColor DarkCyan
try {
    Get-WebConfiguration "system.webServer/globalModules/*" |
        Where-Object { $_.name -match "Rewrite|Compression|StaticFile|DefaultDocument" } |
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
Write-Host "  URL client  : https://localhost:$SITE_PORT" -ForegroundColor Yellow
Write-Host "  URL server  : https://localhost:$SERVER_PORT  (EventForge Server, porta HTTPS)" -ForegroundColor Yellow
Write-Host "  Certificato : Self-signed (trusted in LocalMachine\Root)" -ForegroundColor Yellow
Write-Host "  Log setup   : $TRANSCRIPT" -ForegroundColor Yellow
Write-Host ""
Write-Host "  NOTA: Per deploy o update dei file client eseguire:" -ForegroundColor Cyan
Write-Host "    dotnet publish EventForge.Client -c Release -o $DEPLOY_PATH" -ForegroundColor Cyan
Write-Host "    (i file statici vengono creati automaticamente in $SITE_PATH)" -ForegroundColor Cyan
Write-Host ""
Write-Host "  NOTA: Il server API (porta $SERVER_PORT) deve essere raggiungibile dal client." -ForegroundColor Cyan
Write-Host "  Verifica che il server consenta CORS per https://localhost:$SITE_PORT" -ForegroundColor Cyan
Write-Host ""

if ($needsReboot) {
    Write-Host "  [!] RIAVVIO CONSIGLIATO per completare l'installazione dei moduli" -ForegroundColor Red
    Write-Host ""
}

Stop-Transcript
