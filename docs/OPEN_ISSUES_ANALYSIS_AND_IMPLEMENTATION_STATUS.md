# 📊 Analisi Issue Aperte e Stato di Implementazione - EventForge

> **Obiettivo**: Analizzare tutte le issue aperte (21), accorparle per tema e verificarne lo stato di implementazione, creando documentazione completa per lo stato di avanzamento.

---

## 📋 Executive Summary

**Data Analisi**: Gennaio 2025  
**Issue Aperte Totali**: 21  
**Temi Principali**: 6 macro-aree identificate  
**Epics Completate**: 3 (Epic #178, #274, #276)  
**Stato Generale**: Architettura consolidata, focus su feature avanzate

---

## 🎯 Categorizzazione Tematica delle Issue Aperte

### 1. 🏭 **StationMonitor & Gestione Ordini** 
**Issue**: #317 | **Stato**: 🔴 NON IMPLEMENTATO | **Priorità**: ALTA

#### Descrizione
Estensione e miglioramento della gestione ordini cucina/bar tramite StationMonitor per tracciamento FIFO, concorrenza, assegnazioni e notifiche real-time.

#### Stato Implementazione Corrente
- ✅ **Base Esistente**: `StationOrderQueueItem` con campi base (StationId, DocumentHeaderId, ProductId, Status, AssignedAt, StartedAt, CompletedAt)
- ✅ **Enum Status**: Workflow base (Waiting, Accepted, InPreparation, Ready, Delivered, Cancelled)
- ❌ **Mancante**: Identificazione cliente/servizio, concorrenza optimistic, priorità/SLA, SignalR integration

#### Componenti da Implementare
- **Entity Extensions**: 
  - `AssignedToUserId`, `SourcePosId`, `TicketNumber`, `TableNumber`, `CustomerName`
  - `Priority`, `EstimatedPrepTime`, `CancelledBy`, `CancelledReason`
  - `RowVersion` per optimistic concurrency
- **API Operations**: EnqueueItem, TryAssignItem, StartItem, MarkReady, DeliverItem, CancelItem
- **SignalR**: Eventi real-time per cambio stato
- **UI/UX**: Kitchen display con FIFO queue, colori stato/priorità, timer

#### Roadmap Stimata
1. **Settimana 1-2**: Entity extensions + migration
2. **Settimana 3-4**: Atomic operations + concurrency
3. **Settimana 5-6**: SignalR integration + API
4. **Settimana 7-8**: UI/UX + testing

---

### 2. 🖼️ **Gestione Immagini e DocumentReference** 
**Issue**: #315, ~~#314~~ | **Stato**: 🟡 PARZIALMENTE IMPLEMENTATO (#314 ✅ COMPLETATO) | **Priorità**: MEDIA

#### Descrizione
Standardizzazione gestione immagini per Store entities (StoreUser, StoreUserGroup, StorePos) e Product tramite sistema DocumentReference unificato.

#### Stato Implementazione Corrente
- ✅ **DocumentReference Esistente**: Implementato per Team/TeamMember con supporto completo
  - Supporta OwnerType/OwnerId pattern
  - Gestione thumbnail, storage, signed URLs
  - MIME type validation, file size limits
- ✅ **Product Entity**: Ha campo `ImageUrl` (string) - **DEPRECATO** ma mantenuto per backward compatibility
- ✅ **Product DocumentReference**: ✅ COMPLETATO (Issue #314)
  - Entity: Product.ImageDocumentId + ImageDocument navigation property
  - Migration: 20251001060806_AddImageDocumentToProduct
  - API: POST/GET/DELETE `/api/v1/products/{id}/image`
  - DTOs: ProductDto, CreateProductDto, UpdateProductDto, ProductDetailDto
  - Tests: 9 unit tests (164 total)
- ❌ **Store Entities**: Non hanno gestione immagini

#### Implementazione Richiesta

##### ✅ Issue #314 - Product Images (COMPLETATO)
- ✅ **Entity Changes**: 
  - ✅ Aggiunto `ImageDocumentId` (Guid?), `ImageDocument` navigation property
  - ✅ Deprecato `ImageUrl` field (mantenuto per backward compatibility)
- ✅ **API Endpoints**: POST/GET/DELETE `/api/v1/products/{id}/image`
- ✅ **DTO Updates**: Esposti `ImageDocumentId`, `ImageUrl`, `ThumbnailUrl`
- ✅ **Database Migration**: 20251001060806_AddImageDocumentToProduct
- ✅ **Unit Tests**: 9 tests passing
- ✅ **Service Implementation**: UploadProductImageAsync, GetProductImageDocumentAsync, DeleteProductImageAsync

##### Issue #315 - Store Entities Images (IN PROGRESS)
- **StoreUser**: `PhotoDocumentId`, `PhotoConsent`, `PhotoConsentAt`, `PhoneNumber`, `LastPasswordChangedAt`, `TwoFactorEnabled`
- **StoreUserGroup**: `LogoDocumentId`, `ColorHex`, `IsSystemGroup`, `IsDefault`
- **StorePos**: `ImageDocumentId`, `TerminalIdentifier`, `IPAddress`, `IsOnline`, `LastSyncAt`, `LocationLatitude/Longitude`
- **StoreUserPrivilege**: `IsSystemPrivilege`, `DefaultAssigned`, `Resource`, `Action`, `PermissionKey`

#### Roadmap Stimata
1. ✅ **Settimana 1**: Product DocumentReference integration (COMPLETATO)
2. **Settimana 2-3**: Store entities extensions + migration
3. **Settimana 4**: Store API endpoints implementation
4. **Settimana 5**: UI integration + testing

---

### 3. 🧙‍♂️ **Wizard Multi-step e UI Vendita**
**Issue**: #277 (Epic), #267, #262, #261 | **Stato**: 🔴 NON IMPLEMENTATO | **Priorità**: ALTA

#### Descrizione
Epic completo per wizard multi-step creazione documenti e refactoring UI vendita con ottimizzazione UX/performance.

#### Componenti Principali

##### #267 - Wizard Multi-step Documenti (Backend)
- **Architettura**: Frontend admin + backend API, gestione multi-serie, permessi granulari
- **Steps**: Tipo documento → Serie → Dati generali → Logistica → Righe → Riepilogo → Approvazione
- **Features**: Stato bozza, cleanup automatico, validazioni centralizzate, audit log

##### #262 - Progettazione UI Wizard Vendita
- **Flusso**: Autenticazione → Tipologia vendita → Prodotti → Pagamento → Stampa → Reset
- **UI**: Touch-first, layout personalizzabile, dashboard operatore
- **Device Support**: Tablet, monitor POS, mobile

##### #261 - Refactoring Wizard Frontend Vendita
- **Modelli**: SaleSession, SaleSessionStatus, PaymentMethod, SessionNote, TableSession
- **Features Avanzate**: Split/merge tavoli, multi-pagamento, sandbox admin, algoritmi smart

#### Stato Implementazione Corrente
- ❌ **Wizard Documents**: Non implementato
- ❌ **Sales UI**: Struttura base presente ma non wizard completo
- ❌ **Touch Interface**: Non implementato

#### Roadmap Stimata
1. **Mese 1**: Backend wizard documenti + API
2. **Mese 2**: UI design + prototyping
3. **Mese 3**: Frontend vendita refactoring
4. **Mese 4**: Integration + testing

---

### 4. 📄 **Document Management Avanzato**
**Issue**: #248, #250, #251, #253, #255, #256, #257 | **Stato**: 🟡 PARZIALMENTE IMPLEMENTATO | **Priorità**: BASSA

#### Descrizione
Suite completa di funzionalità avanzate per gestione documentale: workflow, collaborazione, AI, privacy, integrazione.

#### Stato Base Documentale
- ✅ **Entità Core**: DocumentHeader, DocumentRow, DocumentType con workflow base
- ✅ **API REST**: CRUD completo, operazioni business
- ✅ **Relazioni**: Magazzino, promozioni, listini, business party
- ❌ **Features Avanzate**: Tutte da implementare

#### Features da Implementare
- **#250 - Allegati Evoluti**: OCR, firma elettronica, multi-formato, cloud storage
- **#251 - Collaborazione**: Chat/commenti, task assignment, timeline attività
- **#253 - Document Intelligence**: AI suggerimenti, automazione, analisi predittiva
- **#255 - Layout/Export**: Editor visuale template, branding, formati multipli
- **#256 - Integrazione Esterna**: Webhook, sync ERP/CRM, sistemi fiscali
- **#257 - Privacy/Sicurezza**: Crittografia, retention GDPR, logging accessi

#### Priorità Implementazione
1. **ALTA**: #250 (Allegati), #251 (Collaborazione)
2. **MEDIA**: #255 (Layout), #256 (Integrazione)
3. **BASSA**: #253 (AI), #257 (Privacy avanzata)

---

### 5. 💰 **Gestione Prezzi e Unità di Misura**
**Issue**: #245, #244 | **Stato**: 🟡 PARZIALMENTE IMPLEMENTATO | **Priorità**: MEDIA

#### Descrizione
Ottimizzazione gestione listini prezzi e unità di misura con conversioni decimali.

#### Stato Implementazione Corrente
- ✅ **PriceList Base**: Entità PriceList/PriceListEntry con priorità, validità, stato
- ✅ **UM Base**: ProductUnit con conversion factor (attualmente int)
- ❌ **Conversion Decimale**: ConversionFactor deve essere decimal
- ❌ **Arrotondamento**: MidpointRounding.AwayFromZero non implementato

#### Implementazione Richiesta

##### #244 - Unit of Measure Evolution
- **Entity Change**: ConversionFactor da int a decimal
- **Logic Update**: Math.Round con AwayFromZero policy
- **Migration**: DB schema update per conversion factor
- **Validation**: Supporto valori decimali

##### #245 - Price List Optimization
- **Performance**: Query optimization, caching, proiezioni
- **Precedence**: Documentazione regole precedenza listini
- **Import/Export**: Bulk operations con validazione
- **API**: Prezzo applicato, history prezzi
- **Documentation**: Esempi pratici, best practices

#### Roadmap Stimata
- **Settimana 1-2**: UM decimal conversion + migration
- **Settimana 3-4**: PriceList optimization + API
- **Settimana 5**: Testing + documentation

---

### 6. 📦 **Inventory & Traceability Avanzato**
**Issue**: #239, #240, #241, #242, #243 | **Stato**: 🔴 NON IMPLEMENTATO | **Priorità**: BASSA

#### Descrizione
Sistema completo di tracciabilità prodotti con lotti/matricole e gestione magazzino avanzata.

#### Scope Completo
- **#239**: Multi-lotto, storico, avvisi, barcode, reportistica
- **#240**: Tracciabilità per magazzino, documenti qualità, provenienza, resi, manutenzione
- **#241**: Stock avanzato, scorte min/max, ottimizzazione, multi-azienda, dispositivi fisici
- **#242**: Integrazione tracciabilità-magazzino, workflow validazione, FEFO, dashboard
- **#243**: Reverse logistics, manutenzioni, commesse, sostenibilità

#### Stato Implementazione Corrente
- ✅ **Warehouse Base**: Entità Warehouse, Stock base
- ❌ **Lot/Serial Tracking**: Non implementato
- ❌ **Location Management**: Non implementato
- ❌ **Traceability**: Non implementato

#### Roadmap Stimata (Long-term)
- **Fase 1** (2-3 mesi): Base lot/serial tracking
- **Fase 2** (2-3 mesi): Warehouse locations + movements
- **Fase 3** (3-4 mesi): Advanced features + integrations

---

## 📊 Matrice di Priorità e Impatto

| Tema | Issue | Priorità | Impatto Business | Complessità | Stima Effort |
|------|-------|----------|------------------|-------------|--------------|
| StationMonitor | #317 | 🔴 ALTA | ALTO | MEDIA | 8 settimane |
| Wizard UI | #277 | 🔴 ALTA | ALTO | ALTA | 16 settimane |
| Image Management | #314,#315 | 🟡 MEDIA | MEDIO | BASSA | 5 settimane |
| Price/UM | #244,#245 | 🟡 MEDIA | MEDIO | BASSA | 5 settimane |
| Document Advanced | #248-257 | 🟢 BASSA | MEDIO | ALTA | 20+ settimane |
| Inventory/Trace | #239-243 | 🟢 BASSA | ALTO | MOLTO ALTA | 30+ settimane |

---

## 🏗️ Stato Architetturale Generale

### ✅ Fondazioni Completate
- **Epic #178**: Architettura "Indestructible" .NET (95/100 score)
- **Epic #274**: Backend Refactoring unificato (100% complete)
- **Epic #276**: Cart & Promotions sistema (100% complete)

### 🎯 Aree di Focus Immediate
1. **StationMonitor Enhancement** (#317) - Operatività cucina/bar
2. **Image Management Standardization** (#314, #315) - UX consistency
3. **Price/UM Optimization** (#244, #245) - Business logic accuracy

### 📈 Evoluzione Prevista
- **Q1 2025**: Focus operatività (Station, Images, Prices)
- **Q2 2025**: Wizard multi-step implementation
- **Q3-Q4 2025**: Document management avanzato
- **2026**: Inventory/Traceability sistema completo

---

## 📝 Raccomandazioni Strategiche

### 🚀 Priorità Immediate (Q1 2025)
1. **StationMonitor** (#317): Implementazione completa per operatività cucina/bar
2. **Image Management** (#314, #315): Standardizzazione sistema DocumentReference
3. **Unit/Price Optimization** (#244, #245): Correzioni business logic critiche

### 🎯 Priorità Medie (Q2 2025)
1. **Wizard Multi-step** (#277): Epic completo UI vendita e documenti
2. **Document Collaboration** (#250, #251): Features collaborazione base

### 📋 Priorità Future (Q3+ 2025)
1. **Document Intelligence** (#253): AI e automazione
2. **Inventory Advanced** (#239-243): Sistema tracciabilità completo

### 🔧 Considerazioni Tecniche
- **Performance**: Tutti i nuovi sviluppi devono includere caching e optimization
- **Testing**: Coverage obbligatorio per nuove features
- **Documentation**: API docs e user guides per ogni release
- **Migration**: Strategia backward compatibility per DB changes

---

## 📊 Dashboard di Monitoraggio

### KPI Implementazione
- **Issue Chiuse**: 0/21 (0%)
- **Epic Attivi**: 1 (#277)
- **Tema con Maggiore Priorità**: StationMonitor + Wizard UI
- **Effort Totale Stimato**: 84+ settimane di sviluppo

### Milestone Target
- **Marzo 2025**: StationMonitor + Images + Price/UM complete
- **Giugno 2025**: Wizard Multi-step MVP
- **Dicembre 2025**: Document Management avanzato
- **Giugno 2026**: Inventory/Traceability completo

---

*Documento generato automaticamente - Ultimo aggiornamento: Gennaio 2025*