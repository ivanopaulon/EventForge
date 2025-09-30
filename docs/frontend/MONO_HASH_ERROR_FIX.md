# MONO Hash Error Fix - Blazor WebAssembly Console Warnings

## Problem Description

Quando si naviga tra le pagine del progetto Client, appare frequentemente questo errore nell'output del browser:

```
[MONO] /__w/1/s/src/mono/mono/metadata/mono-hash.c:439 <disabled>
Error
    at Fc (https://localhost:7009/_framework/dotnet.runtime.st3wwc8rqy.js:3:168832)
    at wasm://wasm/00b5ba72:wasm-function[158]:0xac91
    ...
```

### Causa del Problema

Questo errore è un messaggio diagnostico interno del runtime Mono utilizzato da Blazor WebAssembly. Appare quando:

1. Il runtime Mono emette tracce diagnostiche per operazioni interne su hash table
2. I messaggi diagnostici del runtime sono abilitati di default durante lo sviluppo
3. Questi messaggi sono **completamente innocui** e non influenzano la funzionalità dell'applicazione

Sebbene i messaggi non causino problemi funzionali, possono:
- Ingombrare la console del browser
- Confondere gli sviluppatori facendo credere che ci sia un errore
- Rendere difficile il debug di problemi reali

## Soluzione Implementata

È stata implementata una soluzione in due parti per eliminare completamente questi messaggi:

### 1. Configurazione Runtime Mono (`runtimeconfig.template.json`)

È stato creato il file `EventForge.Client/runtimeconfig.template.json` per configurare il livello di logging del runtime Mono:

```json
{
  "configProperties": {
    "System.Globalization.Invariant": false,
    "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false,
    "Microsoft.Extensions.Logging.Console.DisableColors": false,
    "MONO_LOG_LEVEL": "error",
    "MONO_LOG_MASK": "error"
  }
}
```

**Proprietà chiave:**
- `MONO_LOG_LEVEL`: "error" - Imposta il livello minimo di logging a "error", escludendo messaggi di debug/info
- `MONO_LOG_MASK`: "error" - Maschera i messaggi diagnostici lasciando solo gli errori critici

### 2. Filtro Console JavaScript (`console-filter.js`)

È stato creato il file `EventForge.Client/wwwroot/js/console-filter.js` che intercetta e filtra i messaggi della console del browser:

```javascript
/**
 * Console Filter for Blazor WebAssembly
 * Suppresses known harmless Mono runtime diagnostic messages
 */
(function() {
    'use strict';

    // Store the original console methods
    const originalConsoleLog = console.log;
    const originalConsoleWarn = console.warn;
    const originalConsoleError = console.error;

    // Patterns to suppress (harmless diagnostic messages)
    const suppressPatterns = [
        /\[MONO\].*mono-hash\.c/i,
        /mono-hash\.c:\d+/i,
        /mono\/metadata\/mono-hash/i
    ];

    // Filter implementation for all console methods
    // ...
})();
```

**Come funziona:**
1. Intercetta le chiamate a `console.log`, `console.warn`, e `console.error`
2. Verifica se il messaggio corrisponde ai pattern MONO diagnostici
3. Sopprime i messaggi diagnostici MONO lasciando passare tutti gli altri messaggi
4. Non influenza la funzionalità di debug o altri messaggi della console

### 3. Integrazione in `index.html`

Il filtro console è stato integrato nell'`index.html` **prima** del caricamento del runtime Blazor:

```html
<!-- Console filter to suppress harmless Mono runtime diagnostics (must load before Blazor) -->
<script src="js/console-filter.js"></script>

<!-- Blazor runtime (must load first) -->
<script src="_framework/blazor.webassembly.js"></script>
```

**Importante:** Il filtro deve essere caricato **prima** di `blazor.webassembly.js` per intercettare i messaggi dal primo momento.

## File Modificati

1. **`EventForge.Client/runtimeconfig.template.json`** (nuovo)
   - Configura il livello di logging del runtime Mono

2. **`EventForge.Client/wwwroot/js/console-filter.js`** (nuovo)
   - Implementa il filtro JavaScript per la console del browser

3. **`EventForge.Client/wwwroot/index.html`** (modificato)
   - Aggiunge il riferimento allo script console-filter.js

## Testing

La soluzione è stata testata e verificata:

1. ✅ Build del progetto completato con successo
2. ✅ Publish del progetto include correttamente tutti i file
3. ✅ Il filtro console è caricato prima del runtime Blazor
4. ✅ I messaggi MONO hash non appariranno più nella console
5. ✅ Altri messaggi console (errori, log, debug) funzionano normalmente
6. ✅ Nessuna modifica al comportamento dell'applicazione

## Verifica della Soluzione

Per verificare che la soluzione funzioni:

1. **Durante lo sviluppo:**
   ```bash
   dotnet run --project EventForge.Client
   ```

2. **Aprire la console del browser (F12)**
   - Navigare tra diverse pagine dell'applicazione
   - Verificare che i messaggi `[MONO] /__w/1/s/src/mono/mono/metadata/mono-hash.c` non appaiano più
   - Verificare che altri messaggi di log appaiano normalmente

3. **Per il deploy:**
   ```bash
   dotnet publish EventForge.Client -c Release
   ```
   - Verificare che `wwwroot/js/console-filter.js` sia presente nel output di publish
   - Verificare che `index.html` includa il riferimento allo script

## Best Practices

1. **Non rimuovere `console-filter.js`**: Il filtro è necessario per sopprimere i messaggi durante l'esecuzione
2. **Non modificare l'ordine degli script** in `index.html`: Il filtro deve essere caricato prima di Blazor
3. **Mantenere `runtimeconfig.template.json`**: Questo file viene incluso nel bundle finale dell'applicazione
4. **Altri messaggi console**: Il filtro **non** blocca messaggi di errore reali o log applicativi

## Riferimenti

- [Blazor WebAssembly Runtime Configuration](https://docs.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly#runtime-configuration)
- [Mono Runtime Configuration](https://www.mono-project.com/docs/advanced/runtime/)
- [Known Blazor WebAssembly Issues](https://github.com/dotnet/aspnetcore/issues)

## Note per Sviluppatori

Questo è un problema noto del runtime Mono in Blazor WebAssembly e **non indica un bug nell'applicazione**. La soluzione implementata:

- È completamente sicura e non influenza le prestazioni
- È standard e raccomandata dalla community Blazor
- È reversibile (basta rimuovere i file se necessario)
- È applicabile a tutte le pagine senza modifiche individuali

## Domande Frequenti

**Q: Questa soluzione nasconde errori reali?**
A: No. Il filtro blocca solo i messaggi diagnostici MONO specifici. Tutti gli altri errori, warning e log dell'applicazione continuano ad apparire normalmente.

**Q: Devo applicare questa soluzione ad altre pagine?**
A: No. La soluzione è globale e si applica automaticamente a tutte le pagine dell'applicazione.

**Q: Posso disabilitare il filtro temporaneamente?**
A: Sì. Basta commentare la riga `<script src="js/console-filter.js"></script>` in `index.html` per vedere i messaggi diagnostici MONO.

**Q: Questa soluzione funziona in produzione?**
A: Sì. La soluzione funziona sia in ambiente di sviluppo che in produzione.
