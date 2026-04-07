# Epson POS Printer WebAPI (ePOS-Print XML) – Usage Guide

Documentazione dell'implementazione del protocollo Epson POS Printer WebAPI per stampanti di rete.

**Riferimento**: *Epson POS Printer WebAPI Interface Specification* (Rev. A).

Stampanti supportate: qualsiasi modello della serie **Epson TM** con connettività Ethernet/WiFi
(TM-T88VII, TM-m30III, TM-m50II, TM-T20III, TM-T70II, ecc.).

---

## Architettura

```
EpsonFiscalPrinterService  (implementa IFiscalPrinterService)
  └── EpsonWebApiCommunication  (HTTP POST → /api/1/request:80)
  └── EpsonXmlBuilder           (costruisce i documenti ePOS-Print XML)
  └── EpsonResponseParser       (analizza le risposte XML del server)
```

Il `FiscalPrinterServiceRouter` instrada automaticamente le chiamate al servizio Epson
quando il campo `ProtocolType` della stampante è impostato su `"Epson"`.

---

## Endpoint HTTP

| Campo | Valore |
|---|---|
| **Porta di default** | `80` |
| **Path** | `/api/1/request` |
| **Method** | `POST` |
| **Content-Type** | `text/xml; charset=utf-8` |
| **Timeout** | 30 secondi (configurabile) |

---

## Struttura della richiesta SOAP

Ogni richiesta è incapsulata in un envelope SOAP. L'elemento `<epos-print>` deve
includere gli attributi `devid` (device ID della stampante, default `local_printer`)
e `timeout` (millisecondi):

```xml
<?xml version="1.0" encoding="utf-8"?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/">
  <SOAP-ENV:Body>
    <epos-print xmlns="http://www.epson-pos.com/schemas/2011/03/epos-print"
                devid="local_printer"
                timeout="10000">
      <!-- comandi di stampa -->
    </epos-print>
  </SOAP-ENV:Body>
</SOAP-ENV:Envelope>
```

---

## Struttura della risposta SOAP

```xml
<?xml version="1.0" encoding="utf-8"?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV="http://schemas.xmlsoap.org/soap/envelope/">
  <SOAP-ENV:Body>
    <response xmlns="http://www.epson-pos.com/schemas/2011/03/epos-print"
              success="true"
              code=""
              status="42" />
  </SOAP-ENV:Body>
</SOAP-ENV:Envelope>
```

In caso di errore:
```xml
<response success="false" code="EPTR_REC_EMPTY" status="..."/>
```

---

## Comandi principali ePOS-Print

### Testo

```xml
<!-- Testo semplice, allineato a sinistra -->
<text align="left">Articolo 1         €10.00\n</text>

<!-- Testo centrato e in grassetto -->
<text align="center" bold="true">** SCONTRINO **\n</text>

<!-- Testo Font B (compatto) -->
<text font="font_b" align="left">Nota aggiuntiva\n</text>
```

### Avanzamento carta

```xml
<feed line="3"/>
```

### Taglio carta

```xml
<cut type="feed"/>    <!-- taglia con avanzamento -->
<cut type="no_feed"/> <!-- taglia senza avanzamento -->
```

### Apertura cassetto

```xml
<pulse drawer="drawer_1" time="pulse_100"/>
```

---

## Codici di errore (`code` nella risposta)

| Codice | Descrizione |
|---|---|
| `EPTR_COVER_OPEN` | Coperchio aperto |
| `EPTR_REC_EMPTY` | Carta esaurita |
| `EPTR_MECHANICAL` | Errore meccanico |
| `EPTR_AUTOCUTTER` | Errore tagliacarta |
| `EPTR_UNRECOVERABLE` | Errore non recuperabile |
| `SchemaError` | XML non valido |
| `DeviceNotFound` | Device ID non trovato |
| `EX_TIMEOUT` | Timeout richiesta |

---

## Bitmask di stato (`status` nella risposta)

Il valore `status` è un intero decimale. I bit hanno il seguente significato:

| Bit (valore) | Flag |
|---|---|
| Bit 1 (0x02) | `IsDrawerOpen` – cassetto aperto |
| Bit 2 (0x04) | `IsOnline` – stampante in linea |
| Bit 5 (0x20) | `IsPaperLow` – carta quasi esaurita |
| Bit 6 (0x40) | `IsPaperOut` – carta esaurita |
| Bit 9 (0x200) | `IsCoverOpen` – coperchio aperto |

---

## Configurazione stampante

Aggiungere la stampante dal wizard `FiscalPrinterSetupWizard` selezionando:
- **Protocol Type**: `Epson`
- **Connection Type**: `Tcp`
- **Address**: indirizzo IP della stampante (es. `192.168.1.200`)
- **Port**: `80` (default WebAPI Epson)
- **UsbDeviceId**: device ID configurato nella stampante (default: `local_printer`)

Per trovare il device ID della propria stampante, accedere all'interfaccia web della
stampante (`http://{ip}/`) e verificare le impostazioni del servizio WebAPI.

---

## Nota sulle operazioni fiscali

Questo protocollo non utilizza la memoria fiscale hardware (RT). Le operazioni di
"chiusura giornaliera" e "Z-report" vengono eseguite a livello software dal server
EventForge che aggrega i dati di vendita dal database e stampa un documento di
riepilogo sulla stampante di rete Epson.

Per la conformità fiscale italiana con RT (Registratore Telematico), utilizzare il
protocollo **Custom** su stampanti con memoria fiscale certificata.
