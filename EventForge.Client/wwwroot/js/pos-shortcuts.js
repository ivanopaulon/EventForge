window.setupPOSKeyboardShortcuts = function(dotNetRef) {
    document.addEventListener('keydown', async function(e) {
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
    });
};
