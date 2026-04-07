# Testing & Quality Assurance Documentation

Documentazione completa per testing e controllo qualità di EventForge.

## 📋 Indice

### 🧪 Testing Framework
- [Testing Guide](./testing-guide.md) - Guida completa ai test
- [Unit Testing](./unit-testing.md) - Test unitari
- [Integration Testing](./integration-testing.md) - Test di integrazione
- [End-to-End Testing](./e2e-testing.md) - Test end-to-end

### 🔍 Analysis & Monitoring
- [Route Analysis](./route-analysis.md) - Analisi conflitti route e Swagger
- [Performance Monitoring](./performance-monitoring.md) - Monitoraggio performance
- [Error Tracking](./error-tracking.md) - Tracciamento errori

### 🔍 Audit System
- [Audit System Overview](./audit/README.md) - Sistema di audit completo
- [Automated Auditing](./audit/automated-auditing.md) - Audit automatizzato
- [Manual Verification](./audit/manual-verification.md) - Verifica manuale
- [Audit Reports](./audit/audit-reports.md) - Report di audit

### 📊 Quality Assurance
- [QA Checklist](./quality-assurance.md) - Checklist controllo qualità
- [Code Quality Standards](./code-quality.md) - Standard qualità codice
- [Review Process](./review-process.md) - Processo di review
- [Compliance Testing](./compliance-testing.md) - Test di conformità

## 🚀 Quick Start per Testing

### Struttura Test
EventForge utilizza xUnit con categorizzazione tramite traits:

```bash
# Esegui tutti i test
dotnet test

# Test unitari
dotnet test --filter Category=Unit

# Test di integrazione
dotnet test --filter Category=Integration

# Analisi route
dotnet test --filter Category=RouteAnalysis
```

### Configurazione CI/CD
```yaml
# Esempio GitHub Actions
- name: Run Unit Tests
  run: dotnet test --filter Category=Unit --logger trx

- name: Run Integration Tests  
  run: dotnet test --filter Category=Integration --logger trx

- name: Analyze Routes
  run: dotnet test --filter Category=RouteAnalysis --logger trx
```

## 🔍 Route Analysis System

### Analisi Automatica
Lo script di analisi route rileva:
- **Conflitti route**: Route duplicate o ambigue
- **Mapping completo**: Tutte le route con HTTP methods
- **Soluzioni suggerite**: Correzioni per ogni conflitto
- **Statistiche**: Distribuzione route per controller

### Comandi Disponibili
```bash
# Analisi standard
./analyze-routes.sh

# Analisi personalizzata
./analyze-routes.sh "percorso/custom" "report.txt"

# Analisi tramite test
dotnet test --filter Category=RouteAnalysis
```

### Output Reports
- `route_analysis_report.txt` - Report dettagliato
- `simple_route_analysis.txt` - Analisi semplificata

## 🔧 Audit System

### Componenti Principali
- **CodebaseAuditor.cs** - Motore audit principale
- **MarkdownReportGenerator.cs** - Generatore report
- **Automated Tools** - Script automatizzati
- **Manual Checklists** - Verifiche manuali

### Esecuzione Audit
```bash
# Unix/Linux/macOS
./audit/run-audit.sh

# Windows
.\audit\EventForge-Audit.ps1

# .NET diretto
cd audit && dotnet run
```

### Report Generati
- **AUDIT_REPORT.md** - Report audit dettagliato
- **INTEGRATION_SUMMARY.md** - Riassunto esecutivo
- **MANUAL_VERIFICATION_CHECKLIST.md** - Checklist verifiche
- **SWAGGER_DIAGNOSTIC.md** - Diagnostica Swagger

## 📋 Quality Assurance Process

### Manual Testing Checklist
1. **Functional Testing**
   - Tutte le funzionalità principali
   - Flussi utente critici
   - Gestione errori

2. **UI/UX Testing**
   - Responsive design
   - Accessibilità (WCAG)
   - Cross-browser compatibility

3. **Performance Testing**
   - Tempi di caricamento
   - Utilizzo memoria
   - Scalabilità

4. **Security Testing**
   - Autenticazione/Autorizzazione
   - Input validation
   - Data protection

### Translation Completeness
- Verifica traduzioni complete per tutte le lingue
- Consistenza terminologica
- Context-appropriate translations
- Pluralization support

### SuperAdmin UI Consistency
- Layout uniformità
- Navigation consistency
- Permission enforcement
- Audit trail completeness

## 🔗 Collegamenti Utili

- [Backend Documentation](../backend/) - Documentazione backend
- [Frontend Documentation](../frontend/) - Documentazione frontend
- [Deployment Guide](../deployment/) - Guida deployment
- [Feature Testing](../features/) - Test funzionalità specifiche