/**
 * QZ Tray Setup Helper
 * 
 * Configures QZ Tray certificate and signature promises to use EventForge server endpoints.
 * Supports both cookie-based authentication (default) and optional Bearer token authentication.
 * 
 * RESILIENT LOADING (PR: Make QZ Tray loading resilient):
 * - Does not throw exceptions if QZ Tray library is unavailable
 * - Returns Promise.resolve() for graceful degradation
 * - Logs warnings but does not block application startup
 * 
 * Usage:
 *   window.qzSetup.init({ baseUrl: 'https://localhost:7241' })
 *     .then(() => console.log('QZ Tray ready or gracefully skipped'));
 * 
 * Optional Bearer token support:
 *   Define window.getAuthToken() to return a Bearer token, otherwise cookies are used.
 */

(function() {
    'use strict';

    /**
     * Initialize QZ Tray promises with EventForge server endpoints
     * @param {Object} options - Configuration options
     * @param {string} options.baseUrl - Base URL for API endpoints (e.g., 'https://localhost:7241')
     * @returns {Promise} - Resolves when initialization is complete or skipped
     */
    function init(options) {
        if (!options || !options.baseUrl) {
            console.warn('QZ Tray setup: baseUrl is required but not provided. Initialization skipped.');
            return Promise.resolve();
        }

        // Check if QZ Tray library is available (graceful degradation)
        if (typeof qz === 'undefined') {
            console.warn('QZ Tray non disponibile - inizializzazione saltata. Le funzionalità di stampa saranno disabilitate.');
            return Promise.resolve();
        }

        // Detect QZ Tray version and API structure
        // QZ Tray 2.1.x uses qz.api.setCertificatePromise
        // QZ Tray 2.2.x+ uses qz.security.setCertificatePromise
        const securityApi = qz.security || qz.api;
        
        if (!securityApi || typeof securityApi.setCertificatePromise !== 'function') {
            console.warn('QZ Tray API non compatibile - inizializzazione saltata. Le funzionalità di stampa saranno disabilitate.');
            return Promise.resolve();
        }

        const baseUrl = options.baseUrl.replace(/\/$/, ''); // Remove trailing slash

        // Helper function to get authentication headers
        function getAuthHeaders() {
            const headers = {};
            
            // Check if optional Bearer token function is available
            if (typeof window.getAuthToken === 'function') {
                try {
                    const token = window.getAuthToken();
                    if (token) {
                        headers['Authorization'] = `Bearer ${token}`;
                    }
                } catch (error) {
                    console.warn('Error getting auth token, falling back to cookies:', error);
                }
            }
            
            return headers;
        }

        // Helper function to handle fetch responses
        async function handleResponse(response) {
            if (!response.ok) {
                let errorMessage = `HTTP ${response.status}`;
                try {
                    // Try to get error message from response body
                    const errorText = await response.text();
                    if (errorText) {
                        errorMessage = errorText;
                    }
                } catch (e) {
                    // Ignore error reading response body
                }
                throw new Error(errorMessage);
            }
            return response;
        }

        // Set certificate promise to fetch from server endpoint
        securityApi.setCertificatePromise(function(resolve, reject) {
            const headers = {
                ...getAuthHeaders()
            };

            fetch(`${baseUrl}/api/printing/qz/certificate`, {
                method: 'GET',
                credentials: 'include', // Send cookies for authentication
                headers: headers
            })
            .then(handleResponse)
            .then(response => response.text())
            .then(resolve)
            .catch(reject);
        });

        // Set signature promise to POST challenge to server endpoint
        securityApi.setSignaturePromise(function(toSign) {
            return function(resolve, reject) {
                const headers = {
                    'Content-Type': 'text/plain',
                    ...getAuthHeaders()
                };

                fetch(`${baseUrl}/api/printing/qz/sign`, {
                    method: 'POST',
                    credentials: 'include', // Send cookies for authentication
                    headers: headers,
                    body: toSign
                })
                .then(handleResponse)
                .then(response => response.text())
                .then(resolve)
                .catch(reject);
            };
        });

        console.info('QZ Tray configurato correttamente per base URL:', baseUrl);
        return Promise.resolve();
    }

    // Expose the setup function globally
    window.qzSetup = {
        init: init
    };

})();