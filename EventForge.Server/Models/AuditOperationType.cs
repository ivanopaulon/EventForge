public enum AuditOperationType
{
    // ... altri valori già presenti ...
    TenantStatusChanged,
    AdminTenantGranted,
    AdminTenantRevoked,
    // AGGIUNGERE I SEGUENTI:
    TenantCreated,
    TenantUpdated,
    ForcePasswordChange
}