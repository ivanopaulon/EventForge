public enum AuditOperationType
{
    // ... altri valori gi� presenti ...
    TenantStatusChanged,
    AdminTenantGranted,
    AdminTenantRevoked,
    // AGGIUNGERE I SEGUENTI:
    TenantCreated,
    TenantUpdated,
    ForcePasswordChange
}