# Backend Development Documentation

Documentazione completa per lo sviluppo backend di EventForge.

## 📋 Indice

### 🏗️ Architettura
- [Refactoring Guide](./refactoring-guide.md) - Guida completa al refactoring backend
- [Data Models & Entities](./data-models.md) - Modelli dati e entità
- [Services Architecture](./services-controllers.md) - Architettura servizi e controller

### 🔌 API Development
- [API Development Guide](./api-development.md) - Sviluppo e migrazione API
- [Endpoint Best Practices](./api-best-practices.md) - Best practice per endpoint
- [Authentication & Authorization](./auth-security.md) - Autenticazione e sicurezza

### 📊 Data & Persistence
- [Entity Framework Setup](./ef-setup.md) - Configurazione Entity Framework
- [Database Migrations](./migrations.md) - Gestione migrazioni database
- [Audit System](./audit-system.md) - Sistema di audit e logging

### 🔄 Services & Business Logic
- [Service Layer Patterns](./service-patterns.md) - Pattern layer servizi
- [CRUD Operations](./crud-operations.md) - Operazioni CRUD standard
- [Multi-Tenant Architecture](./multi-tenant.md) - Architettura multi-tenant

## 🚀 Quick Start per Backend Developers

1. **Setup Iniziale**: Leggi [Project Structure](../core/project-structure.md)
2. **Refactoring**: Inizia con [Refactoring Guide](./refactoring-guide.md)
3. **API Development**: Consulta [API Development Guide](./api-development.md)
4. **Testing**: Vai a [Testing Documentation](../testing/)

## 📝 Linee Guida di Sviluppo

### Convenzioni di Naming
- **Controller**: Suffisso `Controller` (es. `ProductsController`)
- **Service**: Suffisso `Service` (es. `ProductService`)
- **DTO**: Suffisso `Dto` (es. `ProductDto`)
- **Entity**: Nessun suffisso (es. `Product`)

### Pattern Architetturali
- **Repository Pattern**: Per accesso dati
- **Service Layer**: Per logica business
- **DTO Pattern**: Per trasferimento dati
- **CQRS**: Per operazioni complesse

### Gestione Errori
- Usa `IResult` per response API
- Implementa exception handling centralizzato
- Log tutti gli errori con dettagli contestuali
- Restituisci messaggi utente appropriati

## 🔗 Collegamenti Utili

- [Frontend Documentation](../frontend/) - Documentazione frontend
- [Testing Guidelines](../testing/) - Linee guida testing
- [Deployment Guide](../deployment/) - Guida deployment
- [Migration Reports](../migration/) - Report migrazioni