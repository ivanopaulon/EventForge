# Critical Missing Translations for EventForge

> **⚠️ UPDATE (2024):** Spanish and French language support has been removed from the project.
> The system now only supports Italian (it) and English (en), which have been fully aligned with 1615 keys each.
> This document is kept for historical reference but is no longer applicable.

This document contains translations for the most critical missing keys that should be added IMMEDIATELY to all 4 language files.

## Priority 1: Activity Feed (Complete Feature - 30 keys)

These translations enable the entire Activity Feed page (`Pages/Notifications/ActivityFeed.razor`).

### Italian (it.json) - Add to "activityFeed" section:
```json
"activityFeed": {
  "allTime": "Tutto il periodo",
  "allTypes": "Tutti i tipi",
  "apply": "Applica",
  "chat": "Chat",
  "daysAgo": "{0} giorni fa",
  "events": "Eventi",
  "filterByType": "Filtra per tipo",
  "filters": "Filtri",
  "filtersApplied": "Filtri applicati",
  "filtersTitle": "Filtri Attività",
  "hoursAgo": "{0} ore fa",
  "justNow": "Proprio ora",
  "loadMore": "Carica altro",
  "loading": "Caricamento...",
  "minutesAgo": "{0} minuti fa",
  "noActivities": "Nessuna attività",
  "noActivitiesDesc": "Non ci sono attività da visualizzare",
  "notifications": "Notifiche",
  "pageTitle": "Feed Attività",
  "refresh": "Aggiorna",
  "refreshed": "Aggiornato",
  "search": "Cerca attività...",
  "subtitle": "Visualizza tutte le attività recenti nel sistema",
  "system": "Sistema",
  "thisMonth": "Questo mese",
  "thisWeek": "Questa settimana",
  "timeRange": "Intervallo temporale",
  "title": "Feed Attività - EventForge",
  "today": "Oggi",
  "viewDetails": "Visualizza dettagli"
}
```

### English (en.json) - Add to "activityFeed" section:
```json
"activityFeed": {
  "allTime": "All time",
  "allTypes": "All types",
  "apply": "Apply",
  "chat": "Chat",
  "daysAgo": "{0} days ago",
  "events": "Events",
  "filterByType": "Filter by type",
  "filters": "Filters",
  "filtersApplied": "Filters applied",
  "filtersTitle": "Activity Filters",
  "hoursAgo": "{0} hours ago",
  "justNow": "Just now",
  "loadMore": "Load more",
  "loading": "Loading...",
  "minutesAgo": "{0} minutes ago",
  "noActivities": "No activities",
  "noActivitiesDesc": "There are no activities to display",
  "notifications": "Notifications",
  "pageTitle": "Activity Feed",
  "refresh": "Refresh",
  "refreshed": "Refreshed",
  "search": "Search activities...",
  "subtitle": "View all recent activities in the system",
  "system": "System",
  "thisMonth": "This month",
  "thisWeek": "This week",
  "timeRange": "Time range",
  "title": "Activity Feed - EventForge",
  "today": "Today",
  "viewDetails": "View details"
}
```

### Spanish (es.json) - Add to "activityFeed" section:
```json
"activityFeed": {
  "allTime": "Todo el tiempo",
  "allTypes": "Todos los tipos",
  "apply": "Aplicar",
  "chat": "Chat",
  "daysAgo": "hace {0} días",
  "events": "Eventos",
  "filterByType": "Filtrar por tipo",
  "filters": "Filtros",
  "filtersApplied": "Filtros aplicados",
  "filtersTitle": "Filtros de Actividad",
  "hoursAgo": "hace {0} horas",
  "justNow": "Justo ahora",
  "loadMore": "Cargar más",
  "loading": "Cargando...",
  "minutesAgo": "hace {0} minutos",
  "noActivities": "Sin actividades",
  "noActivitiesDesc": "No hay actividades para mostrar",
  "notifications": "Notificaciones",
  "pageTitle": "Feed de Actividad",
  "refresh": "Actualizar",
  "refreshed": "Actualizado",
  "search": "Buscar actividades...",
  "subtitle": "Ver todas las actividades recientes en el sistema",
  "system": "Sistema",
  "thisMonth": "Este mes",
  "thisWeek": "Esta semana",
  "timeRange": "Rango de tiempo",
  "title": "Feed de Actividad - EventForge",
  "today": "Hoy",
  "viewDetails": "Ver detalles"
}
```

### French (fr.json) - Add to "activityFeed" section:
```json
"activityFeed": {
  "allTime": "Toute la période",
  "allTypes": "Tous les types",
  "apply": "Appliquer",
  "chat": "Chat",
  "daysAgo": "il y a {0} jours",
  "events": "Événements",
  "filterByType": "Filtrer par type",
  "filters": "Filtres",
  "filtersApplied": "Filtres appliqués",
  "filtersTitle": "Filtres d'Activité",
  "hoursAgo": "il y a {0} heures",
  "justNow": "À l'instant",
  "loadMore": "Charger plus",
  "loading": "Chargement...",
  "minutesAgo": "il y a {0} minutes",
  "noActivities": "Aucune activité",
  "noActivitiesDesc": "Il n'y a aucune activité à afficher",
  "notifications": "Notifications",
  "pageTitle": "Flux d'Activité",
  "refresh": "Actualiser",
  "refreshed": "Actualisé",
  "search": "Rechercher des activités...",
  "subtitle": "Voir toutes les activités récentes du système",
  "system": "Système",
  "thisMonth": "Ce mois-ci",
  "thisWeek": "Cette semaine",
  "timeRange": "Plage de temps",
  "title": "Flux d'Activité - EventForge",
  "today": "Aujourd'hui",
  "viewDetails": "Voir les détails"
}
```

## Priority 2: Critical Common UI Keys

These are used across multiple pages and should be added to enable basic UI functionality.

### Add to all language files in the "common" section:

#### Italian additions to common:
```json
"all": "Tutti",
"applyFilters": "Applica filtri",
"clearFilters": "Cancella filtri",
"clearSelection": "Cancella selezione",
"create": "Crea",
"dataRefreshed": "Dati aggiornati",
"errorLoading": "Errore nel caricamento",
"filter": "Filtra",
"filtersApplied": "Filtri applicati",
"noRecords": "Nessun record",
"notImplemented": "Non implementato",
"notSpecified": "Non specificato",
"refreshed": "Aggiornato",
"resetToDefaults": "Ripristina predefiniti",
"saveSettings": "Salva impostazioni",
"settingsReset": "Impostazioni ripristinate",
"settingsSaved": "Impostazioni salvate"
```

#### English additions to common:
```json
"all": "All",
"applyFilters": "Apply filters",
"clearFilters": "Clear filters",
"clearSelection": "Clear selection",
"create": "Create",
"dataRefreshed": "Data refreshed",
"errorLoading": "Error loading",
"filter": "Filter",
"filtersApplied": "Filters applied",
"noRecords": "No records",
"notImplemented": "Not implemented",
"notSpecified": "Not specified",
"refreshed": "Refreshed",
"resetToDefaults": "Reset to defaults",
"saveSettings": "Save settings",
"settingsReset": "Settings reset",
"settingsSaved": "Settings saved"
```

#### Spanish additions to common:
```json
"all": "Todos",
"applyFilters": "Aplicar filtros",
"clearFilters": "Limpiar filtros",
"clearSelection": "Limpiar selección",
"create": "Crear",
"dataRefreshed": "Datos actualizados",
"errorLoading": "Error al cargar",
"filter": "Filtrar",
"filtersApplied": "Filtros aplicados",
"noRecords": "Sin registros",
"notImplemented": "No implementado",
"notSpecified": "No especificado",
"refreshed": "Actualizado",
"resetToDefaults": "Restablecer predeterminados",
"saveSettings": "Guardar configuración",
"settingsReset": "Configuración restablecida",
"settingsSaved": "Configuración guardada"
```

#### French additions to common:
```json
"all": "Tous",
"applyFilters": "Appliquer les filtres",
"clearFilters": "Effacer les filtres",
"clearSelection": "Effacer la sélection",
"create": "Créer",
"dataRefreshed": "Données actualisées",
"errorLoading": "Erreur de chargement",
"filter": "Filtrer",
"filtersApplied": "Filtres appliqués",
"noRecords": "Aucun enregistrement",
"notImplemented": "Non implémenté",
"notSpecified": "Non spécifié",
"refreshed": "Actualisé",
"resetToDefaults": "Réinitialiser aux valeurs par défaut",
"saveSettings": "Enregistrer les paramètres",
"settingsReset": "Paramètres réinitialisés",
"settingsSaved": "Paramètres enregistrés"
```

## Priority 3: Action Keys

Simple action translations used in buttons and UI elements.

### Add "action" section to all language files:

#### Italian:
```json
"action": {
  "cancel": "Annulla",
  "clearFilters": "Cancella filtri",
  "delete": "Elimina"
}
```

#### English:
```json
"action": {
  "cancel": "Cancel",
  "clearFilters": "Clear filters",
  "delete": "Delete"
}
```

#### Spanish:
```json
"action": {
  "cancel": "Cancelar",
  "clearFilters": "Limpiar filtros",
  "delete": "Eliminar"
}
```

#### French:
```json
"action": {
  "cancel": "Annuler",
  "clearFilters": "Effacer les filtres",
  "delete": "Supprimer"
}
```

## Implementation Instructions

1. Open each language file in `EventForge.Client/wwwroot/i18n/`
2. Add the sections above in the appropriate location within the JSON structure
3. Ensure proper JSON syntax (commas between sections, no trailing commas)
4. Validate JSON syntax before saving
5. Test the application to verify translations load correctly

## Remaining Work

After implementing these priority translations, 419 keys will still be missing. Please refer to the main Translation Coverage Report (`docs/frontend/translation-coverage-report.md`) for the complete list and implementation strategy.
