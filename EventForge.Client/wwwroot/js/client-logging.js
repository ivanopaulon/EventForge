// EventForge Client Logging - Safe JSInterop helper functions
// These functions avoid the use of eval() for CSP compliance and security.

window.eventforge_getLocationPath = function () {
    return window.location.pathname + window.location.search;
};

window.eventforge_getUserAgent = function () {
    return navigator.userAgent;
};
