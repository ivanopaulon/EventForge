/**
 * QZ Tray Setup Helper
 * 
 * Configures QZ Tray certificate and signature promises to use EventForge server endpoints.
 * Supports both cookie-based authentication (default) and optional Bearer token authentication.
 * 
 * Usage:
 *   window.qzSetup.init({ baseUrl: 'https://localhost:7241' });
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
     */
    function init(options) {
        if (!options || !options.baseUrl) {
            throw new Error('baseUrl is required for QZ setup');
        }

        if (typeof qz === 'undefined' || !qz.api) {
            throw new Error('QZ Tray library not loaded or qz.api not available');
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
        qz.api.setCertificatePromise(function() {
            const headers = {
                ...getAuthHeaders()
            };

            return fetch(`${baseUrl}/api/printing/qz/certificate`, {
                method: 'GET',
                credentials: 'include', // Send cookies for authentication
                headers: headers
            })
            .then(handleResponse)
            .then(response => response.text());
        });

        // Set signature promise to POST challenge to server endpoint
        qz.api.setSignaturePromise(function(toSign) {
            const headers = {
                'Content-Type': 'text/plain',
                ...getAuthHeaders()
            };

            return fetch(`${baseUrl}/api/printing/qz/sign`, {
                method: 'POST',
                credentials: 'include', // Send cookies for authentication
                headers: headers,
                body: toSign
            })
            .then(handleResponse)
            .then(response => response.text());
        });

        console.log('QZ Tray promises configured for base URL:', baseUrl);
    }

    // Expose the setup function globally
    window.qzSetup = {
        init: init
    };

})();