/**
 * Audit Log Viewer JavaScript
 * Handles detail panel, export, and interactions
 */

// Show audit log detail in offcanvas
async function showAuditDetail(logId) {
    const offcanvas = new bootstrap.Offcanvas(document.getElementById('auditDetailPanel'));
    const contentDiv = document.getElementById('auditDetailContent');
    
    // Show loading state
    contentDiv.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    `;
    
    offcanvas.show();
    
    try {
        // Fetch detail from server
        const response = await fetch(`/dashboard/auditlog/detail/${logId}`);
        
        if (!response.ok) {
            throw new Error('Failed to load audit log detail');
        }
        
        const html = await response.text();
        contentDiv.innerHTML = html;
    } catch (error) {
        console.error('Error loading audit detail:', error);
        contentDiv.innerHTML = `
            <div class="alert alert-danger">
                <i class="bi bi-exclamation-triangle"></i> Failed to load audit log detail.
            </div>
        `;
    }
}

// Export audit logs to CSV
async function exportAuditLog() {
    const form = document.getElementById('auditFilterForm');
    const formData = new FormData(form);
    
    // Build query string from current filters
    const params = new URLSearchParams(formData);
    
    try {
        // Show loading indicator
        const btn = event.target.closest('button');
        const originalHTML = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Exporting...';
        
        // Call export endpoint
        const response = await fetch(`/dashboard/auditlog/export?${params.toString()}`);
        
        if (!response.ok) {
            throw new Error('Export failed');
        }
        
        // Download CSV file
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `audit_log_${new Date().toISOString().split('T')[0]}.csv`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        
        // Restore button
        btn.disabled = false;
        btn.innerHTML = originalHTML;
        
        // Show success message
        showToast('Audit log exported successfully!', 'success');
    } catch (error) {
        console.error('Export error:', error);
        showToast('Failed to export audit log. Please try again.', 'danger');
        
        // Restore button
        const btn = event.target.closest('button');
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-download"></i> Export CSV';
    }
}

// Toast notification helper
function showToast(message, type = 'info') {
    const toastDiv = document.createElement('div');
    toastDiv.className = `alert alert-${type} position-fixed top-0 start-50 translate-middle-x mt-3`;
    toastDiv.style.zIndex = '9999';
    toastDiv.innerHTML = message;
    document.body.appendChild(toastDiv);
    
    setTimeout(() => {
        toastDiv.remove();
    }, 3000);
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    console.log('Audit Log Viewer initialized');
});
