// ES6 module for POS keyboard shortcuts with route validation and cleanup
let keydownHandler = null;
let dotNetReference = null;

export function setupPOSKeyboardShortcuts(dotNetRef) {
    // Store reference for cleanup
    dotNetReference = dotNetRef;
    
    // Define handler function so we can remove it later
    keydownHandler = async function(e) {
        // Only work on POS 2026 page — exact match prevents interference with /sales/pos and /sales/postouch
        if (window.location.pathname !== '/sales/pos2026') {
            return;
        }
        
        // Solo se non siamo in un input text (eccetto F-keys)
        const isInInput = ['INPUT', 'TEXTAREA'].includes(document.activeElement.tagName);
        const isFKey = e.key.startsWith('F') && e.key.length <= 3;
        
        if (isInInput && !isFKey) return;
        
        const shortcuts = ['F2', 'F3', 'F4', 'F8', 'F12', 'Escape'];
        
        if (shortcuts.includes(e.key)) {
            e.preventDefault();
            
            // F8: focus on barcode scanner
            if (e.key === 'F8') {
                const barcodeInput = document.querySelector('[data-barcode-input]');
                if (barcodeInput) {
                    barcodeInput.focus();
                    barcodeInput.select();
                }
                return;
            }
            
            await dotNetRef.invokeMethodAsync('HandleKeyboardShortcut', e.key);
        }
    };
    
    document.addEventListener('keydown', keydownHandler);
}

export function cleanup() {
    // Remove event listener to prevent memory leaks
    if (keydownHandler) {
        document.removeEventListener('keydown', keydownHandler);
        keydownHandler = null;
    }
    
    // Dispose .NET reference
    if (dotNetReference) {
        dotNetReference = null;
    }
}

// =========================================================================
// Piano E (opzione E3) — cattura barcode a livello documento, indipendente dal
// focus DOM, ma disattivata esplicitamente quando:
//  - l'input di ricerca (data-barcode-input) ha già il focus (lo gestisce
//    direttamente Pos26SearchBar.OnKeyDown, evitando la doppia elaborazione);
//  - un dialog modale MudBlazor è aperto (.mud-overlay.mud-overlay-dialog);
//  - un altro campo di testo (input/textarea/select/contenteditable) ha il
//    focus attivo, per non intercettare la digitazione volontaria dell'utente
//    in campi come note ordine o coupon.
// La logica di rilevamento (timing tra tasti + fallback Enter) replica quella
// già usata in Pos26SearchBar.razor (OnKeyDown/FireBarcodeAfterSilenceAsync)
// per mantenere lo stesso comportamento percepito indipendentemente da dove
// scatta la cattura.
// =========================================================================
let barcodeKeydownHandler = null;
let barcodeDotNetRef = null;
let barcodeBuffer = '';
let barcodeLastKeyTime = 0;
let barcodeSilenceTimer = null;

const BARCODE_MAX_INTERVAL_MS = 50;
const BARCODE_MIN_CHARS = 4;
const BARCODE_SILENCE_MS = 80;

export function setupDocumentBarcodeCapture(dotNetRef) {
    barcodeDotNetRef = dotNetRef;
    barcodeBuffer = '';
    barcodeLastKeyTime = 0;

    barcodeKeydownHandler = function (e) {
        if (window.location.pathname !== '/sales/pos2026') return;

        const active = document.activeElement;

        // L'input di ricerca gestisce già da sé la propria cattura via @onkeydown.
        if (active && active.hasAttribute && active.hasAttribute('data-barcode-input')) return;

        // Dialog modale aperto: non intercettare (l'utente potrebbe star digitando nel dialog).
        if (document.querySelector('.mud-overlay.mud-overlay-dialog')) return;

        // Un altro campo editabile ha il focus: rispetta la digitazione manuale dell'utente.
        const isEditable = !!active && (
            active.tagName === 'INPUT' ||
            active.tagName === 'TEXTAREA' ||
            active.tagName === 'SELECT' ||
            active.isContentEditable === true
        );
        if (isEditable) return;

        const now = performance.now();
        const intervalMs = barcodeLastKeyTime === 0 ? Infinity : now - barcodeLastKeyTime;
        barcodeLastKeyTime = now;

        if (e.key === 'Enter' && barcodeBuffer.length > 0) {
            const code = barcodeBuffer;
            barcodeBuffer = '';
            clearTimeout(barcodeSilenceTimer);
            fireBarcodeDetected(code);
            return;
        }

        const isPrintable = e.key.length === 1;
        if (!isPrintable) return;

        barcodeBuffer = intervalMs < BARCODE_MAX_INTERVAL_MS ? (barcodeBuffer + e.key) : e.key;

        if (barcodeBuffer.length >= BARCODE_MIN_CHARS) {
            clearTimeout(barcodeSilenceTimer);
            const snapshot = barcodeBuffer;
            barcodeSilenceTimer = setTimeout(() => {
                barcodeBuffer = '';
                fireBarcodeDetected(snapshot);
            }, BARCODE_SILENCE_MS);
        }
    };

    document.addEventListener('keydown', barcodeKeydownHandler);
}

function fireBarcodeDetected(code) {
    if (barcodeDotNetRef && code) {
        barcodeDotNetRef.invokeMethodAsync('HandleDocumentBarcodeDetected', code)
            .catch(err => console.error('[pos-shortcuts] Error invoking HandleDocumentBarcodeDetected:', err));
    }
}

export function cleanupBarcodeCapture() {
    if (barcodeKeydownHandler) {
        document.removeEventListener('keydown', barcodeKeydownHandler);
        barcodeKeydownHandler = null;
    }
    if (barcodeSilenceTimer) {
        clearTimeout(barcodeSilenceTimer);
        barcodeSilenceTimer = null;
    }
    barcodeBuffer = '';
    barcodeDotNetRef = null;
}

