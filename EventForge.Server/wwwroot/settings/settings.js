// EventForge Settings Panel JavaScript

const API_BASE = '/api/v1/settings';
let authToken = null;

// Initialize on page load
document.addEventListener('DOMContentLoaded', async () => {
    console.log('Settings Panel Initializing...');
    
    // Get JWT token from localStorage or session (same key as used in login/logout)
    authToken = localStorage.getItem('serverToken') || sessionStorage.getItem('serverToken');
    
    if (!authToken) {
        showError('Authentication required. Please login as SuperAdmin.');
        // Redirect to login after 2 seconds
        setTimeout(() => {
            window.location.href = '/ServerAuth/Login?returnUrl=' + encodeURIComponent('/settings');
        }, 2000);
        return;
    }
    
    // Show logout button when authenticated
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.style.display = 'block';
    }
    
    // Setup tab switching
    setupTabs();
    
    // Load initial data
    await loadDashboardData();
    
    console.log('Settings Panel Ready');
});

// Handle logout
function handleLogout() {
    // Remove server token
    localStorage.removeItem('serverToken');
    sessionStorage.removeItem('serverToken');
    
    // Redirect to landing page
    window.location.href = '/';
}

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
            loadConfigurationForm();
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

// API call helper with method
async function apiCall(endpoint, method = 'GET', body = null) {
    const url = `${API_BASE}${endpoint}`;
    
    const options = {
        method: method,
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${authToken}`
        }
    };
    
    if (body && method !== 'GET') {
        options.body = JSON.stringify(body);
    }
    
    const response = await fetch(url, options);
    
    if (!response.ok) {
        if (response.status === 401) {
            showError('Unauthorized. Please login as SuperAdmin.');
            return null;
        }
        if (response.status === 403) {
            showError('Forbidden. SuperAdmin role required.');
            return null;
        }
        
        // Try to get error details from response
        try {
            const errorData = await response.json();
            throw new Error(errorData.message || errorData.Message || `API call failed: ${response.status}`);
        } catch {
            throw new Error(`API call failed: ${response.status} ${response.statusText}`);
        }
    }
    
    return await response.json();
}

// Load configuration form
async function loadConfigurationForm() {
    const loading = document.getElementById('config-loading');
    const form = document.getElementById('config-form');
    
    try {
        loading.style.display = 'block';
        form.style.display = 'none';
        
        const config = await apiCall('/file');
        
        if (config && config.configured) {
            // Populate form with null checks
            document.getElementById('server-address').value = config.database?.serverAddress || '';
            document.getElementById('database-name').value = config.database?.databaseName || '';
            document.getElementById('auth-type').value = config.database?.authenticationType || 'SQL';
            document.getElementById('username').value = config.database?.username || '';
            document.getElementById('trust-cert').checked = config.database?.trustServerCertificate !== false;
            document.getElementById('enforce-https').checked = config.security?.enforceHttps !== false;
            document.getElementById('enable-hsts').checked = config.security?.enableHsts !== false;
            
            toggleAuthFields();
        }
        
        loading.style.display = 'none';
        form.style.display = 'block';
        
    } catch (error) {
        console.error('Error loading configuration:', error);
        loading.innerHTML = '<p class="error">Failed to load configuration</p>';
    }
}

// Toggle SQL auth fields based on auth type
function toggleAuthFields() {
    const authType = document.getElementById('auth-type').value;
    const sqlAuthFields = document.getElementById('sql-auth-fields');
    sqlAuthFields.style.display = authType === 'SQL' ? 'block' : 'none';
}

// Test connection
async function testConnection() {
    const btn = document.getElementById('test-connection-btn');
    const resultDiv = document.getElementById('test-result');
    const saveBtn = document.getElementById('save-config-btn');
    
    btn.disabled = true;
    btn.textContent = '🔄 Testing...';
    resultDiv.style.display = 'none';
    
    // Validate required fields
    const serverAddress = document.getElementById('server-address').value.trim();
    const databaseName = document.getElementById('database-name').value.trim();
    
    if (!serverAddress || !databaseName) {
        resultDiv.className = 'alert alert-error';
        resultDiv.innerHTML = '❌ Server address and database name are required';
        resultDiv.style.display = 'block';
        saveBtn.disabled = true;
        btn.disabled = false;
        btn.textContent = '🔌 Test Connection';
        return;
    }
    
    const requestData = {
        serverAddress: serverAddress,
        databaseName: databaseName,
        authenticationType: document.getElementById('auth-type').value,
        username: document.getElementById('username').value,
        password: document.getElementById('password').value,
        trustServerCertificate: document.getElementById('trust-cert').checked
    };
    
    try {
        const result = await apiCall('/test-connection', 'POST', requestData);
        
        if (!result) {
            resultDiv.className = 'alert alert-error';
            resultDiv.innerHTML = '❌ Authentication required. Please login.';
            resultDiv.style.display = 'block';
            saveBtn.disabled = true;
            return;
        }
        
        if (result.success) {
            resultDiv.className = 'alert alert-success';
            resultDiv.innerHTML = `✅ ${result.message}<br/><small>Server: ${result.serverVersion}</small>`;
            resultDiv.style.display = 'block';
            saveBtn.disabled = false;
        } else {
            resultDiv.className = 'alert alert-error';
            resultDiv.innerHTML = `❌ ${result.message}`;
            resultDiv.style.display = 'block';
            saveBtn.disabled = true;
        }
    } catch (error) {
        resultDiv.className = 'alert alert-error';
        resultDiv.innerHTML = `❌ Connection test failed: ${error.message || error}`;
        resultDiv.style.display = 'block';
        saveBtn.disabled = true;
    } finally {
        btn.disabled = false;
        btn.textContent = '🔌 Test Connection';
    }
}

// Save configuration
async function saveConfiguration() {
    // Validate required fields first
    const serverAddress = document.getElementById('server-address').value.trim();
    const databaseName = document.getElementById('database-name').value.trim();
    
    if (!serverAddress || !databaseName) {
        const resultDiv = document.getElementById('save-result');
        resultDiv.className = 'alert alert-error';
        resultDiv.innerHTML = '❌ Server address and database name are required';
        resultDiv.style.display = 'block';
        return;
    }
    
    if (!confirm('This will save the configuration to appsettings.json. A server restart will be required. Continue?')) {
        return;
    }
    
    const btn = document.getElementById('save-config-btn');
    const resultDiv = document.getElementById('save-result');
    const restartBtn = document.getElementById('restart-server-btn');
    
    btn.disabled = true;
    btn.textContent = '💾 Saving...';
    resultDiv.style.display = 'none';
    
    const requestData = {
        serverAddress: serverAddress,
        databaseName: databaseName,
        authenticationType: document.getElementById('auth-type').value,
        username: document.getElementById('username').value,
        password: document.getElementById('password').value,
        trustServerCertificate: document.getElementById('trust-cert').checked,
        enforceHttps: document.getElementById('enforce-https').checked,
        enableHsts: document.getElementById('enable-hsts').checked
    };
    
    try {
        const result = await apiCall('/save', 'POST', requestData);
        
        if (!result) {
            resultDiv.className = 'alert alert-error';
            resultDiv.innerHTML = '❌ Authentication required. Please login.';
            resultDiv.style.display = 'block';
            return;
        }
        
        if (result.success) {
            resultDiv.className = 'alert alert-success';
            resultDiv.innerHTML = `✅ ${result.message}<br/><small>Backup: ${result.backupPath}</small>`;
            resultDiv.style.display = 'block';
            restartBtn.style.display = 'inline-block';
        } else {
            resultDiv.className = 'alert alert-error';
            resultDiv.innerHTML = `❌ Failed to save: ${result.message}`;
            resultDiv.style.display = 'block';
        }
    } catch (error) {
        resultDiv.className = 'alert alert-error';
        resultDiv.innerHTML = `❌ Save failed: ${error.message || error}`;
        resultDiv.style.display = 'block';
    } finally {
        btn.disabled = false;
        btn.textContent = '💾 Save Configuration';
    }
}

// Restart server
async function restartServer() {
    if (!confirm('This will restart the server. You will need to refresh this page in ~10 seconds. Continue?')) {
        return;
    }
    
    const btn = document.getElementById('restart-server-btn');
    btn.disabled = true;
    btn.textContent = '🔄 Restarting...';
    
    try {
        await apiCall('/restart', 'POST');
        
        // Show countdown
        let countdown = 10;
        const interval = setInterval(() => {
            btn.textContent = `🔄 Restarting... (${countdown}s)`;
            countdown--;
            
            if (countdown === 0) {
                clearInterval(interval);
                window.location.reload();
            }
        }, 1000);
        
    } catch (error) {
        btn.disabled = false;
        btn.textContent = '🔄 Restart Server Now';
        alert('Restart failed: ' + (error.message || error));
    }
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

// Setup event listeners when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Configuration tab
    const authTypeSelect = document.getElementById('auth-type');
    if (authTypeSelect) {
        authTypeSelect.addEventListener('change', toggleAuthFields);
    }
    
    const testBtn = document.getElementById('test-connection-btn');
    if (testBtn) {
        testBtn.addEventListener('click', testConnection);
    }
    
    const saveBtn = document.getElementById('save-config-btn');
    if (saveBtn) {
        saveBtn.addEventListener('click', saveConfiguration);
    }
    
    const restartBtn = document.getElementById('restart-server-btn');
    if (restartBtn) {
        restartBtn.addEventListener('click', restartServer);
    }
});
