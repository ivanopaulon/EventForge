# Health Dialog Translation Keys

This document lists all the translation keys used in the enhanced Health Status Dialog that may need to be added to the translation files.

## New Translation Keys

The following keys should be added to all language files (e.g., `en.json`, `it.json`, etc.):

### Session Management Keys

```json
{
  "health.currentSession": "Current Session",
  "health.activeSessions": "Active Sessions",
  "health.sessionsActive": "active session(s)",
  "health.terminateOtherSessions": "Terminate Other Sessions",
  "health.session": "Session",
  "health.device": "Device",
  "health.location": "Location",
  "health.lastActivity": "Last Activity",
  "health.viewDetails": "View Details",
  "health.terminateSession": "Terminate",
  "health.sessionDetails": "Session Details",
  "health.sessionId": "Session ID",
  "health.ipAddress": "IP Address",
  "health.userAgent": "User Agent",
  "health.browser": "Browser",
  "health.operatingSystem": "Operating System",
  "health.deviceType": "Device Type",
  "health.loginTime": "Login Time",
  "health.isCurrentSession": "Is Current Session",
  "health.tokenExpiry": "Token Expiry",
  "health.tokenTimeRemaining": "Time Remaining",
  "health.permissionsGranted": "permissions granted",
  "health.expired": "Expired",
  "health.sessionTerminated": "Session terminated successfully",
  "health.sessionTerminationFailed": "Failed to terminate session",
  "health.errorTerminatingSession": "Error terminating session: {0}",
  "health.confirmTermination": "Confirm Termination",
  "health.confirmTerminateAllOtherSessions": "Are you sure you want to terminate all other sessions? This action cannot be undone.",
  "health.allOtherSessionsTerminated": "All other sessions terminated successfully",
  "health.terminationFailed": "Failed to terminate other sessions",
  "health.errorTerminatingSessions": "Error terminating sessions: {0}",
  "health.sessionsNotAvailable": "Session information is not available",
  "health.noActiveSessions": "No active sessions"
}
```

## Italian Translations (Suggested)

```json
{
  "health.currentSession": "Sessione Corrente",
  "health.activeSessions": "Sessioni Attive",
  "health.sessionsActive": "sessione/i attiva/e",
  "health.terminateOtherSessions": "Termina Altre Sessioni",
  "health.session": "Sessione",
  "health.device": "Dispositivo",
  "health.location": "Posizione",
  "health.lastActivity": "Ultima Attività",
  "health.viewDetails": "Visualizza Dettagli",
  "health.terminateSession": "Termina",
  "health.sessionDetails": "Dettagli Sessione",
  "health.sessionId": "ID Sessione",
  "health.ipAddress": "Indirizzo IP",
  "health.userAgent": "User Agent",
  "health.browser": "Browser",
  "health.operatingSystem": "Sistema Operativo",
  "health.deviceType": "Tipo Dispositivo",
  "health.loginTime": "Ora di Login",
  "health.isCurrentSession": "È Sessione Corrente",
  "health.tokenExpiry": "Scadenza Token",
  "health.tokenTimeRemaining": "Tempo Rimanente",
  "health.permissionsGranted": "permessi concessi",
  "health.expired": "Scaduto",
  "health.sessionTerminated": "Sessione terminata con successo",
  "health.sessionTerminationFailed": "Impossibile terminare la sessione",
  "health.errorTerminatingSession": "Errore nella terminazione della sessione: {0}",
  "health.confirmTermination": "Conferma Terminazione",
  "health.confirmTerminateAllOtherSessions": "Sei sicuro di voler terminare tutte le altre sessioni? Questa azione non può essere annullata.",
  "health.allOtherSessionsTerminated": "Tutte le altre sessioni terminate con successo",
  "health.terminationFailed": "Impossibile terminare le altre sessioni",
  "health.errorTerminatingSessions": "Errore nella terminazione delle sessioni: {0}",
  "health.sessionsNotAvailable": "Le informazioni sulla sessione non sono disponibili",
  "health.noActiveSessions": "Nessuna sessione attiva"
}
```

## Existing Keys Used

These keys are already defined and used in other parts of the application:

- `field.username` - Username
- `field.fullName` - Full Name  
- `field.roles` - Roles
- `field.permissions` - Permissions
- `field.lastLogin` - Last Login
- `common.yes` - Yes
- `common.no` - No
- `common.all` - All
- `common.refresh` - Refresh
- `button.close` - Close
- `health.status` - Status
- `health.version` - Version
- `health.environment` - Environment
- `health.uptime` - Uptime
- `health.lastUpdated` - Last Updated
- `health.loading` - Loading health information...
- `health.systemHealth` - System Health
- `health.systemLogs` - System Logs
- `health.api.title` - API Status
- `health.database.title` - Database Status
- `health.migrations` - Migrations
- `health.actions` - Actions

## File Locations

Translation files are typically located in:
- `EventForge.Client/wwwroot/i18n/en.json` (English)
- `EventForge.Client/wwwroot/i18n/it.json` (Italian)
- Other language files as supported by the application

## Implementation Notes

1. **Parameterized Strings**: Some keys use parameter substitution (e.g., `{0}`)
   - Example: `"health.errorTerminatingSession": "Error terminating session: {0}"`
   - The `{0}` will be replaced with the actual error message at runtime

2. **Fallback Behavior**: The `TranslationService.GetTranslation()` method accepts a default value
   - If a key is not found, the default value is used
   - Example: `TranslationService.GetTranslation("health.currentSession", "Current Session")`

3. **Consistency**: Ensure all language files have the same set of keys for consistency

4. **Testing**: After adding translations, test the dialog in different languages to ensure:
   - All text displays correctly
   - No truncation or layout issues
   - Parameter substitution works as expected

## Migration Steps

1. **Identify Current Translation Files**:
   ```bash
   find EventForge.Client/wwwroot/i18n -name "*.json"
   ```

2. **Add New Keys**: Add all the new keys listed above to each language file

3. **Verify Syntax**: Ensure JSON is valid (no trailing commas, proper quotes)

4. **Test in UI**: Open the Health Dialog and verify all text appears correctly

5. **Multi-language Test**: Switch languages in the app and verify translations

## Priority Translation Languages

Based on typical EventForge deployment:
1. **Italian (it)** - Primary language (high priority)
2. **English (en)** - International/default (high priority)
3. Other languages as supported

## Future Enhancements

Consider adding these optional keys for enhanced UX:

```json
{
  "health.sessionDuration": "Session Duration",
  "health.refreshSessions": "Refresh Sessions",
  "health.exportSessions": "Export Sessions",
  "health.filterSessions": "Filter Sessions",
  "health.sortBy": "Sort By",
  "health.deviceTypeDesktop": "Desktop",
  "health.deviceTypeMobile": "Mobile",
  "health.deviceTypeTablet": "Tablet",
  "health.deviceTypeUnknown": "Unknown"
}
```

## Validation Checklist

- [ ] All new keys added to `en.json`
- [ ] All new keys added to `it.json`
- [ ] JSON files are valid (no syntax errors)
- [ ] Tested in English language
- [ ] Tested in Italian language
- [ ] Parameter substitution tested (`{0}` placeholders)
- [ ] Responsive layout tested with translated text
- [ ] No text truncation or overflow issues
- [ ] Confirmation dialogs display correctly in all languages
- [ ] Snackbar messages display correctly in all languages

## Support Resources

For translation assistance:
- Use Google Translate as a starting point
- Consult with native speakers for accuracy
- Review existing translations in the codebase for consistency
- Consider cultural nuances in confirmations and warnings
