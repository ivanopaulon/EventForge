// Font preferences helper functions - Multi-Context Font System
// Supports: Noto Sans, Noto Sans Display, Noto Serif, Noto Serif Display, Noto Sans Mono
//           Roboto, Roboto Condensed, Roboto Slab, Roboto Mono
window.EventForge = window.EventForge || {};

window.EventForge.setFontPreferences = function(bodyFont, headingsFont, monoFont, contentFont, fontSize) {
    try {
        if (bodyFont) {
            document.documentElement.style.setProperty('--font-family-body', bodyFont);
            // Backward compatibility
            document.documentElement.style.setProperty('--font-family-primary', bodyFont);
        }
        if (headingsFont) {
            document.documentElement.style.setProperty('--font-family-headings', headingsFont);
        }
        if (monoFont) {
            document.documentElement.style.setProperty('--font-family-monospace', monoFont);
        }
        if (contentFont) {
            document.documentElement.style.setProperty('--font-family-content', contentFont);
            // Backward compatibility
            document.documentElement.style.setProperty('--font-family-serif', contentFont);
        }
        if (fontSize) {
            document.documentElement.style.fontSize = fontSize;
        }
        return true;
    } catch (error) {
        console.error('Error setting font preferences:', error);
        return false;
    }
};
