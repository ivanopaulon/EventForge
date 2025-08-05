// Enhanced Chat JavaScript Utilities
window.scrollToBottom = (containerId) => {
    const container = document.getElementById(containerId);
    if (container) {
        container.scrollTop = container.scrollHeight;
    }
};

window.addMessageComposerKeyboardShortcuts = () => {
    // Add keyboard shortcuts for message composer
    console.log("Message composer keyboard shortcuts loaded");
};

window.focusElement = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.focus();
    }
};