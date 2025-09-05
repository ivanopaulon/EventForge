# 🔍 Analisi Tecnica Dettagliata Issue Aperte - EventForge

> **Dettaglio tecnico completo** per l'implementazione delle 21 issue aperte organizzate per tema con roadmap di sviluppo, analisi di impatto e requisiti implementativi.

---

## 📋 Indice

1. [StationMonitor & Gestione Ordini](#1-stationmonitor--gestione-ordini)
2. [Gestione Immagini e DocumentReference](#2-gestione-immagini-e-documentreference)
3. [Wizard Multi-step e UI Vendita](#3-wizard-multi-step-e-ui-vendita)
4. [Document Management Avanzato](#4-document-management-avanzato)
5. [Gestione Prezzi e Unità di Misura](#5-gestione-prezzi-e-unità-di-misura)
6. [Inventory & Traceability Avanzato](#6-inventory--traceability-avanzato)

---

## 1. 🏭 StationMonitor & Gestione Ordini

### Issue #317 - StationMonitor Enhancement

#### 📊 Analisi Stato Corrente

**Entità Esistenti:**
```csharp
// ✅ ESISTENTE
public class StationOrderQueueItem : AuditableEntity
{
    public Guid StationId { get; set; }
    public Guid DocumentHeaderId { get; set; }
    public Guid? DocumentRowId { get; set; }
    public Guid? TeamMemberId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public StationOrderQueueStatus Status { get; set; }
    public int SortOrder { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

// ✅ ESISTENTE
public enum StationOrderQueueStatus
{
    Waiting, Accepted, InPreparation, Ready, Delivered, Cancelled
}
```

#### 🚧 Modifiche Richieste

**1. Entity Extensions:**
```csharp
public class StationOrderQueueItem : AuditableEntity
{
    // ✅ Campi esistenti...
    
    // 🆕 NUOVI CAMPI - Identificazione Cliente/Servizio
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public Guid? SourcePosId { get; set; }
    public Guid? SourceStoreUserId { get; set; }
    public string? TicketNumber { get; set; }
    public int? TableNumber { get; set; }
    public string? CustomerName { get; set; }
    
    // 🆕 NUOVI CAMPI - Priorità e SLA
    public int Priority { get; set; } = 0;
    public int? EstimatedPrepTime { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
    
    // 🆕 NUOVI CAMPI - Cancellazione e Audit
    public Guid? CancelledBy { get; set; }
    public string? CancelledReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool IsArchived { get; set; } = false;
    
    // 🆕 NUOVI CAMPI - Concorrenza
    [Timestamp]
    public byte[] RowVersion { get; set; }
    
    // 🆕 NAVIGATION PROPERTIES
    public StoreUser? AssignedToUser { get; set; }
    public StorePos? SourcePos { get; set; }
    public StoreUser? SourceStoreUser { get; set; }
    public StoreUser? CancelledByUser { get; set; }
}
```

**2. Database Migration:**
```sql
-- Migration: AddStationMonitorEnhancements
ALTER TABLE StationOrderQueueItems 
ADD AssignedToUserId UNIQUEIDENTIFIER NULL,
    AssignedToUserName NVARCHAR(100) NULL,
    SourcePosId UNIQUEIDENTIFIER NULL,
    SourceStoreUserId UNIQUEIDENTIFIER NULL,
    TicketNumber NVARCHAR(50) NULL,
    TableNumber INT NULL,
    CustomerName NVARCHAR(100) NULL,
    Priority INT NOT NULL DEFAULT 0,
    EstimatedPrepTime INT NULL,
    RetryCount INT NOT NULL DEFAULT 0,
    LastError NVARCHAR(500) NULL,
    CancelledBy UNIQUEIDENTIFIER NULL,
    CancelledReason NVARCHAR(200) NULL,
    CancelledAt DATETIME2 NULL,
    IsArchived BIT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION NOT NULL;

-- Indici per performance
CREATE INDEX IX_StationOrderQueueItems_Status_StationId 
ON StationOrderQueueItems (Status, StationId);

CREATE INDEX IX_StationOrderQueueItems_AssignedToUserId 
ON StationOrderQueueItems (AssignedToUserId);

CREATE INDEX IX_StationOrderQueueItems_IsArchived 
ON StationOrderQueueItems (IsArchived);

-- Foreign Keys
ALTER TABLE StationOrderQueueItems
ADD CONSTRAINT FK_StationOrderQueueItems_AssignedToUser
FOREIGN KEY (AssignedToUserId) REFERENCES StoreUsers(Id);

ALTER TABLE StationOrderQueueItems
ADD CONSTRAINT FK_StationOrderQueueItems_SourcePos
FOREIGN KEY (SourcePosId) REFERENCES StorePos(Id);
```

#### ⏱️ Timeline Implementazione

- **Week 1**: Entity extensions + migration
- **Week 2**: Service layer + atomic operations  
- **Week 3**: SignalR integration + API
- **Week 4**: UI/UX kitchen display
- **Week 5**: Testing + performance optimization
- **Week 6**: Documentation + deployment

---

## 📊 Riepilogo Effort Stimato per Tema

| Tema | Issue | Complessità | Effort (Settimane) | Dipendenze |
|------|-------|-------------|-------------------|------------|
| StationMonitor | #317 | Media | 6-8 | SignalR, Concurrency |
| Image Management | #314,#315 | Bassa | 4-5 | DocumentReference |
| Wizard Epic | #277 | Alta | 16-20 | UI Framework, Touch |
| Document Advanced | #248-257 | Alta | 20-25 | AI, Workflow, Integration |
| Price/UM | #244,#245 | Bassa | 4-5 | DB Migration |
| Inventory/Trace | #239-243 | Molto Alta | 30-35 | Complex Domain Logic |

**Total Effort Estimate: 80-98 settimane di sviluppo**

---

*Documento aggiornato: Gennaio 2025*