(function () {
  'use strict';

  /* ── State ──────────────────────────────────────────────────────────────── */
  var _busy           = false;
  var _formSubmitting = false;

  /* ── Public API (usable by inline scripts for AJAX-driven operations) ───── */
  window.efNavGuard = {
    activate:   function (message) { _busy = true;  _formSubmitting = false; _showOverlay(message); },
    deactivate: function ()        { _busy = false; _formSubmitting = false; _hideOverlay(); }
  };

  /* ── Overlay helpers ────────────────────────────────────────────────────── */
  function _showOverlay(message) {
    var overlay = document.getElementById('ef-nav-guard-overlay');
    if (!overlay) return;
    if (message) {
      var msgEl = overlay.querySelector('.ef-nav-guard-msg');
      if (msgEl) msgEl.textContent = message;
    }
    overlay.style.display = 'flex';
  }

  function _hideOverlay() {
    var overlay = document.getElementById('ef-nav-guard-overlay');
    if (overlay) overlay.style.display = 'none';
  }

  /* ── Guard all POST form submissions ────────────────────────────────────── */
  document.addEventListener('submit', function (e) {
    var form = e.target;
    if ((form.method || 'get').toLowerCase() !== 'post') return;
    _busy           = true;
    _formSubmitting = true;
    _showOverlay();
  });

  /* ── Block sidebar / nav-link clicks while busy (capture phase) ─────────── */
  document.addEventListener('click', function (e) {
    if (!_busy) return;
    var link = e.target.closest('a[href]');
    if (!link) return;
    var href = link.getAttribute('href') || '';
    if (!href || href === '#' || href.startsWith('javascript:') || link.target === '_blank') return;
    e.preventDefault();
    e.stopPropagation();
  }, true);

  /* ── Warn on browser close / back button (only if not a form submission) ── */
  window.addEventListener('beforeunload', function (e) {
    if (_busy && !_formSubmitting) {
      e.preventDefault();
      return (e.returnValue = '');
    }
  });

})();
