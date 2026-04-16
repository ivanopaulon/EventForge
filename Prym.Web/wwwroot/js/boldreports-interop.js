/**
 * boldreports-interop.js
 * JS Interop helpers for the Bold Reports JavaScript designer and viewer components.
 * Called from ReportDesignerComponent.razor and ReportViewerComponent.razor via IJSRuntime.
 *
 * Bold Reports v2.0 (EJ1-based) exposes jQuery plugins:
 *   $("#elem").boldReportDesigner({...})   — designer
 *   $("#elem").boldReportViewer({...})     — viewer
 * Instance retrieved via: $("#elem").data("boldReportDesigner")
 *
 * The Bold Reports JavaScript libraries must be loaded in index.html via CDN or local files
 * before these functions are called.
 */

window.boldReportsInterop = (function () {
    'use strict';

    // Track initialised component instances (elementId → true) for cleanup.
    const _instances = {};

    // Guard to ensure the auth interceptors are installed exactly once per page lifetime.
    let _authInterceptorInstalled = false;

    /**
     * Initialises the Bold Reports Report Designer inside the given element.
     *
     * @param {string}      elementId  - ID of the container element.
     * @param {string}      serviceUrl - URL of the server-side designer service (/api/v1/boldreports/designer).
     * @param {string|null} reportPath - GUID of an existing report to load, or null for a new (blank) report.
     * @param {object}      dotNetRef  - DotNet object reference for save callbacks.
     */
    function initReportDesigner(elementId, serviceUrl, reportPath, dotNetRef) {
        _ensureAuthInterceptor();
        try {
            const el = document.getElementById(elementId);
            if (!el) {
                console.error('[BoldReports] Container element not found:', elementId);
                return;
            }

            if (typeof $.fn.boldReportDesigner === 'undefined') {
                console.error('[BoldReports] $.fn.boldReportDesigner is not defined. ' +
                    'Ensure the Bold Reports CDN scripts are loaded in index.html.');
                return;
            }

            // Destroy any existing instance in this container.
            _destroyInstance(elementId);

            const options = {
                serviceUrl: serviceUrl,
            };

            // Load an existing report when a GUID is provided.
            if (reportPath) {
                options.reportPath = reportPath;
            }

            // ── Save handling ──────────────────────────────────────────────────
            // The designer POSTs to SetData on the server when the user saves.
            // SetData persists the RDLC directly to the database.
            // We listen for the reportSaved event only to show a success notification
            // in the Blazor UI. We pass the reportPath (GUID) so the page can
            // refresh its local report metadata if needed.
            options.reportSaved = function (args) {
                if (!dotNetRef) return;
                if (args && args.status === true) {
                    dotNetRef.invokeMethodAsync('OnReportSaved', reportPath || '')
                        .catch(err => console.error('[BoldReports] OnReportSaved callback error:', err));
                } else if (args && args.status === false) {
                    console.error('[BoldReports] Save failed:', args.message);
                }
            };

            $('#' + elementId).boldReportDesigner(options);
            _instances[elementId] = true;
        } catch (err) {
            console.error('[BoldReports] initReportDesigner error:', err);
        }
    }

    /**
     * Initialises the Bold Reports Report Viewer inside the given element.
     *
     * @param {string}      elementId  - ID of the container element.
     * @param {string}      serviceUrl - URL of the server-side viewer service (/api/v1/boldreports/viewer).
     * @param {string}      reportPath - GUID of the report to display.
     * @param {object|null} params     - Optional dictionary of report parameter values.
     */
    function initReportViewer(elementId, serviceUrl, reportPath, params) {
        _ensureAuthInterceptor();
        try {
            const el = document.getElementById(elementId);
            if (!el) {
                console.error('[BoldReports] Container element not found:', elementId);
                return;
            }

            if (typeof $.fn.boldReportViewer === 'undefined') {
                console.error('[BoldReports] $.fn.boldReportViewer is not defined. ' +
                    'Ensure the Bold Reports CDN scripts are loaded in index.html.');
                return;
            }

            // Destroy any existing instance in this container.
            _destroyInstance(elementId);

            const options = {
                reportServiceUrl: serviceUrl,
                reportPath: reportPath,
            };

            if (params && Object.keys(params).length > 0) {
                options.parameters = Object.entries(params).map(([name, value]) => ({
                    name: name,
                    values: [String(value)],
                }));
            }

            $('#' + elementId).boldReportViewer(options);
            _instances[elementId] = true;
        } catch (err) {
            console.error('[BoldReports] initReportViewer error:', err);
        }
    }

    /**
     * Destroys and removes a Bold Reports component instance.
     *
     * @param {string} elementId - ID of the container element whose instance to destroy.
     */
    function destroyComponent(elementId) {
        _destroyInstance(elementId);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    /**
     * Installs JWT Bearer authentication interceptors for Bold Reports service requests.
     * Called once before the first designer or viewer is initialised.
     *
     * Bold Reports JavaScript SDK makes its own XHR/AJAX requests to the server-side
     * service URL.  These requests do NOT go through the Blazor HttpClient and therefore
     * do NOT carry the JWT stored in localStorage.
     * This function patches two request layers so that every request to a
     * /api/v1/boldreports/* endpoint automatically includes:
     *   Authorization: Bearer <token>
     *
     * Layer 1 – jQuery $.ajaxPrefilter  : covers jQuery $.ajax calls
     * Layer 2 – XMLHttpRequest prototype : covers any raw XHR calls
     */
    function _ensureAuthInterceptor() {
        if (_authInterceptorInstalled) return;
        _authInterceptorInstalled = true;

        const TOKEN_KEY    = 'auth_token';
        const BR_PATH_HINT = '/api/v1/boldreports';

        // ── Layer 1: jQuery AJAX prefilter ────────────────────────────────────
        if (typeof $ !== 'undefined' && typeof $.ajaxPrefilter === 'function') {
            $.ajaxPrefilter(function (options, _orig, jqXHR) {
                if (!options.url || options.url.indexOf(BR_PATH_HINT) < 0) return;
                const token = localStorage.getItem(TOKEN_KEY);
                if (token) jqXHR.setRequestHeader('Authorization', 'Bearer ' + token);
            });
        }

        // ── Layer 2: XHR prototype intercept ─────────────────────────────────
        const origOpen = XMLHttpRequest.prototype.open;
        XMLHttpRequest.prototype.open = function (method, url) {
            this._brUrl = (typeof url === 'string' ? url : String(url));
            return origOpen.apply(this, arguments);
        };

        const origSend = XMLHttpRequest.prototype.send;
        XMLHttpRequest.prototype.send = function () {
            if (this._brUrl && this._brUrl.indexOf(BR_PATH_HINT) >= 0) {
                const token = localStorage.getItem(TOKEN_KEY);
                if (token) {
                    try {
                        this.setRequestHeader('Authorization', 'Bearer ' + token);
                    } catch (_) {
                        // setRequestHeader throws if the request is already sent; safe to ignore.
                    }
                }
            }
            return origSend.apply(this, arguments);
        };
    }

    function _destroyInstance(elementId) {
        if (_instances[elementId]) {
            try {
                const $el = $('#' + elementId);
                // Try designer destroy first, then viewer.
                if ($el.data('boldReportDesigner')) {
                    $el.boldReportDesigner('destroy');
                } else if ($el.data('boldReportViewer')) {
                    $el.boldReportViewer('destroy');
                }
            } catch (err) {
                console.warn('[BoldReports] destroy error for', elementId, err);
            }
            delete _instances[elementId];
        }
    }

    return {
        initReportDesigner: initReportDesigner,
        initReportViewer: initReportViewer,
        destroyComponent: destroyComponent,
    };
}());
