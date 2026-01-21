/**
 * Keyboard Shortcuts Handler for AddDocumentRowDialog
 * PR #2c-Part1 - Commit 2
 */
window.KeyboardShortcuts = {
    dotNetRef: null,
    handler: null,

    /**
     * Register keyboard shortcuts with .NET component reference
     */
    register: function (dotNetReference) {
        // Clean up existing handler first
        this.unregister();
        
        this.dotNetRef = dotNetReference;
        
        this.handler = async (e) => {
            // Don't interfere with input fields (except for Ctrl shortcuts)
            const isInputField = e.target.tagName === 'INPUT' || 
                                 e.target.tagName === 'TEXTAREA' ||
                                 e.target.isContentEditable;
            
            let shortcut = null;
            
            // Ctrl shortcuts (work everywhere)
            if (e.ctrlKey && e.key === 's') {
                e.preventDefault();
                shortcut = 'ctrl+s';
            } else if (e.ctrlKey && e.key === 'Enter') {
                e.preventDefault();
                shortcut = 'ctrl+enter';
            } else if (e.ctrlKey && e.key === 'e') {
                e.preventDefault();
                shortcut = 'ctrl+e';
            }
            // Non-input shortcuts
            else if (!isInputField) {
                if (e.key === '?') {
                    e.preventDefault();
                    shortcut = '?';
                } else if (e.key === 'F2') {
                    e.preventDefault();
                    shortcut = 'f2';
                } else if (e.key === 'F3') {
                    e.preventDefault();
                    shortcut = 'f3';
                } else if (e.key === '+') {
                    e.preventDefault();
                    shortcut = '+';
                } else if (e.key === '-') {
                    e.preventDefault();
                    shortcut = '-';
                } else if (e.key === '*') {
                    e.preventDefault();
                    shortcut = '*';
                }
            }
            
            if (shortcut && this.dotNetRef) {
                try {
                    await this.dotNetRef.invokeMethodAsync('HandleKeyboardShortcut', shortcut);
                } catch (error) {
                    console.error('Error invoking keyboard shortcut:', error);
                }
            }
        };
        
        document.addEventListener('keydown', this.handler);
    },

    /**
     * Unregister and cleanup
     * Note: DotNetObjectReference disposal is handled in .NET code
     */
    unregister: function () {
        if (this.handler) {
            document.removeEventListener('keydown', this.handler);
            this.handler = null;
        }
        // Clear reference but don't dispose - disposal is handled in .NET
        this.dotNetRef = null;
    }
};

/**
 * Focus element by ID or CSS selector
 */
window.focusElement = function (elementIdOrSelector) {
    try {
        const element = document.getElementById(elementIdOrSelector) || 
                       document.querySelector(elementIdOrSelector);
        
        if (element) {
            // For MudBlazor components, focus the input inside
            const input = element.querySelector('input') || 
                         element.querySelector('textarea') || 
                         element;
            
            input.focus();
            
            // Select text if it's an input
            if (input.tagName === 'INPUT' && input.type === 'text') {
                input.select();
            }
            
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error focusing element:', error);
        return false;
    }
};
