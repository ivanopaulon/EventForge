// Font preferences helper functions
window.EventForge = window.EventForge || {};

window.EventForge.setFontPreferences = function(primaryFont, monoFont, fontSize) {
    try {
        if (primaryFont) {
            document.documentElement.style.setProperty('--font-family-primary', primaryFont);
        }
        if (monoFont) {
            document.documentElement.style.setProperty('--font-family-monospace', monoFont);
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
