# Custom Fiscal Printer Protocol – Usage Guide

Documentazione delle 11 casistiche POS per il protocollo Custom di stampa fiscale.

Tutti i comandi vengono costruiti con `CustomCommandBuilder` (frame binario: STX + payload + ETX + checksum XOR).  
Le costanti di protocollo sono definite in `CustomProtocolCommands`.

---

## Casistica 1 – Vendita normale

```csharp
// Cappellino €15.00, qty 1, IVA 22% (codice 1), reparto 1
byte[] open = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_OPEN_RECEIPT)  // "01"
    .Build();

byte[] item = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM)    // "02"
    .AddField("Cappellino")                                 // description
    .AddField(1)                                            // qty
    .AddField(15.00m)                                       // unit price → "1500"
    .AddField(1)                                            // VAT code
    .AddField(CustomProtocolCommands.DEPT_DEFAULT)          // dept 1
    .Build();

byte[] payment = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_PAYMENT)       // "04"
    .AddField(15.00m)                                       // amount → "1500"
    .AddField(1)                                            // CASH
    .Build();

byte[] close = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_CLOSE_RECEIPT) // "05"
    .Build();
```

---

## Casistica 2 – Sconto riga % (cappellino €15 –10% = €13.50)

```csharp
byte[] item = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_WITH_DISCOUNT)  // "02S"
    .AddField("Cappellino")
    .AddField(1)
    .AddField(15.00m)                                                   // → "1500"
    .AddField(1)
    .AddField(CustomProtocolCommands.DEPT_DEFAULT)
    .AddField(10.00m)                                                   // 10% → "1000"
    .AddField(CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE)          // "P"
    .Build();
// Netto riga: 15.00 × 0.90 = 13.50
```

---

## Casistica 3 – Sconto riga € (zaino €89.90 –€20 = €69.90)

```csharp
byte[] item = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_WITH_DISCOUNT)  // "02S"
    .AddField("Zaino")
    .AddField(1)
    .AddField(89.90m)                                                   // → "8990"
    .AddField(1)
    .AddField(CustomProtocolCommands.DEPT_DEFAULT)
    .AddField(20.00m)                                                   // €20 → "2000"
    .AddField(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT)              // "A"
    .Build();
// Netto riga: 89.90 – 20.00 = 69.90
```

---

## Casistica 4 – Maggiorazione riga (birra €5 +€0.50 supplemento servizio = €5.50)

```csharp
byte[] item = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_WITH_SURCHARGE) // "02M"
    .AddField("Birra")
    .AddField(1)
    .AddField(5.00m)                                                    // → "500"
    .AddField(1)
    .AddField(CustomProtocolCommands.DEPT_BEVERAGE)                     // reparto 3
    .AddField(0.50m)                                                    // +€0.50 → "50"
    .AddField(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT)              // "A"
    .Build();
// Netto riga: 5.00 + 0.50 = 5.50
```

---

## Casistica 5 – Omaggio (gadget promozionale, prezzo di listino €5)

```csharp
byte[] item = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM_FREE)           // "02G"
    .AddField("Gadget promozionale")
    .AddField(1)
    .AddField(5.00m)            // prezzo originale stampato → "500"
    .AddField(1)
    .AddField(CustomProtocolCommands.DEPT_DEFAULT)
    .Build();
// L'importo non viene sommato al totale.
// ItemFlag = ITEM_FLAG_FREE ("1") in FiscalReceiptItem.
```

---

## Casistica 6 – Reso (maglietta qty –1, rimborso €29.90)

```csharp
byte[] item = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM)    // "02"
    .AddField("Maglietta (RESO)")
    .AddField(-1)                                           // qty negativa
    .AddField(29.90m)                                       // → "2990"
    .AddField(1)
    .AddField(CustomProtocolCommands.DEPT_NON_FOOD)         // reparto 4
    .Build();
// ItemFlag = ITEM_FLAG_RETURN ("2") in FiscalReceiptItem.
// Il totale viene decrementato.
```

---

## Casistica 7 – Sconto globale % (subtotale €44.90 –15% fidelity = €38.16)

```csharp
byte[] globalDiscount = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_GLOBAL_DISCOUNT)           // "03S"
    .AddField(15.00m)                                                   // 15% → "1500"
    .AddField(CustomProtocolCommands.DISCOUNT_TYPE_PERCENTAGE)          // "P"
    .AddField("Sconto Fidelity Gold")
    .Build();
// Inviare dopo l'ultimo articolo e prima dei pagamenti.
// Netto: 44.90 × 0.85 = 38.165 → arrotondato a 38.16
```

---

## Casistica 8 – Sconto globale € (subtotale €44.90 –€10 buono sconto = €34.90)

```csharp
byte[] globalDiscount = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_GLOBAL_DISCOUNT)           // "03S"
    .AddField(10.00m)                                                   // €10 → "1000"
    .AddField(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT)              // "A"
    .AddField("Buono sconto")
    .Build();
// Netto: 44.90 – 10.00 = 34.90
```

---

## Casistica 9 – Maggiorazione globale (coperto 2 persone +€2.50, IVA 10%)

```csharp
byte[] surcharge = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_GLOBAL_SURCHARGE)          // "03M"
    .AddField(2.50m)                                                    // €2.50 → "250"
    .AddField(CustomProtocolCommands.DISCOUNT_TYPE_AMOUNT)              // "A"
    .AddField("Coperto 2 persone")
    .AddField(2)                                                        // VAT code 2 = 10%
    .Build();
// Totale finale: subtotale + 2.50
```

---

## Casistica 10 – Pagamenti multipli (€50 = €20 contanti + €30 carta)

```csharp
byte[] cash = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_PAYMENT)   // "04"
    .AddField(20.00m)                                   // → "2000"
    .AddField(1)                                        // codice CASH
    .AddField("Contanti")
    .Build();

byte[] card = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_PAYMENT)   // "04"
    .AddField(30.00m)                                   // → "3000"
    .AddField(4)                                        // codice CARTA DI CREDITO
    .AddField("Carta di credito")
    .Build();
// Entrambi i comandi vengono inviati sequenzialmente alla stampante.
// La somma dei pagamenti deve corrispondere al totale dovuto.
```

---

## Casistica 11 – Resto automatico (totale €47.50, pagato €50, resto €2.50)

```csharp
const decimal totalDue = 47.50m;
const decimal amountPaid = 50.00m;
decimal change = amountPaid - totalDue;   // = 2.50 (resto calcolato lato server)

byte[] payment = new CustomCommandBuilder()
    .StartCommand(CustomProtocolCommands.CMD_PAYMENT)   // "04"
    .AddField(amountPaid)                               // importo versato → "5000"
    .AddField(1)                                        // codice CASH
    .AddField("Contanti")
    .Build();
// La stampante calcola e stampa automaticamente il resto
// quando l'importo versato supera il totale dovuto.
// Il valore 'change' può essere mostrato all'operatore nel POS prima dell'invio.
```

---

## Note generali

| Costante | Valore | Descrizione |
|---|---|---|
| `CMD_PRINT_ITEM` | `"02"` | Vendita normale / reso (qty negativa) |
| `CMD_PRINT_ITEM_WITH_DISCOUNT` | `"02S"` | Vendita con sconto su riga |
| `CMD_PRINT_ITEM_WITH_SURCHARGE` | `"02M"` | Vendita con maggiorazione su riga |
| `CMD_PRINT_ITEM_FREE` | `"02G"` | Omaggio (prezzo stampato ma non conteggiato) |
| `CMD_GLOBAL_DISCOUNT` | `"03S"` | Sconto globale scontrino |
| `CMD_GLOBAL_SURCHARGE` | `"03M"` | Maggiorazione globale scontrino |
| `DISCOUNT_TYPE_PERCENTAGE` | `"P"` | Sconto/maggiorazione in percentuale |
| `DISCOUNT_TYPE_AMOUNT` | `"A"` | Sconto/maggiorazione a importo fisso |
| `ITEM_FLAG_NORMAL` | `"0"` | Articolo normale |
| `ITEM_FLAG_FREE` | `"1"` | Articolo omaggio |
| `ITEM_FLAG_RETURN` | `"2"` | Articolo reso |

**Encoding decimale**: tutti i valori monetari sono moltiplicati per 100 e trasmessi come stringa intera (`12.50` → `"1250"`). Usare sempre `AddField(decimal value, int decimals = 2)`.

**Sequenza comandi obbligatoria**:
```
CMD_OPEN_RECEIPT
  [CMD_PRINT_ITEM / 02S / 02M / 02G] × N
  [CMD_GLOBAL_DISCOUNT / CMD_GLOBAL_SURCHARGE]  ← opzionale
CMD_PAYMENT × 1..N
CMD_CLOSE_RECEIPT
```
