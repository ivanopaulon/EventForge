// ES6 module for POS keyboard shortcuts with route validation and cleanup
let keydownHandler = null;
let dotNetReference = null;

export function setupPOSKeyboardShortcuts(dotNetRef) {
    // Store reference for cleanup
    dotNetReference = dotNetRef;
    
    // Define handler function so we can remove it later
    keydownHandler = async function(e) {
        // Only work on POS page - prevent global shortcuts leak
        if (!window.location.pathname.includes('/sales/pos')) {
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
