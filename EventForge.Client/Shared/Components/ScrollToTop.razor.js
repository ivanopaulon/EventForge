// Module-level reference so destroyScrollListener can remove the exact handler.
let _scrollHandler = null;

/**
 * Initializes a scroll listener on the window and notifies the Blazor component
 * when the user scrolls past the given threshold.
 * @param {object} dotNetRef - DotNet object reference for invoking Blazor callbacks
 * @param {number} threshold - Scroll Y offset in pixels before the button becomes visible
 */
export function initScrollListener(dotNetRef, threshold) {
    // Remove any previous listener before registering a new one (e.g. hot-reload).
    destroyScrollListener();

    _scrollHandler = function onScroll() {
        const visible = window.scrollY > threshold;
        dotNetRef.invokeMethodAsync('OnScrollChanged', visible);
    };
    window.addEventListener('scroll', _scrollHandler, { passive: true });
}

/**
 * Removes the scroll listener registered by initScrollListener.
 * Must be called before the DotNetObjectReference is disposed to avoid
 * "There is no tracked object" errors from stale callbacks.
 */
export function destroyScrollListener() {
    if (_scrollHandler) {
        window.removeEventListener('scroll', _scrollHandler);
        _scrollHandler = null;
    }
}

/**
 * Scrolls the window smoothly to the top.
 */
export function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
}
