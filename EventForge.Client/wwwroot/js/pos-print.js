// POS Receipt Printing Functionality
window.printReceipt = function(htmlContent) {
    // Create a new window/iframe for printing
    const printWindow = window.open('', '_blank', 'width=800,height=600');
    
    if (printWindow) {
        printWindow.document.write(htmlContent);
        printWindow.document.close();
        
        // Wait for content to load, then print
        printWindow.onload = function() {
            printWindow.focus();
            // Give a small delay for rendering
            setTimeout(function() {
                printWindow.print();
                // Close after printing (or user cancels)
                setTimeout(function() {
                    printWindow.close();
                }, 100);
            }, 250);
        };
    } else {
        console.error('Could not open print window. Pop-up may be blocked.');
        alert('Impossibile aprire la finestra di stampa. Verifica che i popup non siano bloccati.');
    }
};
