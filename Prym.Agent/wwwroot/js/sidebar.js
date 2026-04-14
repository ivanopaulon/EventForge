(function () {
  'use strict';

  const wrapper        = document.getElementById('wrapper');
  const toggleTop      = document.getElementById('sidebarToggleTop');
  const toggleClose    = document.getElementById('sidebarToggle');
  const statusDot      = document.getElementById('topStatusDot');

  /* ── Sidebar toggle ───────────────────────────────────────────── */
  function toggleSidebar(e) {
    if (e) e.preventDefault();
    wrapper.classList.toggle('toggled');
    if (window.innerWidth > 991) {
      localStorage.setItem('ef_agent_sidebar_collapsed', wrapper.classList.contains('toggled'));
    }
  }

  if (toggleTop)   toggleTop.addEventListener('click', toggleSidebar);
  if (toggleClose) toggleClose.addEventListener('click', toggleSidebar);

  // Close on overlay click (mobile)
  wrapper.addEventListener('click', function (e) {
    if (window.innerWidth <= 991 &&
        wrapper.classList.contains('toggled') &&
        !document.getElementById('sidebar-wrapper').contains(e.target) &&
        e.target !== toggleTop) {
      wrapper.classList.remove('toggled');
    }
  });

  // Restore desktop state
  if (window.innerWidth > 991 && localStorage.getItem('ef_agent_sidebar_collapsed') === 'true') {
    wrapper.classList.add('toggled');
  }

  // Mobile: start collapsed
  if (window.innerWidth <= 991) {
    wrapper.classList.remove('toggled');
  }

  /* ── Status dot ───────────────────────────────────────────────── */
  if (statusDot) {
    fetch('/api/agent/status', { cache: 'no-store' })
      .then(r => {
        if (!r.ok) { statusDot.classList.add('degraded'); return null; }
        return r.json();
      })
      .then(data => {
        if (!data) return;
        const state = (data.hubConnectionState || '').toLowerCase();
        if (state === 'connected') {
          statusDot.classList.remove('error', 'degraded');
        } else if (state === 'reconnecting') {
          statusDot.classList.remove('error');
          statusDot.classList.add('degraded');
        } else {
          statusDot.classList.remove('degraded');
          statusDot.classList.add('error');
        }
      })
      .catch(() => statusDot.classList.add('error'));
  }

  /* ── Auto-dismiss alerts ──────────────────────────────────────── */
  document.querySelectorAll('.alert.alert-success').forEach(function (el) {
    setTimeout(function () {
      el.style.transition = 'opacity .4s';
      el.style.opacity    = '0';
      setTimeout(function () { el.remove(); }, 420);
    }, 4000);
  });

})();
