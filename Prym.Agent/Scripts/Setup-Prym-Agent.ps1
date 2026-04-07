#Requires -RunAsAdministrator
# ==============================================================================
#  Prym Agent - Windows Service Setup Script
#  Deploy path   : C:\Prym\Agent
#  Service name  : PrymAgent
#  Display name  : Prym Agent
#  .NET Target   : 10
# ==============================================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

# ------------------------------------------------------------------------------
# Config
# ------------------------------------------------------------------------------
$DEPLOY_PATH     = "C:\Prym\Agent"
$LOG_DIR         = "C:\Prym\SetupLogs"
$TRANSCRIPT      = "$LOG_DIR\setup_agent_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
$TEMP_DIR        = "C:\Prym\_tmp"
$SERVICE_NAME    = "PrymAgent"
$SERVICE_DISPLAY = "Prym Agent"
$SERVICE_DESC    = "Manages Prym installation updates by communicating with the remote Prym Hub."
$APP_EXE         = "Prym.Agent.exe"
$APP_SETTINGS    = "appsettings.json"

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
Write-Host "  Prym Agent Service Setup - $(Get-Date -Format 'dd/MM/yyyy HH:mm:ss')" -ForegroundColor Magenta
Write-Host "  Transcript: $TRANSCRIPT" -ForegroundColor Magenta
Write-Host "================================================================" -ForegroundColor Magenta

Write-Step "CONFIGURAZIONE"
Write-INFO "Deploy path    : $DEPLOY_PATH"
Write-INFO "Servizio       : $SERVICE_NAME"
Write-INFO "Display name   : $SERVICE_DISPLAY"
Write-INFO "Eseguibile     : $APP_EXE"
Write-INFO "OS             : $((Get-CimInstance Win32_OperatingSystem).Caption)"
Write-INFO "PowerShell     : $($PSVersionTable.PSVersion)"

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
    "$DEPLOY_PATH\work",        # Download zip + temp extract dir (WorkPath)
    "$DEPLOY_PATH\processed",   # Installed packages archive (ProcessedPackagesPath)
    "$DEPLOY_PATH\backups"      # Backup di sicurezza prima di ogni deploy
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
# STEP 3 - .NET 10 Runtime
# ==============================================================================
Write-Step "STEP 3 - .NET 10 Runtime"

$dotnetOutput       = & dotnet --list-runtimes 2>$null
$dotnetRuntimes     = $dotnetOutput | Where-Object { $_ -like "*Microsoft.NETCore.App 10.*" }
$dotnetInstalled    = ($dotnetRuntimes | Measure-Object).Count -gt 0

if ($dotnetInstalled) {
    Write-OK ".NET 10 Runtime trovato:"
    $dotnetRuntimes | ForEach-Object { Write-INFO "  $_" }
} else {
    Write-WARN ".NET 10 Runtime NON trovato"
    Write-INFO "Tentativo installazione tramite winget..."
    $wingetAvailable = $null -ne (Get-Command winget -ErrorAction SilentlyContinue)

    if ($wingetAvailable) {
        try {
            winget install Microsoft.DotNet.Runtime.10 --accept-source-agreements --accept-package-agreements --silent
            Write-OK ".NET 10 Runtime installato via winget"
        } catch {
            Write-WARN "Installazione winget fallita: $_"
        }
    } else {
        Write-WARN "winget non disponibile"
    }

    # Verifica post-installazione
    $dotnetRuntimes  = (& dotnet --list-runtimes 2>$null) | Where-Object { $_ -like "*Microsoft.NETCore.App 10.*" }
    $dotnetInstalled = ($dotnetRuntimes | Measure-Object).Count -gt 0

    if (!$dotnetInstalled) {
        Write-WARN "==========================================================="
        Write-WARN "AZIONE MANUALE SE L'INSTALLAZIONE AUTOMATICA FALLISCE:"
        Write-WARN "Scarica .NET 10 Runtime da:"
        Write-WARN "https://dotnet.microsoft.com/en-us/download/dotnet/10.0"
        Write-WARN "Scegli: .NET Runtime (sezione Windows x64)"
        Write-WARN "==========================================================="
    }
}

# ==============================================================================
# STEP 4 - Verifica file deploy
# ==============================================================================
Write-Step "STEP 4 - Verifica file deploy in $DEPLOY_PATH"

$requiredFiles = @($APP_EXE, $APP_SETTINGS)
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
    Write-WARN "Alcuni file mancano. Esegui prima il publish self-contained:"
    Write-WARN "  dotnet publish Prym.Agent -c Release -r win-x64 --self-contained false -o `"$DEPLOY_PATH`""
    Write-WARN "======================================================"
}

$totalFiles = (Get-ChildItem $DEPLOY_PATH -Recurse -File -ErrorAction SilentlyContinue).Count
Write-INFO "File totali nella cartella deploy: $totalFiles"

# ==============================================================================
# STEP 5 - Verifica configurazione appsettings.json
# ==============================================================================
Write-Step "STEP 5 - Verifica configurazione appsettings.json"

$appSettingsPath = Join-Path $DEPLOY_PATH $APP_SETTINGS
if (Test-Path $appSettingsPath) {
    try {
        $json = Get-Content $appSettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $agentConfig = $json.UpdateAgent

        # Applica gli override di produzione (stessa logica usata dal runtime con ASPNETCORE_ENVIRONMENT=Production).
        # Senza questo merge lo script leggerebbe la sezione base (Components.*.Enabled = false)
        # e segnalerebbe erroneamente che nessun componente e abilitato.
        $prodOverride = $json.Environments.Production.UpdateAgent
        if ($prodOverride) {
            if ($prodOverride.PSObject.Properties['Components']) {
                if ($prodOverride.Components.PSObject.Properties['Server']) {
                    $agentConfig.Components.Server = $prodOverride.Components.Server
                }
                if ($prodOverride.Components.PSObject.Properties['Client']) {
                    $agentConfig.Components.Client = $prodOverride.Components.Client
                }
            }
        }

        # HubUrl
        $hubUrl = $agentConfig.HubUrl
        if ([string]::IsNullOrWhiteSpace($hubUrl) -or $hubUrl -eq "https://updatehub.example.com/hubs/update") {
            Write-WARN "HubUrl non configurato. Imposta l'URL del tuo UpdateHub in: $appSettingsPath"
        } else {
            Write-OK "HubUrl: $hubUrl"
        }

        # ApiKey
        $apiKey = $agentConfig.ApiKey
        if ([string]::IsNullOrWhiteSpace($apiKey) -or $apiKey -eq "REPLACE_WITH_INSTALLATION_API_KEY") {
            Write-WARN "ApiKey non configurata. Genera una chiave dalil Hub e imposta in: $appSettingsPath"
        } else {
            Write-OK "ApiKey: configurata ($($apiKey.Substring(0, [Math]::Min(8, $apiKey.Length)))...)"
        }

        # InstallationId
        $installId = $agentConfig.InstallationId
        if ([string]::IsNullOrWhiteSpace($installId) -or $installId -eq "00000000-0000-0000-0000-000000000000") {
            Write-WARN "InstallationId non configurato. Registra questa installazione nelil Hub e imposta l'ID."
        } else {
            Write-OK "InstallationId: $installId"
        }

        # UI.Password
        $uiPassword = $agentConfig.UI.Password
        if ([string]::IsNullOrWhiteSpace($uiPassword) -or $uiPassword -eq "Admin#123!") {
            Write-WARN "UI.Password e ancora il valore di default ('Admin#123!')."
            Write-WARN "Cambia la password nella sezione UpdateAgent.UI in: $appSettingsPath"
        } else {
            Write-OK "UI.Password: configurata"
        }

        # Components
        $serverEnabled = $agentConfig.Components.Server.Enabled
        $clientEnabled = $agentConfig.Components.Client.Enabled
        Write-INFO "Componenti abilitati (prod) -> Server: $serverEnabled | Client: $clientEnabled"
        if (!$serverEnabled -and !$clientEnabled) {
            Write-WARN "Nessun componente abilitato. Abilita almeno Server o Client in appsettings.json"
        }

        if ($serverEnabled) {
            $serverPath = $agentConfig.Components.Server.DeployPath
            if ([string]::IsNullOrWhiteSpace($serverPath)) {
                Write-WARN "Components.Server.DeployPath non configurato"
            } else {
                Write-INFO "Server DeployPath : $serverPath"
            }
            $connStr = $agentConfig.Components.Server.ConnectionString
            if ([string]::IsNullOrWhiteSpace($connStr)) {
                Write-WARN "Components.Server.ConnectionString non configurata (necessaria per le migrazioni SQL)"
            } else {
                Write-OK "ConnectionString  : configurata"
            }

            # HealthCheckUrl
            $healthUrl = $agentConfig.Components.Server.HealthCheckUrl
            if ([string]::IsNullOrWhiteSpace($healthUrl) -or $healthUrl -like "http://localhost/api/*") {
                Write-WARN "Components.Server.HealthCheckUrl usa un valore placeholder: '$healthUrl'"
                Write-WARN "Imposta l'URL corretto (es. https://localhost:7242/api/v1/health)"
            } else {
                Write-OK "Server HealthCheckUrl: $healthUrl"
            }

            # Server NotificationBaseUrl
            $srvNotifUrl = $agentConfig.Components.Server.NotificationBaseUrl
            if ([string]::IsNullOrWhiteSpace($srvNotifUrl) -or $srvNotifUrl -eq "http://localhost") {
                Write-WARN "Components.Server.NotificationBaseUrl usa un valore placeholder: '$srvNotifUrl'"
                Write-WARN "Imposta l'URL corretto (es. https://localhost:7242)"
            } else {
                Write-OK "Server NotificationBaseUrl: $srvNotifUrl"
            }

            # Server MaintenanceSecret
            $srvSecret = $agentConfig.Components.Server.MaintenanceSecret
            if ([string]::IsNullOrWhiteSpace($srvSecret) -or $srvSecret -eq "REPLACE_WITH_STRONG_SECRET") {
                Write-WARN "Components.Server.MaintenanceSecret e ancora il placeholder."
                Write-WARN "Deve corrispondere a UpdateHub.MaintenanceSecret in appsettings.json del Server."
            } else {
                Write-OK "Server MaintenanceSecret: configurato"
            }
        }

        if ($clientEnabled) {
            $clientPath = $agentConfig.Components.Client.DeployPath
            if ([string]::IsNullOrWhiteSpace($clientPath)) {
                Write-WARN "Components.Client.DeployPath non configurato"
            } else {
                Write-INFO "Client DeployPath : $clientPath"
            }

            # Client NotificationBaseUrl
            $cliNotifUrl = $agentConfig.Components.Client.NotificationBaseUrl
            if ([string]::IsNullOrWhiteSpace($cliNotifUrl) -or $cliNotifUrl -eq "http://localhost") {
                Write-WARN "Components.Client.NotificationBaseUrl usa un valore placeholder: '$cliNotifUrl'"
                Write-WARN "Imposta l'URL corretto (es. https://localhost:7242)"
            } else {
                Write-OK "Client NotificationBaseUrl: $cliNotifUrl"
            }

            # Client MaintenanceSecret
            $cliSecret = $agentConfig.Components.Client.MaintenanceSecret
            if ([string]::IsNullOrWhiteSpace($cliSecret) -or $cliSecret -eq "REPLACE_WITH_STRONG_SECRET") {
                Write-WARN "Components.Client.MaintenanceSecret e ancora il placeholder."
                Write-WARN "Deve corrispondere a UpdateHub.MaintenanceSecret in appsettings.json del Server."
            } else {
                Write-OK "Client MaintenanceSecret: configurato"
            }
        }

    } catch {
        Write-WARN "Impossibile leggere appsettings.json: $_"
    }
} else {
    Write-WARN "appsettings.json non trovato in $DEPLOY_PATH"
    Write-WARN "Il servizio non potra avviarsi senza configurazione."
}

# ==============================================================================
# STEP 6 - Servizio Windows: Crea o aggiorna
# ==============================================================================
Write-Step "STEP 6 - Servizio Windows: $SERVICE_NAME"

$exePath = Join-Path $DEPLOY_PATH $APP_EXE

if (!(Test-Path $exePath)) {
    Write-FAIL "Eseguibile non trovato: $exePath"
    Write-FAIL "Esegui prima il publish. Il servizio non puo essere installato."
} else {
    $existingService = Get-Service -Name $SERVICE_NAME -ErrorAction SilentlyContinue

    if ($existingService) {
        Write-INFO "Servizio '$SERVICE_NAME' gia presente. Aggiornamento..."

        # Ferma il servizio se in esecuzione
        if ($existingService.Status -ne "Stopped") {
            Write-INFO "Arresto del servizio in corso..."
            try {
                Stop-Service -Name $SERVICE_NAME -Force -ErrorAction Stop
                $null = (Get-Service $SERVICE_NAME).WaitForStatus("Stopped", (New-TimeSpan -Seconds 30))
                Write-OK "Servizio fermato"
            } catch {
                Write-WARN "Impossibile fermare il servizio: $_"
            }
        }

        # Aggiorna percorso e descrizione via sc.exe
        try {
            $scResult = & sc.exe config $SERVICE_NAME binPath= "`"$exePath`"" start= auto 2>&1
            Write-OK "Percorso eseguibile aggiornato: $scResult"
        } catch {
            Write-WARN "sc.exe config fallito: $_"
        }

    } else {
        Write-INFO "Creazione nuovo servizio Windows '$SERVICE_NAME'..."
        try {
            New-Service `
                -Name        $SERVICE_NAME `
                -DisplayName $SERVICE_DISPLAY `
                -Description $SERVICE_DESC `
                -BinaryPathName "`"$exePath`"" `
                -StartupType Automatic `
                -ErrorAction Stop
            Write-OK "Servizio '$SERVICE_NAME' creato"
        } catch {
            Write-FAIL "Impossibile creare il servizio: $_"
            Write-FAIL "$_"
        }
    }

    # Imposta descrizione (New-Service non accetta -Description su PS 5)
    try {
        $svcObj = Get-WmiObject Win32_Service -Filter "Name='$SERVICE_NAME'" -ErrorAction SilentlyContinue
        if ($svcObj) {
            $null = $svcObj.Change($null, $null, $null, $null, $null, $null, $null, $null, $null, $null, $SERVICE_DESC)
            Write-OK "Descrizione servizio impostata"
        }
    } catch {
        Write-INFO "Impossibile impostare la descrizione del servizio: $_"
    }
}

# ==============================================================================
# STEP 7 - Configura azioni di recovery (riavvio automatico su crash)
# ==============================================================================
Write-Step "STEP 7 - Azioni di recovery (riavvio automatico su crash)"

$existingService2 = Get-Service -Name $SERVICE_NAME -ErrorAction SilentlyContinue
if ($existingService2) {
    try {
        # Riavvio dopo 30s al primo crash, 60s al secondo, 120s dal terzo in poi
        $scFailResult = & sc.exe failure $SERVICE_NAME reset= 86400 actions= restart/30000/restart/60000/restart/120000 2>&1
        Write-OK "Recovery configurata: restart 30s/60s/120s | reset=86400s -> $scFailResult"
    } catch {
        Write-WARN "Impossibile configurare recovery: $_"
    }
} else {
    Write-WARN "Servizio non trovato, recovery non configurata"
}

# ==============================================================================
# STEP 8 - Permessi filesystem
# ==============================================================================
Write-Step "STEP 8 - Permessi filesystem"

# Il servizio gira come LocalSystem di default; puo accedere a tutto.
# Se si vuole girare con un account dedicato, aggiungere i permessi qui.
$serviceAccount = "NT AUTHORITY\SYSTEM"

try {
    icacls $DEPLOY_PATH /grant "${serviceAccount}:(OI)(CI)F" /T /Q
    Write-OK "Accesso completo concesso a '$serviceAccount' su $DEPLOY_PATH"
} catch {
    Write-WARN "Errore permessi su $DEPLOY_PATH : $_"
}

$logFolder = "$DEPLOY_PATH\logs"
if (!(Test-Path $logFolder)) { New-Item -ItemType Directory -Force -Path $logFolder | Out-Null }
try {
    icacls $logFolder /grant "${serviceAccount}:(OI)(CI)F" /T /Q
    Write-OK "Scrittura concessa su $logFolder"
} catch {
    Write-WARN "Errore permessi logs: $_"
}

# ==============================================================================
# STEP 9 - Windows Firewall (outbound SignalR - di solito gia aperto)
# ==============================================================================
Write-Step "STEP 9 - Windows Firewall"

Write-INFO "L'agente avvia solo connessioni OUTBOUND verso il Hub."
Write-INFO "Le connessioni outbound HTTPS (443) sono generalmente permesse di default."
Write-INFO "Nessuna regola inbound necessaria per l'agente."

# Verifica che l'outbound HTTPS non sia bloccato
$blockedOutbound = Get-NetFirewallRule -Direction Outbound -Action Block -Enabled True -ErrorAction SilentlyContinue |
    Where-Object { $_.Profile -match "Domain|Private|Public" }
if ($blockedOutbound) {
    Write-WARN "Trovate regole outbound block attive. Verificare che l'agente possa raggiungere il Hub."
} else {
    Write-OK "Nessuna regola outbound block attiva"
}

# ==============================================================================
# STEP 10 - Avvio del servizio
# ==============================================================================
Write-Step "STEP 10 - Avvio del servizio Windows"

$appSettingsPath2 = Join-Path $DEPLOY_PATH $APP_SETTINGS
$configOk = $true

if (Test-Path $appSettingsPath2) {
    try {
        $json2    = Get-Content $appSettingsPath2 -Raw -Encoding UTF8 | ConvertFrom-Json
        $hubUrl2  = $json2.UpdateAgent.HubUrl
        $apiKey2  = $json2.UpdateAgent.ApiKey
        $instId2  = $json2.UpdateAgent.InstallationId
        if ($hubUrl2 -eq "https://updatehub.example.com/hubs/update" -or
            $apiKey2 -eq "REPLACE_WITH_INSTALLATION_API_KEY" -or
            $instId2 -eq "00000000-0000-0000-0000-000000000000") {
            $configOk = $false
            Write-WARN "Configurazione incompleta (vedi STEP 5). Il servizio verra creato ma non avviato."
            Write-WARN "Completa appsettings.json e poi avvia manualmente:"
            Write-WARN "  Start-Service $SERVICE_NAME"
            Write-WARN "  # oppure:"
            Write-WARN "  sc.exe start $SERVICE_NAME"
        }
    } catch { }
}

if ($configOk) {
    $svcToStart = Get-Service -Name $SERVICE_NAME -ErrorAction SilentlyContinue
    if ($svcToStart) {
        try {
            Start-Service -Name $SERVICE_NAME -ErrorAction Stop
            Start-Sleep -Seconds 3
            $svcToStart.Refresh()
            if ($svcToStart.Status -eq "Running") {
                Write-OK "Servizio avviato: $($svcToStart.Status)"
            } else {
                Write-WARN "Servizio non in Running: $($svcToStart.Status)"
                Write-WARN "Controlla i log: $DEPLOY_PATH\logs\"
                Write-WARN "Controlla Event Viewer: Applicazione -> Prym Update Agent"
            }
        } catch {
            Write-FAIL "Impossibile avviare il servizio: $_"
            Write-WARN "Controlla i log: $DEPLOY_PATH\logs\"
        }
    } else {
        Write-WARN "Servizio non trovato, impossibile avviare."
    }
}

# ==============================================================================
# STEP 11 - Verifica stato servizio
# ==============================================================================
Write-Step "STEP 11 - Verifica stato servizio"

$finalSvc = Get-Service -Name $SERVICE_NAME -ErrorAction SilentlyContinue
if ($finalSvc) {
    $statusColor = switch ($finalSvc.Status) {
        "Running" { "Green" }
        "Stopped" { "Yellow" }
        default   { "Red" }
    }
    Write-Host "  Servizio   : $($finalSvc.DisplayName)" -ForegroundColor White
    Write-Host "  Stato      : $($finalSvc.Status)"      -ForegroundColor $statusColor
    Write-Host "  StartType  : $($finalSvc.StartType)"   -ForegroundColor White

    # Mostra le ultime righe di log se esistono
    $logPattern = "$DEPLOY_PATH\logs\agent-*.log"
    $latestLog  = Get-ChildItem $logPattern -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        Write-INFO "Ultime righe log ($($latestLog.Name)):"
        Get-Content $latestLog.FullName -Tail 5 -ErrorAction SilentlyContinue | ForEach-Object { Write-INFO "  $_" }
    }
} else {
    Write-WARN "Servizio '$SERVICE_NAME' non trovato nel registro di sistema."
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
Write-Host "  Servizio        : $SERVICE_NAME" -ForegroundColor Yellow
Write-Host "  Deploy path     : $DEPLOY_PATH" -ForegroundColor Yellow
Write-Host "  Configurazione  : $DEPLOY_PATH\$APP_SETTINGS" -ForegroundColor Yellow
Write-Host "  Log agente      : $DEPLOY_PATH\logs\" -ForegroundColor Yellow
Write-Host "  Download/work   : $DEPLOY_PATH\work\" -ForegroundColor Yellow
Write-Host "  Pacchetti inst. : $DEPLOY_PATH\processed\" -ForegroundColor Yellow
Write-Host "  Backup          : $DEPLOY_PATH\backups\" -ForegroundColor Yellow
Write-Host "  Log setup       : $TRANSCRIPT" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Comandi utili:" -ForegroundColor DarkCyan
Write-Host "    Start-Service $SERVICE_NAME" -ForegroundColor White
Write-Host "    Stop-Service  $SERVICE_NAME" -ForegroundColor White
Write-Host "    Get-Service   $SERVICE_NAME" -ForegroundColor White
Write-Host "    sc.exe query  $SERVICE_NAME" -ForegroundColor White
Write-Host "    # Per disinstallare il servizio:" -ForegroundColor DarkGray
Write-Host "    sc.exe delete $SERVICE_NAME" -ForegroundColor White
Write-Host ""
Write-Host "  IMPORTANTE: Configura HubUrl, ApiKey e InstallationId in appsettings.json" -ForegroundColor Red
Write-Host "              prima di avviare il servizio." -ForegroundColor Red
Write-Host ""

Stop-Transcript
