/**
 * Console Filter for Blazor WebAssembly
 * Suppresses known harmless Mono runtime diagnostic messages
 * that appear frequently during normal application operation.
 */
(function() {
    'use strict';

    // Store the original console methods
    const originalConsoleLog = console.log;
    const originalConsoleWarn = console.warn;
    const originalConsoleError = console.error;

    // Patterns to suppress (these are harmless diagnostic messages)
    const suppressPatterns = [
        /\[MONO\].*mono-hash\.c/i,  // MONO hash table diagnostics
        /mono-hash\.c:\d+/i,         // Any mono-hash.c line references
        /mono\/metadata\/mono-hash/i // Mono hash metadata warnings
    ];

    /**
     * Check if a message should be suppressed
     * @param {string} message - The console message to check
     * @returns {boolean} - True if the message should be suppressed
     */
    function shouldSuppress(message) {
        if (typeof message !== 'string') {
            return false;
        }
        
        return suppressPatterns.some(pattern => pattern.test(message));
    }

    /**
     * Filtered console.log
     */
    console.log = function(...args) {
        const message = args[0];
        if (!shouldSuppress(message)) {
            originalConsoleLog.apply(console, args);
        }
    };

    /**
     * Filtered console.warn
     */
    console.warn = function(...args) {
        const message = args[0];
        if (!shouldSuppress(message)) {
            originalConsoleWarn.apply(console, args);
        }
    };

    /**
     * Filtered console.error
     */
    console.error = function(...args) {
        const message = args[0];
        if (!shouldSuppress(message)) {
            originalConsoleError.apply(console, args);
        }
    };

    // Log that the filter has been initialized (only in development)
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
        originalConsoleLog('[EventForge] Console filter initialized - Mono runtime diagnostics suppressed');
    }
})();
