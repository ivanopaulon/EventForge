// EventForge Settings Panel JavaScript

const API_BASE = '/api/v1/settings';
let authToken = null;

// Initialize on page load
document.addEventListener('DOMContentLoaded', async () => {
    console.log('Settings Panel Initializing...');
    
    // Get JWT token from localStorage or session
    authToken = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
    
    if (!authToken) {
        showError('Authentication required. Please login as SuperAdmin.');
        return;
    }
    
    // Setup tab switching
    setupTabs();
    
    // Load initial data
    await loadDashboardData();
    
    console.log('Settings Panel Ready');
});

// Setup tab switching
function setupTabs() {
    const tabs = document.querySelectorAll('.tab');
    tabs.forEach(tab => {
        tab.addEventListener('click', () => {
            const tabName = tab.getAttribute('data-tab');
            switchTab(tabName);
        });
    });
}

// Switch between tabs
function switchTab(tabName) {
    // Update tab buttons
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelector(`[data-tab="${tabName}"]`)?.classList.add('active');
    
    // Update tab content
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    document.getElementById(`${tabName}-tab`)?.classList.add('active');
    
    // Load tab-specific data
    switch(tabName) {
        case 'dashboard':
            loadDashboardData();
            break;
        case 'configuration':
            loadConfigurations();
            break;
        case 'database':
            loadDatabaseStatus();
            break;
        case 'audit':
            loadAuditLogs();
            break;
    }
}

// Load dashboard data
async function loadDashboardData() {
    try {
        // Load restart status for uptime
        const restartStatus = await apiCall('/server/restart/status');
        if (restartStatus) {
            updateDashboardStatus(restartStatus);
        }
        
        // Load database status
        const dbStatus = await apiCall('/database/status');
        if (dbStatus) {
            updateDatabaseStatusDashboard(dbStatus);
        }
    } catch (error) {
        console.error('Error loading dashboard:', error);
    }
}

// Update dashboard status display
function updateDashboardStatus(status) {
    const envName = document.getElementById('env-name');
    const uptime = document.getElementById('uptime');
    
    if (envName) {
        envName.textContent = status.environment || 'Unknown';
    }
    
    if (uptime && status.uptime) {
        // Convert uptime from timespan string to readable format
        const hours = Math.floor(status.uptime.hours || 0);
        const minutes = Math.floor(status.uptime.minutes || 0);
        uptime.textContent = `${hours}h ${minutes}m`;
    }
    
    // Show restart banner if needed
    if (status.restartRequired && status.pendingChanges?.length > 0) {
        showRestartBanner(status.pendingChanges);
    }
}

// Update database status on dashboard
function updateDatabaseStatusDashboard(dbStatus) {
    const statusEl = document.getElementById('db-status');
    if (statusEl) {
        if (dbStatus.isConnected) {
            statusEl.innerHTML = '<span class="status-connected">✓ Connected</span>';
        } else {
            statusEl.innerHTML = '<span class="status-disconnected">✗ Disconnected</span>';
        }
    }
}

// Load configuration settings
async function loadConfigurations() {
    const loading = document.getElementById('config-loading');
    const list = document.getElementById('config-list');
    
    try {
        loading.style.display = 'block';
        list.innerHTML = '';
        
        const configs = await apiCall('/configuration');
        
        loading.style.display = 'none';
        
        if (!configs || configs.length === 0) {
            list.innerHTML = '<p class="help-text">No configuration settings found.</p>';
            return;
        }
        
        // Group by category
        const grouped = {};
        configs.forEach(config => {
            if (!grouped[config.category]) {
                grouped[config.category] = [];
            }
            grouped[config.category].push(config);
        });
        
        // Render grouped configs
        Object.keys(grouped).sort().forEach(category => {
            const categoryDiv = document.createElement('div');
            categoryDiv.className = 'config-category';
            categoryDiv.innerHTML = `<h3>${category}</h3>`;
            
            grouped[category].forEach(config => {
                const item = document.createElement('div');
                item.className = 'config-item';
                
                const icon = config.canHotReload ? '🔄' : '⚠️';
                const reloadText = config.canHotReload ? 'Hot Reload' : 'Requires Restart';
                
                item.innerHTML = `
                    <div>
                        <strong>${config.key}</strong>
                        <span style="margin-left: 10px; font-size: 0.9em;">${icon} ${reloadText}</span>
                    </div>
                    ${config.description ? `<p style="margin-top: 5px; color: #666; font-size: 0.9em;">${config.description}</p>` : ''}
                    <div class="config-value">${config.value}</div>
                    <div class="config-meta">
                        Version: ${config.version} | 
                        Modified: ${config.modifiedAt ? new Date(config.modifiedAt).toLocaleString() : 'Never'} |
                        By: ${config.modifiedBy || config.createdBy}
                    </div>
                `;
                
                categoryDiv.appendChild(item);
            });
            
            list.appendChild(categoryDiv);
        });
    } catch (error) {
        loading.style.display = 'none';
        list.innerHTML = '<p class="help-text">Error loading configurations. Please try again.</p>';
        console.error('Error loading configurations:', error);
    }
}

// Load database status
async function loadDatabaseStatus() {
    try {
        const status = await apiCall('/database/status');
        
        document.getElementById('db-connection-status').innerHTML = 
            status.isConnected ? 
            '<span class="status-connected">✓ Connected</span>' : 
            '<span class="status-disconnected">✗ Disconnected</span>';
        
        document.getElementById('db-provider').textContent = status.provider || '-';
        document.getElementById('db-name').textContent = status.databaseName || '-';
    } catch (error) {
        document.getElementById('db-connection-status').innerHTML = 
            '<span class="status-disconnected">✗ Error</span>';
        console.error('Error loading database status:', error);
    }
}

// Load audit logs
async function loadAuditLogs() {
    const loading = document.getElementById('audit-loading');
    const list = document.getElementById('audit-list');
    
    try {
        loading.style.display = 'block';
        list.innerHTML = '';
        
        const logs = await apiCall('/audit?page=1&pageSize=50');
        
        loading.style.display = 'none';
        
        if (!logs || logs.length === 0) {
            list.innerHTML = '<p class="help-text">No audit logs found.</p>';
            return;
        }
        
        logs.forEach(log => {
            const item = document.createElement('div');
            item.className = 'audit-item';
            
            const statusIcon = log.success ? '✓' : '✗';
            const statusColor = log.success ? '#27ae60' : '#e74c3c';
            
            item.innerHTML = `
                <div>
                    <strong style="color: ${statusColor};">${statusIcon} ${log.action}</strong>
                    <span style="margin-left: 10px; color: #666;">| ${log.operationType}</span>
                </div>
                <p style="margin-top: 5px;">${log.description}</p>
                <div class="config-meta">
                    ${new Date(log.executedAt).toLocaleString()} | ${log.executedBy}
                    ${log.entityType ? ` | ${log.entityType}` : ''}
                </div>
            `;
            
            list.appendChild(item);
        });
    } catch (error) {
        loading.style.display = 'none';
        list.innerHTML = '<p class="help-text">Error loading audit logs. Please try again.</p>';
        console.error('Error loading audit logs:', error);
    }
}

// Show restart banner
function showRestartBanner(changes) {
    const banner = document.getElementById('restart-banner');
    const message = document.getElementById('restart-message');
    
    message.textContent = `${changes.length} configuration change(s) require server restart: ${changes.join(', ')}`;
    banner.style.display = 'flex';
}

// API call helper
async function apiCall(endpoint, options = {}) {
    const url = `${API_BASE}${endpoint}`;
    
    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${authToken}`
        }
    };
    
    const response = await fetch(url, { ...defaultOptions, ...options });
    
    if (!response.ok) {
        if (response.status === 401) {
            showError('Unauthorized. Please login as SuperAdmin.');
            return null;
        }
        if (response.status === 403) {
            showError('Forbidden. SuperAdmin role required.');
            return null;
        }
        throw new Error(`API call failed: ${response.status} ${response.statusText}`);
    }
    
    return await response.json();
}

// Show error message
function showError(message) {
    const container = document.querySelector('.settings-container');
    container.innerHTML = `
        <div class="card" style="margin: 50px; padding: 40px; text-align: center;">
            <h2 style="color: #e74c3c;">⚠️ ${message}</h2>
            <p style="margin-top: 20px; color: #666;">
                The Settings Panel requires SuperAdmin authentication.
            </p>
        </div>
    `;
}
