#!/bin/bash

# Script di utilità per eseguire l'analisi dei conflitti di route in EventForge
# Utilizzo: ./analyze-routes.sh [percorso-controllers] [file-output]

echo "EventForge Route Conflict Analyzer - Avvio Script"
echo "================================================="

# Controllo se il progetto analyzer esiste
if [ ! -d "RouteConflictAnalyzer" ]; then
    echo "❌ Errore: Cartella RouteConflictAnalyzer non trovata!"
    echo "Assicurati di eseguire lo script dalla root del progetto EventForge"
    exit 1
fi

# Parametri di default
CONTROLLERS_PATH=${1:-"EventForge.Server/Controllers"}
OUTPUT_FILE=${2:-"route_analysis_report.txt"}

echo "📂 Percorso Controllers: $CONTROLLERS_PATH"
echo "📄 File Report: $OUTPUT_FILE"
echo ""

# Esegui build del progetto analyzer
echo "🔨 Build dell'analyzer..."
cd RouteConflictAnalyzer
dotnet build --configuration Release --verbosity quiet

if [ $? -ne 0 ]; then
    echo "❌ Errore durante il build dell'analyzer"
    exit 1
fi

echo "✅ Build completato"
echo ""

# Esegui l'analisi
echo "🔍 Avvio analisi dei conflitti di route..."
dotnet run --configuration Release -- "../$CONTROLLERS_PATH" "../$OUTPUT_FILE"

ANALYZER_EXIT_CODE=$?

cd ..

echo ""

# Mostra il risultato
if [ $ANALYZER_EXIT_CODE -eq 0 ]; then
    echo "✅ Analisi completata con successo - Nessun conflitto rilevato!"
else
    echo "⚠️  Analisi completata - Conflitti rilevati!"
    echo "Consulta il file $OUTPUT_FILE per i dettagli"
fi

echo ""
echo "📊 Risultati salvati in: $OUTPUT_FILE"
echo "📋 Consulta SWAGGER_ROUTE_CONFLICTS_CHECKLIST.md per la procedura di risoluzione"

exit $ANALYZER_EXIT_CODE