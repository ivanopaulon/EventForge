@echo off
REM Script di utilità per eseguire l'analisi dei conflitti di route in EventForge
REM Utilizzo: analyze-routes.bat [percorso-controllers] [file-output]

echo EventForge Route Conflict Analyzer - Avvio Script
echo =================================================

REM Parametri di default
set CONTROLLERS_PATH=%1
if "%CONTROLLERS_PATH%"=="" set CONTROLLERS_PATH=EventForge.Server/Controllers

set OUTPUT_FILE=%2
if "%OUTPUT_FILE%"=="" set OUTPUT_FILE=route_analysis_report.txt

echo 📂 Percorso Controllers: %CONTROLLERS_PATH%
echo 📄 File Report: %OUTPUT_FILE%
echo.

REM Esegui build del progetto di test
echo 🔨 Build del progetto di test...
dotnet build EventForge.Tests --configuration Release --verbosity quiet

if %ERRORLEVEL% neq 0 (
    echo ❌ Errore durante il build del progetto di test
    exit /b 1
)

echo ✅ Build completato
echo.

REM Esegui l'analisi tramite test
echo 🔍 Avvio analisi dei conflitti di route...
dotnet test EventForge.Tests --filter Category=RouteAnalysis --configuration Release --nologo

set ANALYZER_EXIT_CODE=%ERRORLEVEL%

echo.

REM Mostra il risultato
if %ANALYZER_EXIT_CODE% equ 0 (
    echo ✅ Analisi completata con successo - Nessun conflitto rilevato!
) else (
    echo ⚠️  Analisi completata - Conflitti rilevati!
    echo Consulta il file %OUTPUT_FILE% per i dettagli
)

echo.
echo 📊 Risultati salvati in: %OUTPUT_FILE%
echo 📋 Consulta SWAGGER_ROUTE_CONFLICTS_CHECKLIST.md per la procedura di risoluzione

exit /b %ANALYZER_EXIT_CODE%