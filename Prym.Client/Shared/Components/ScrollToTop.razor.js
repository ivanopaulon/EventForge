/**
 * Initializes a scroll listener on the window and notifies the Blazor component
 * when the user scrolls past the given threshold.
 * @param {object} dotNetRef - DotNet object reference for invoking Blazor callbacks
 * @param {number} threshold - Scroll Y offset in pixels before the button becomes visible
 * @returns {function} cleanup function to remove the listener
 */
export function initScrollListener(dotNetRef, threshold) {
    function onScroll() {
        const visible = window.scrollY > threshold;
        dotNetRef.invokeMethodAsync('OnScrollChanged', visible);
    }
    window.addEventListener('scroll', onScroll, { passive: true });
    // Return a token so we can remove it later
    return { dispose: () => window.removeEventListener('scroll', onScroll) };
}

/**
 * Scrolls the window smoothly to the top.
 */
export function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
}
