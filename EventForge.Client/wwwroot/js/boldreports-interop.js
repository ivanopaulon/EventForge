/**
 * boldreports-interop.js
 * JS Interop helpers for the Bold Reports JavaScript designer and viewer components.
 * Called from ReportDesignerComponent.razor and ReportViewerComponent.razor via IJSRuntime.
 *
 * The Bold Reports JavaScript libraries must be loaded in index.html via CDN or local files
 * before these functions are called.
 */

window.boldReportsInterop = (function () {
    'use strict';

    // Track initialised component instances for cleanup.
    const _instances = {};

    /**
     * Initialises the Bold Reports Report Designer inside the given element.
     *
     * @param {string}  elementId   - ID of the container element.
     * @param {string}  serviceUrl  - URL of the server-side designer service (/api/v1/boldreports/designer).
     * @param {string|null} reportPath - GUID of an existing report to load, or null for a new report.
     * @param {object}  dotNetRef   - DotNet object reference for save callbacks.
     */
    function initReportDesigner(elementId, serviceUrl, reportPath, dotNetRef) {
        try {
            const el = document.getElementById(elementId);
            if (!el) {
                console.error('[BoldReports] Container element not found:', elementId);
                return;
            }

            if (typeof BoldReportDesigner === 'undefined') {
                console.error('[BoldReports] BoldReportDesigner is not defined. ' +
                    'Ensure the Bold Reports CDN scripts are loaded in index.html.');
                return;
            }

            // Destroy any existing instance in this container.
            _destroyInstance(elementId);

            const options = {
                serviceUrl: serviceUrl,
                width: '100%',
                height: '100%',
            };

            if (reportPath) {
                options.reportPath = reportPath;
            }

            // Callback invoked when the user clicks Save in the designer toolbar.
            options.reportSave = function (args) {
                if (dotNetRef && args && args.rdlData) {
                    dotNetRef.invokeMethodAsync('OnReportSaved', args.rdlData)
                        .catch(err => console.error('[BoldReports] Save callback error:', err));
                }
            };

            const instance = new BoldReportDesigner(options);
            instance.appendTo('#' + elementId);
            _instances[elementId] = instance;
        } catch (err) {
            console.error('[BoldReports] initReportDesigner error:', err);
        }
    }

    /**
     * Initialises the Bold Reports Report Viewer inside the given element.
     *
     * @param {string}  elementId   - ID of the container element.
     * @param {string}  serviceUrl  - URL of the server-side viewer service (/api/v1/boldreports/viewer).
     * @param {string}  reportPath  - GUID of the report to display.
     * @param {object|null} params  - Optional dictionary of report parameter values.
     */
    function initReportViewer(elementId, serviceUrl, reportPath, params) {
        try {
            const el = document.getElementById(elementId);
            if (!el) {
                console.error('[BoldReports] Container element not found:', elementId);
                return;
            }

            if (typeof BoldReportViewer === 'undefined') {
                console.error('[BoldReports] BoldReportViewer is not defined. ' +
                    'Ensure the Bold Reports CDN scripts are loaded in index.html.');
                return;
            }

            // Destroy any existing instance in this container.
            _destroyInstance(elementId);

            const options = {
                reportServiceUrl: serviceUrl,
                reportPath: reportPath,
                width: '100%',
                height: '100%',
            };

            if (params && Object.keys(params).length > 0) {
                options.parameters = Object.entries(params).map(([name, value]) => ({
                    name: name,
                    values: [String(value)],
                }));
            }

            const instance = new BoldReportViewer(options);
            instance.appendTo('#' + elementId);
            _instances[elementId] = instance;
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

    function _destroyInstance(elementId) {
        const existing = _instances[elementId];
        if (existing) {
            try {
                if (typeof existing.destroy === 'function') {
                    existing.destroy();
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
