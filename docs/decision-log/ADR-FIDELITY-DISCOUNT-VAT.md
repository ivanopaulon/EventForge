# ADR: Trattamento IVA dello sconto fidelity card sul totale ordine

**Decision Date:** 2026-07-10
**Status:** Confermato dal commercialista del progetto, in produzione
**Decision Maker:** commercialista del progetto

## Contesto

Lo sconto associato al livello di una fidelity card (`FidelityCard.DiscountPercentage`) va applicato al totale dell'ordine in `SaleSessionService.CalculateTotalsAsync`. Il carrello può contenere prodotti con aliquote IVA diverse (es. 22%, 10%, 4%). Serve stabilire come lo sconto riduce imponibile e imposta quando le aliquote sono miste.

## Decisione

Lo sconto fidelity è un "sconto in denaro incondizionato" ai sensi dell'art. 26 DPR 633/1972 (distinto dallo sconto in natura ex art. 15) — riduce direttamente il corrispettivo, e la base imponibile va calcolata al netto dello sconto.

Formula adottata: lo sconto (percentuale uniforme su tutto il carrello) viene sottratto dal totale netto, e l'IVA complessiva viene ridotta applicando lo stesso rapporto percentuale al totale imposta già calcolato per riga. Questo è matematicamente equivalente a ridurre ogni riga (e la sua aliquota specifica) della stessa percentuale e risommare — vale perché lo sconto è sempre percentuale e uniforme, non un importo fisso o un targeting per riga.

## Conseguenze

- Corretto per lo stato attuale (sconto fidelity come percentuale unica per carta, applicata all'intero carrello).
- **Da rivedere se in futuro lo sconto fidelity diventa un importo fisso invece che percentuale**, o se si introduce un targeting per prodotto/categoria (come già annotato per le campagne punti in `PROMPT_10_FIDELITY_POINTS_ENTITIES.md`) — l'equivalenza matematica alla base di questa decisione non si applica più in quei casi.
- Verificare separatamente (non ancora fatto) che la generazione di fattura elettronica, se presente, calcoli il riepilogo IVA per aliquota dai dati per-riga (`SaleItem`, che restano corretti individualmente) e non dal campo aggregato `SaleSession.TaxAmount` — il riepilogo IVA per aliquota richiesto dalla fattura elettronica non può essere ricostruito da un singolo totale imposta aggregato.
