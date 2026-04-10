/**
 * Camera Barcode Scanner
 *
 * Strategy (no paid libraries):
 *   1. BarcodeDetector API  – native, built into Chrome on Android and recent desktop Chrome/Edge
 *   2. @zxing/browser       – loaded on-demand from jsDelivr CDN (Apache-2.0, free)
 *                             used as fallback for iOS Safari and any browser without BarcodeDetector
 *
 * Public API (called from Blazor via IJSRuntime):
 *   window.checkCameraScannerSupport()
 *   window.getCameraDevices()
 *   window.initCameraScanner(videoId, canvasId, dotNetHelper, preferBackCamera, deviceId?)
 *   window.stopCameraScanner()
 *   window.switchCamera(videoId, canvasId, dotNetHelper, preferBackCamera, deviceId?)
 *   window.toggleTorch(enable)
 */
(function () {
    'use strict';

    const ZXING_CDN = 'https://cdn.jsdelivr.net/npm/@zxing/browser@0.1.5/umd/index.min.js';
    const DEBOUNCE_MS = 1500;

    let state = {
        stream: null,
        videoEl: null,
        canvasEl: null,
        dotNetHelper: null,
        rafId: null,
        isScanning: false,
        lastCode: null,
        lastCodeTime: 0,
        barcodeDetector: null,
        zxingContinuousReader: null
    };

    let zxingLoadPromise = null;

    // ── Capability detection ──────────────────────────────────────────────────

    const hasBarcodeDetector = typeof BarcodeDetector !== 'undefined';

    function loadZXing() {
        if (zxingLoadPromise) return zxingLoadPromise;
        if (window.ZXingBrowser) return Promise.resolve();
        zxingLoadPromise = new Promise((resolve, reject) => {
            const s = document.createElement('script');
            s.src = ZXING_CDN;
            s.onload = resolve;
            s.onerror = () => reject(new Error('Failed to load ZXing from CDN: ' + ZXING_CDN));
            document.head.appendChild(s);
        });
        return zxingLoadPromise;
    }

    // ── Public helpers ────────────────────────────────────────────────────────

    window.checkCameraScannerSupport = async function () {
        const hasGetUserMedia = !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
        return {
            supported: hasGetUserMedia,
            hasNativeDetector: hasBarcodeDetector,
            isMobile: /Android|iPhone|iPad|iPod/i.test(navigator.userAgent)
        };
    };

    window.getCameraDevices = async function () {
        try {
            // Requesting permissions first so labels are populated
            const tmpStream = await navigator.mediaDevices.getUserMedia({ video: true }).catch(() => null);
            const devices = await navigator.mediaDevices.enumerateDevices();
            if (tmpStream) tmpStream.getTracks().forEach(t => t.stop());
            return devices
                .filter(d => d.kind === 'videoinput')
                .map((d, i) => ({ deviceId: d.deviceId, label: d.label || ('Camera ' + (i + 1)) }));
        } catch (e) {
            console.error('[CameraScanner] enumerateDevices error:', e);
            return [];
        }
    };

    // ── Main: init ────────────────────────────────────────────────────────────

    window.initCameraScanner = async function (videoId, canvasId, dotNetHelper, preferBackCamera, deviceId) {
        try {
            await stopInternal();

            const video = document.getElementById(videoId);
            const canvas = document.getElementById(canvasId);
            if (!video || !canvas) throw new Error('Video/canvas element not found');

            state.videoEl = video;
            state.canvasEl = canvas;
            state.dotNetHelper = dotNetHelper;
            state.lastCode = null;
            state.lastCodeTime = 0;

            const constraints = {
                video: deviceId
                    ? { deviceId: { exact: deviceId } }
                    : { facingMode: preferBackCamera ? 'environment' : 'user' }
            };

            state.stream = await navigator.mediaDevices.getUserMedia(constraints);
            video.srcObject = state.stream;

            await new Promise((resolve) => {
                video.onloadedmetadata = resolve;
                video.onerror = resolve;   // continue even on error
                setTimeout(resolve, 4000); // safety timeout
            });
            await video.play();

            state.isScanning = true;

            if (hasBarcodeDetector) {
                state.barcodeDetector = new BarcodeDetector({
                    formats: [
                        'qr_code', 'ean_13', 'ean_8', 'code_128', 'code_39',
                        'code_93', 'itf', 'upc_a', 'upc_e', 'aztec', 'pdf417', 'data_matrix'
                    ]
                });
                startNativeLoop();
            } else {
                await loadZXing();
                if (!window.ZXingBrowser) throw new Error('ZXing library unavailable');
                state.zxingContinuousReader = new window.ZXingBrowser.BrowserMultiFormatReader();
                startZXingLoop();
            }

            return { success: true, usingNativeDetector: hasBarcodeDetector };
        } catch (err) {
            console.error('[CameraScanner] init error:', err);
            return { success: false, error: err.message };
        }
    };

    window.stopCameraScanner = async function () {
        await stopInternal();
    };

    window.switchCamera = async function (videoId, canvasId, dotNetHelper, preferBackCamera, deviceId) {
        return window.initCameraScanner(videoId, canvasId, dotNetHelper, preferBackCamera, deviceId);
    };

    window.toggleTorch = async function (enable) {
        if (!state.stream) return false;
        const track = state.stream.getVideoTracks()[0];
        if (!track) return false;
        try {
            const caps = track.getCapabilities();
            if (caps && caps.torch) {
                await track.applyConstraints({ advanced: [{ torch: enable }] });
                return true;
            }
        } catch (e) {
            console.warn('[CameraScanner] torch error:', e);
        }
        return false;
    };

    // ── Scanning loops ────────────────────────────────────────────────────────

    function startNativeLoop() {
        async function frame() {
            if (!state.isScanning) return;
            try {
                if (state.videoEl.readyState >= state.videoEl.HAVE_ENOUGH_DATA) {
                    const codes = await state.barcodeDetector.detect(state.videoEl);
                    if (codes.length > 0) await onCode(codes[0].rawValue);
                }
            } catch (_) { /* frame error – continue */ }
            if (state.isScanning) state.rafId = requestAnimationFrame(frame);
        }
        state.rafId = requestAnimationFrame(frame);
    }

    function startZXingLoop() {
        const ctx = state.canvasEl.getContext('2d', { willReadFrequently: true });

        function frame() {
            if (!state.isScanning) return;
            try {
                const video = state.videoEl;
                const canvas = state.canvasEl;
                if (video.readyState >= video.HAVE_ENOUGH_DATA && video.videoWidth > 0) {
                    canvas.width = video.videoWidth;
                    canvas.height = video.videoHeight;
                    ctx.drawImage(video, 0, 0);

                    const luminance = new window.ZXingBrowser.HTMLCanvasElementLuminanceSource(canvas);
                    const bitmap = new window.ZXingBrowser.BinaryBitmap(
                        new window.ZXingBrowser.HybridBinarizer(luminance)
                    );
                    try {
                        const result = state.zxingContinuousReader.decodeBitmap(bitmap);
                        if (result) {
                            onCode(result.getText()).finally(() => {
                                if (state.isScanning) state.rafId = requestAnimationFrame(frame);
                            });
                            return;
                        }
                    } catch (_) { /* NotFoundException – no barcode in this frame */ }
                }
            } catch (_) { /* frame error – continue */ }
            if (state.isScanning) state.rafId = requestAnimationFrame(frame);
        }
        state.rafId = requestAnimationFrame(frame);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    async function onCode(code) {
        const now = Date.now();
        if (code === state.lastCode && (now - state.lastCodeTime) < DEBOUNCE_MS) return;
        state.lastCode = code;
        state.lastCodeTime = now;

        if (navigator.vibrate) navigator.vibrate(50);
        if (window.playBeep) window.playBeep('success');

        if (state.dotNetHelper) {
            try {
                await state.dotNetHelper.invokeMethodAsync('OnBarcodeDetectedFromJs', code);
            } catch (e) {
                console.warn('[CameraScanner] dotNet callback error:', e);
            }
        }
    }

    async function stopInternal() {
        state.isScanning = false;

        if (state.rafId) {
            cancelAnimationFrame(state.rafId);
            state.rafId = null;
        }
        if (state.zxingContinuousReader) {
            try { state.zxingContinuousReader.reset(); } catch (_) { }
            state.zxingContinuousReader = null;
        }
        if (state.stream) {
            state.stream.getTracks().forEach(t => t.stop());
            state.stream = null;
        }
        if (state.videoEl) {
            state.videoEl.srcObject = null;
            state.videoEl = null;
        }
        state.barcodeDetector = null;
        state.dotNetHelper = null;
    }

})();
