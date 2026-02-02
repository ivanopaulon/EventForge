// Sidebar toggle functionality
document.addEventListener('DOMContentLoaded', function() {
    const wrapper = document.getElementById('wrapper');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebarToggleTop = document.getElementById('sidebarToggleTop');

    // Toggle sidebar on mobile (X button inside sidebar)
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function(e) {
            e.preventDefault();
            wrapper.classList.toggle('toggled');
        });
    }

    // Toggle sidebar from top navbar (hamburger button)
    if (sidebarToggleTop) {
        sidebarToggleTop.addEventListener('click', function(e) {
            e.preventDefault();
            wrapper.classList.toggle('toggled');
        });
    }

    // Close sidebar when clicking outside on mobile
    document.addEventListener('click', function(e) {
        if (window.innerWidth <= 768) {
            const sidebar = document.getElementById('sidebar-wrapper');
            const isClickInside = sidebar && sidebar.contains(e.target);
            const isToggleButton = sidebarToggleTop && sidebarToggleTop.contains(e.target);
            
            if (!isClickInside && !isToggleButton && !wrapper.classList.contains('toggled')) {
                wrapper.classList.add('toggled');
            }
        }
    });

    // Save sidebar state to localStorage (desktop only)
    const savedState = localStorage.getItem('sidebarToggled');
    if (savedState === 'true' && window.innerWidth > 768) {
        wrapper.classList.add('toggled');
    }

    // Save state on toggle
    const saveSidebarState = function() {
        if (window.innerWidth > 768) {
            localStorage.setItem('sidebarToggled', wrapper.classList.contains('toggled'));
        }
    };

    // Debounce state saving
    let saveTimeout;
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.attributeName === 'class') {
                clearTimeout(saveTimeout);
                saveTimeout = setTimeout(saveSidebarState, 300);
            }
        });
    });

    observer.observe(wrapper, { attributes: true });
});
