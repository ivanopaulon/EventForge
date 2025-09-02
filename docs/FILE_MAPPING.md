# Documentation File Mapping

Questo file mantiene la mappatura tra i file di documentazione originali e la loro nuova posizione nella struttura organizzata.

## 📋 Mappatura File

### Core Documentation
| File Originale | Nuova Posizione | Descrizione |
|---|---|---|
| `README.md` | `README.md` + `docs/core/README.md` | Documentazione principale + panoramica progetto |
| - | `docs/core/getting-started.md` | Guida rapida (nuovo) |
| - | `docs/core/project-structure.md` | Struttura progetto (nuovo) |

### Backend Documentation
| File Originale | Nuova Posizione | Descrizione |
|---|---|---|
| `BACKEND_REFACTORING_GUIDE.md` | `docs/backend/refactoring-guide.md` | Guida refactoring backend |
| `API_ENDPOINT_MIGRATION_SUMMARY.md` | `docs/backend/api-development.md` | Sviluppo e migrazione API |
| `SUPERADMIN_IMPLEMENTATION.md` | `docs/backend/SUPERADMIN_IMPLEMENTATION.md` | Implementazione SuperAdmin |
| `NET_INDESTRUCTIBLE_ARCHITECTURE_SUMMARY.md` | `docs/backend/NET_INDESTRUCTIBLE_ARCHITECTURE_SUMMARY.md` | Architettura .NET |

### Frontend Documentation
| File Originale | Nuova Posizione | Descrizione |
|---|---|---|
| `UI_UX_LAYOUT_GUIDELINES.md` | `docs/frontend/ui-guidelines.md` | Linee guida UI/UX |
| `TRANSLATION_GUIDE.md` | `docs/frontend/translation.md` | Sistema traduzione |
| `CUSTOM_THEME_GUIDE.md` | `docs/frontend/theming.md` | Sistema temi |
| `HTTPCLIENT_BEST_PRACTICES.md` | `docs/frontend/HTTPCLIENT_BEST_PRACTICES.md` | Best practice HttpClient |
| `HTTPCLIENT_WEBASSEMBLY_FIX.md` | `docs/frontend/HTTPCLIENT_WEBASSEMBLY_FIX.md` | Fix WebAssembly |
| `MUDBLAZOR_PERFORMANCE_OPTIMIZATION.md` | `docs/frontend/MUDBLAZOR_PERFORMANCE_OPTIMIZATION.md` | Ottimizzazione MudBlazor |
| `BOOTSTRAP_SYSTEM_GUIDE.md` | `docs/frontend/BOOTSTRAP_SYSTEM_GUIDE.md` | Sistema Bootstrap |
| `DRAWER_IMPLEMENTATION_GUIDE.md` | `docs/frontend/DRAWER_IMPLEMENTATION_GUIDE.md` | Implementazione Drawer |
| `FRONTEND_ALIGNMENT_CHECKLIST.md` | `docs/frontend/FRONTEND_ALIGNMENT_CHECKLIST.md` | Checklist allineamento |
| `MANAGEMENT_PAGES_IMPROVEMENTS.md` | `docs/frontend/MANAGEMENT_PAGES_IMPROVEMENTS.md` | Miglioramenti pagine |

### Testing Documentation
| File Originale | Nuova Posizione | Descrizione |
|---|---|---|
| `SWAGGER_ROUTE_CONFLICTS_CHECKLIST.md` | `docs/testing/route-analysis.md` | Analisi conflitti route |
| `ROUTE_ANALYSIS_COMPREHENSIVE_REPORT.md` | `docs/testing/ROUTE_ANALYSIS_COMPREHENSIVE_REPORT.md` | Report analisi route |
| `audit/` | `docs/testing/audit/` | Sistema audit completo |

### Deployment Documentation
| File Originale | Nuova Posizione | Descrizione |
|---|---|---|
| `DEPLOYMENT_GUIDE.md` | `docs/deployment/deployment-guide.md` | Guida deployment |
| `LICENSING_SYSTEM_GUIDE.md` | `docs/deployment/licensing.md` | Sistema licenze |
| `LICENSING_TESTING_GUIDE.md` | `docs/deployment/LICENSING_TESTING_GUIDE.md` | Testing licenze |

### Feature Documentation
| File Originale | Nuova Posizione | Descrizione |
|---|---|---|
| `NOTIFICATIONS_CHAT_DATA_MODEL.md` | `docs/features/NOTIFICATIONS_CHAT_DATA_MODEL.md` | Modello dati chat |
| `NOTIFICATIONS_CHAT_IMPLEMENTATION.md` | `docs/features/NOTIFICATIONS_CHAT_IMPLEMENTATION.md` | Implementazione chat |
| `NOTIFICATIONS_CHAT_UI_IMPLEMENTATION.md` | `docs/features/NOTIFICATIONS_CHAT_UI_IMPLEMENTATION.md` | UI chat |
| `BARCODE_INTEGRATION_GUIDE.md` | `docs/features/BARCODE_INTEGRATION_GUIDE.md` | Integrazione barcode |
| `BARCODE_CROSS_PLATFORM_GUIDE.md` | `docs/features/BARCODE_CROSS_PLATFORM_GUIDE.md` | Barcode cross-platform |
| `QZ_PRINTING_INTEGRATION_GUIDE.md` | `docs/features/QZ_PRINTING_INTEGRATION_GUIDE.md` | Integrazione stampa |
| `QZ_TRAY_SIGNATURE_README.md` | `docs/features/QZ_TRAY_SIGNATURE_README.md` | Signature QZ Tray |
| `PROMOTIONS_ENGINE.md` | `docs/features/PROMOTIONS_ENGINE.md` | Motore promozioni |
| `SIGNALR_IMPLEMENTATION_STEP1.md` | `docs/features/SIGNALR_IMPLEMENTATION_STEP1.md` | Implementazione SignalR |
| `RETAIL_CART_SESSION.md` | `docs/features/RETAIL_CART_SESSION.md` | Sessione carrello |

### Migration Documentation
| File Originale | Nuova Posizione | Descrizione |
|---|---|---|
| `BACKEND_REFACTORING_IMPLEMENTATION_SUMMARY.md` | `docs/migration/BACKEND_REFACTORING_IMPLEMENTATION_SUMMARY.md` | Riassunto refactoring |
| `CONTROLLER_REFACTORING_COMPLETION.md` | `docs/migration/CONTROLLER_REFACTORING_COMPLETION.md` | Completamento controller |
| `CONTROLLER_REFACTORING_SUMMARY.md` | `docs/migration/CONTROLLER_REFACTORING_SUMMARY.md` | Riassunto controller |
| `CONTROLLER_REFACTORING_MIGRATION_GUIDE.md` | `docs/migration/CONTROLLER_REFACTORING_MIGRATION_GUIDE.md` | Migrazione controller |
| `DTO_REORGANIZATION_SUMMARY.md` | `docs/migration/DTO_REORGANIZATION_SUMMARY.md` | Riorganizzazione DTO |
| `DTO_REVIEW_SUMMARY.md` | `docs/migration/DTO_REVIEW_SUMMARY.md` | Review DTO |
| `MULTI_TENANT_REFACTORING_COMPLETION.md` | `docs/migration/MULTI_TENANT_REFACTORING_COMPLETION.md` | Completamento multi-tenant |
| `EPIC_275_IMPLEMENTATION_COMPLETE.md` | `docs/migration/EPIC_275_IMPLEMENTATION_COMPLETE.md` | Completamento Epic 275 |
| `ISSUE_178_COMPLETION_SUMMARY.md` | `docs/migration/ISSUE_178_COMPLETION_SUMMARY.md` | Completamento Issue 178 |
| `DBCONTEXT_FIXES_SUMMARY.md` | `docs/migration/DBCONTEXT_FIXES_SUMMARY.md` | Fix DbContext |
| `CLIENT_LOGGING_IMPLEMENTATION.md` | `docs/migration/CLIENT_LOGGING_IMPLEMENTATION.md` | Implementazione logging |

## 🔄 Link Updates Required

### Internal References to Update
1. **README.md**: Aggiornare link alla documentazione
2. **Cross-references**: Aggiornare link tra documenti
3. **Script references**: Aggiornare percorsi negli script
4. **CI/CD**: Aggiornare percorsi nei workflow

### Common Link Patterns to Update
- `[Link](./DOCUMENT.md)` → `[Link](./docs/category/document.md)`
- `[Link](DOCUMENT.md)` → `[Link](docs/category/document.md)`
- Relative references tra categorie

### Script Updates Required
- `analyze-routes.sh`: Verificare percorsi output
- `audit/run-audit.sh`: Verificare percorsi report
- CI/CD workflows: Aggiornare percorsi documentazione

## 📁 New Directory Structure Summary

```
docs/
├── README.md                           # Indice principale documentazione
├── core/                               # Documentazione core
│   ├── README.md                      # Project overview
│   ├── getting-started.md             # Guida rapida
│   └── project-structure.md           # Struttura progetto
├── backend/                            # Documentazione backend
│   ├── README.md
│   ├── refactoring-guide.md
│   ├── api-development.md
│   ├── SUPERADMIN_IMPLEMENTATION.md
│   └── NET_INDESTRUCTIBLE_ARCHITECTURE_SUMMARY.md
├── frontend/                           # Documentazione frontend
│   ├── README.md
│   ├── ui-guidelines.md
│   ├── translation.md
│   ├── theming.md
│   └── [altri file frontend]
├── testing/                            # Documentazione testing
│   ├── README.md
│   ├── route-analysis.md
│   ├── audit/                         # Sistema audit
│   └── ROUTE_ANALYSIS_COMPREHENSIVE_REPORT.md
├── deployment/                         # Documentazione deployment
│   ├── README.md
│   ├── deployment-guide.md
│   ├── licensing.md
│   └── LICENSING_TESTING_GUIDE.md
├── features/                           # Guide funzionalità
│   ├── README.md
│   └── [file funzionalità specifiche]
└── migration/                          # Report migrazioni
    ├── README.md
    └── [report completamento e migrazioni]
```

## ✅ Next Steps

1. **Update Cross-References**: Aggiornare tutti i link interni
2. **Remove Duplicates**: Rimuovere file duplicati dalla root
3. **Update Scripts**: Aggiornare percorsi negli script
4. **Validate Links**: Verificare tutti i link funzionino
5. **Update CI/CD**: Aggiornare workflow se necessario