// JavaScript functions for interactive walkthrough and help system

/**
 * Gets element information including position and size
 * @param {string} selector - CSS selector for the element
 * @returns {object} Element position and size information
 */
window.getElementInfo = function(selector) {
    try {
        const element = document.querySelector(selector);
        if (!element) {
            console.warn(`Element not found for selector: ${selector}`);
            return null;
        }

        const rect = element.getBoundingClientRect();
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
        const scrollLeft = window.pageXOffset || document.documentElement.scrollLeft;

        return {
            top: rect.top + scrollTop,
            left: rect.left + scrollLeft,
            width: rect.width,
            height: rect.height,
            viewportWidth: window.innerWidth,
            viewportHeight: window.innerHeight
        };
    } catch (error) {
        console.error('Error getting element info:', error);
        return null;
    }
};

/**
 * Scrolls element into view if it's not visible
 * @param {string} selector - CSS selector for the element
 */
window.scrollIntoViewIfNeeded = function(selector) {
    try {
        const element = document.querySelector(selector);
        if (!element) {
            console.warn(`Element not found for selector: ${selector}`);
            return;
        }

        const rect = element.getBoundingClientRect();
        const isVisible = rect.top >= 0 && 
                         rect.left >= 0 && 
                         rect.bottom <= window.innerHeight && 
                         rect.right <= window.innerWidth;

        if (!isVisible) {
            element.scrollIntoView({ 
                behavior: 'smooth', 
                block: 'center',
                inline: 'center' 
            });
        }
    } catch (error) {
        console.error('Error scrolling element into view:', error);
    }
};

/**
 * Starts an interactive walkthrough for a component
 * @param {string} componentId - ID of the component
 * @param {Array} steps - Array of step IDs
 */
window.startInteractiveWalkthrough = function(componentId, steps) {
    console.log(`Starting walkthrough for ${componentId} with steps:`, steps);
    
    // This function can be extended to trigger Blazor component methods
    // For now, it logs the walkthrough start
    
    // Could also add analytics tracking here
    if (window.gtag) {
        window.gtag('event', 'walkthrough_started', {
            component_id: componentId,
            steps_count: steps.length
        });
    }
};

/**
 * Highlights an element on the page
 * @param {string} selector - CSS selector for the element
 * @param {string} highlightClass - CSS class to add for highlighting
 */
window.highlightElement = function(selector, highlightClass = 'walkthrough-highlight-element') {
    try {
        // Remove previous highlights
        document.querySelectorAll(`.${highlightClass}`).forEach(el => {
            el.classList.remove(highlightClass);
        });

        const element = document.querySelector(selector);
        if (element) {
            element.classList.add(highlightClass);
            
            // Add CSS if not already present
            if (!document.getElementById('walkthrough-styles')) {
                const style = document.createElement('style');
                style.id = 'walkthrough-styles';
                style.textContent = `
                    .${highlightClass} {
                        animation: pulse-highlight 2s infinite;
                        position: relative;
                        z-index: 9998;
                    }
                    
                    @keyframes pulse-highlight {
                        0% { box-shadow: 0 0 0 0 rgba(33, 150, 243, 0.7); }
                        70% { box-shadow: 0 0 0 10px rgba(33, 150, 243, 0); }
                        100% { box-shadow: 0 0 0 0 rgba(33, 150, 243, 0); }
                    }
                `;
                document.head.appendChild(style);
            }
            
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error highlighting element:', error);
        return false;
    }
};

/**
 * Removes highlight from all elements
 * @param {string} highlightClass - CSS class used for highlighting
 */
window.removeHighlight = function(highlightClass = 'walkthrough-highlight-element') {
    try {
        document.querySelectorAll(`.${highlightClass}`).forEach(el => {
            el.classList.remove(highlightClass);
        });
    } catch (error) {
        console.error('Error removing highlight:', error);
    }
};

/**
 * Checks if an element exists and is visible
 * @param {string} selector - CSS selector for the element
 * @returns {boolean} Whether the element exists and is visible
 */
window.isElementVisible = function(selector) {
    try {
        const element = document.querySelector(selector);
        if (!element) return false;
        
        const style = window.getComputedStyle(element);
        return style.display !== 'none' && 
               style.visibility !== 'hidden' && 
               style.opacity !== '0';
    } catch (error) {
        console.error('Error checking element visibility:', error);
        return false;
    }
};

/**
 * Adds focus trap for modal dialogs (accessibility)
 * @param {string} modalSelector - CSS selector for the modal
 */
window.addFocusTrap = function(modalSelector) {
    try {
        const modal = document.querySelector(modalSelector);
        if (!modal) return;

        const focusableElements = modal.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        
        if (focusableElements.length === 0) return;

        const firstElement = focusableElements[0];
        const lastElement = focusableElements[focusableElements.length - 1];

        const handleTabKey = function(e) {
            if (e.key === 'Tab') {
                if (e.shiftKey) {
                    if (document.activeElement === firstElement) {
                        lastElement.focus();
                        e.preventDefault();
                    }
                } else {
                    if (document.activeElement === lastElement) {
                        firstElement.focus();
                        e.preventDefault();
                    }
                }
            }
        };

        modal.addEventListener('keydown', handleTabKey);
        firstElement.focus();

        // Return cleanup function
        return function cleanup() {
            modal.removeEventListener('keydown', handleTabKey);
        };
    } catch (error) {
        console.error('Error adding focus trap:', error);
        return null;
    }
};

/**
 * Announces text to screen readers
 * @param {string} message - Message to announce
 * @param {string} priority - Priority level (polite, assertive)
 */
window.announceToScreenReader = function(message, priority = 'polite') {
    try {
        const announcement = document.createElement('div');
        announcement.setAttribute('aria-live', priority);
        announcement.setAttribute('aria-atomic', 'true');
        announcement.className = 'sr-only';
        announcement.textContent = message;
        
        document.body.appendChild(announcement);
        
        // Clean up after announcement
        setTimeout(() => {
            if (document.body.contains(announcement)) {
                document.body.removeChild(announcement);
            }
        }, 1000);
    } catch (error) {
        console.error('Error announcing to screen reader:', error);
    }
};

/**
 * Saves help preferences to localStorage
 * @param {object} preferences - User preferences object
 */
window.saveHelpPreferences = function(preferences) {
    try {
        localStorage.setItem('eventforge_help_preferences', JSON.stringify(preferences));
        return true;
    } catch (error) {
        console.error('Error saving help preferences:', error);
        return false;
    }
};

/**
 * Gets help preferences from localStorage
 * @returns {object} User preferences object
 */
window.getHelpPreferences = function() {
    try {
        const preferences = localStorage.getItem('eventforge_help_preferences');
        return preferences ? JSON.parse(preferences) : {};
    } catch (error) {
        console.error('Error getting help preferences:', error);
        return {};
    }
};

// Initialize help system when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('EventForge Help System initialized');
    
    // Add global keyboard shortcuts for help
    document.addEventListener('keydown', function(e) {
        // F1 key for help
        if (e.key === 'F1') {
            e.preventDefault();
            // Trigger help dialog - this could call a Blazor method
            console.log('Help requested via F1 key');
        }
        
        // Ctrl+Shift+? for walkthrough
        if (e.ctrlKey && e.shiftKey && e.key === '?') {
            e.preventDefault();
            console.log('Walkthrough requested via keyboard shortcut');
        }
    });
});

// Export functions for module systems if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        getElementInfo: window.getElementInfo,
        scrollIntoViewIfNeeded: window.scrollIntoViewIfNeeded,
        startInteractiveWalkthrough: window.startInteractiveWalkthrough,
        highlightElement: window.highlightElement,
        removeHighlight: window.removeHighlight,
        isElementVisible: window.isElementVisible,
        addFocusTrap: window.addFocusTrap,
        announceToScreenReader: window.announceToScreenReader,
        saveHelpPreferences: window.saveHelpPreferences,
        getHelpPreferences: window.getHelpPreferences
    };
}