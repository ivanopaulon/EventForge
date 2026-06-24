/**
 * app-interop.js
 * General-purpose JS interop helpers for the EventForge Blazor WASM application.
 * Handles dynamic theming (Syncfusion CSS swap) and other cross-cutting concerns.
 */

window.EventForge = window.EventForge || {};

/**
 * Dynamically swaps the Syncfusion Blazor theme stylesheet.
 * Removes any existing Syncfusion theme link and inserts the correct one
 * for the active EventForge theme (dark → fluent2-dark, light → fluent2).
 *
 * @param {boolean} isDark - true for dark mode, false for light mode.
 */
window.EventForge.setSyncfusionTheme = function (isDark) {
    try {
        const SYNCFUSION_LINK_ID = 'syncfusion-theme-link';
        const darkHref  = '_content/Syncfusion.Blazor.Themes/fluent2-dark.css';
        const lightHref = '_content/Syncfusion.Blazor.Themes/fluent2.css';
        const targetHref = isDark ? darkHref : lightHref;

        let link = document.getElementById(SYNCFUSION_LINK_ID);

        if (!link) {
            // Remove any pre-existing Syncfusion theme links injected by the static HTML.
            document.querySelectorAll('link[href*="Syncfusion.Blazor.Themes"]').forEach(el => el.remove());

            link = document.createElement('link');
            link.id   = SYNCFUSION_LINK_ID;
            link.rel  = 'stylesheet';
            link.type = 'text/css';
            document.head.appendChild(link);
        }

        if (link.getAttribute('href') !== targetHref) {
            link.href = targetHref;
        }
    } catch (e) {
        console.warn('[EventForge] setSyncfusionTheme error:', e);
    }
};

/**
 * Reads the persisted theme key from localStorage and applies the data-theme
 * attribute to <html> immediately — called via an inline script in index.html
 * before Blazor boots, to avoid the initial theme flash.
 *
 * Safe to call multiple times (idempotent).
 */
window.EventForge.applyPersistedTheme = function () {
    try {
        const STORAGE_KEY = 'eventforge-theme';
        const DEFAULT     = 'carbon-neon-light';
        const VALID       = ['carbon-neon-dark', 'carbon-neon-light'];

        let stored = localStorage.getItem(STORAGE_KEY);

        // Backward compat: old "dark"/"light" strings
        if (stored === 'dark')  stored = 'carbon-neon-dark';
        if (stored === 'light') stored = 'carbon-neon-light';

        const theme = (stored && VALID.includes(stored)) ? stored : DEFAULT;
        document.documentElement.setAttribute('data-theme', theme);

        // Apply Syncfusion theme synchronously so there is no flash.
        const isDark = theme === 'carbon-neon-dark';
        window.EventForge.setSyncfusionTheme(isDark);
    } catch (e) {
        // localStorage may be unavailable in some private-browsing contexts.
        console.warn('[EventForge] applyPersistedTheme error:', e);
    }
};

// Scroll a Blazor ElementReference to the bottom (used by WhatsApp chat simulator and other chat UIs)
window.scrollElementToBottom = function (element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};
