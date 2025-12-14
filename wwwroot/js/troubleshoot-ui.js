/**
 * Barangay Health Center - UI Troubleshooting Tool
 * This file can be included temporarily to diagnose UI issues.
 * 
 * Usage: Add the following line to _AdminLayout.cshtml right before the closing </body> tag:
 * <script src="~/js/troubleshoot-ui.js"></script>
 */

(function() {
    // Create a troubleshooting UI panel
    function createTroubleshootingPanel() {
        const panel = document.createElement('div');
        panel.id = 'bhc-troubleshoot-panel';
        panel.style.cssText = `
            position: fixed;
            bottom: 0;
            right: 0;
            width: 350px;
            max-height: 500px;
            background-color: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 6px 0 0 0;
            box-shadow: 0 -2px 10px rgba(0,0,0,0.1);
            z-index: 9999;
            overflow: hidden;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            font-size: 13px;
            transition: all 0.3s ease;
        `;
        
        // Create header
        const header = document.createElement('div');
        header.style.cssText = `
            background-color: #0066cc;
            color: white;
            padding: 8px 12px;
            font-weight: 600;
            display: flex;
            justify-content: space-between;
            align-items: center;
            cursor: pointer;
        `;
        header.innerHTML = `
            <span>UI Diagnostic Tool</span>
            <div>
                <button id="bhc-run-tests" style="margin-right: 10px; padding: 2px 6px; background: #ffffff; border: none; border-radius: 3px; cursor: pointer;">Run Tests</button>
                <span id="bhc-toggle-panel" style="cursor: pointer;">▼</span>
            </div>
        `;
        
        // Create content area
        const content = document.createElement('div');
        content.id = 'bhc-troubleshoot-content';
        content.style.cssText = `
            padding: 10px;
            overflow-y: auto;
            max-height: 450px;
        `;
        
        // Add initial content
        content.innerHTML = `
            <div style="margin-bottom: 10px;">
                <h4 style="margin: 0 0 8px 0; font-size: 14px;">Diagnostic Tools</h4>
                <button id="bhc-check-notifications" class="bhc-button">Check Notifications</button>
                <button id="bhc-check-users" class="bhc-button">Check User Management</button>
                <button id="bhc-check-api" class="bhc-button">Test API Endpoints</button>
                <button id="bhc-show-logs" class="bhc-button">Show Console Logs</button>
            </div>
            <div id="bhc-results" style="border-top: 1px solid #dee2e6; padding-top: 10px;">
                <p>Click "Run Tests" to start diagnostics...</p>
            </div>
        `;
        
        // Add styles for buttons
        const style = document.createElement('style');
        style.textContent = `
            .bhc-button {
                background-color: #f0f0f0;
                border: 1px solid #ccc;
                border-radius: 4px;
                padding: 5px 8px;
                margin-right: 5px;
                margin-bottom: 5px;
                cursor: pointer;
                font-size: 12px;
            }
            .bhc-button:hover {
                background-color: #e0e0e0;
            }
            .bhc-status-ok {
                color: #28a745;
                margin-right: 5px;
            }
            .bhc-status-warning {
                color: #ffc107;
                margin-right: 5px;
            }
            .bhc-status-error {
                color: #dc3545;
                margin-right: 5px;
            }
            .bhc-toggle-details {
                cursor: pointer;
                color: #0066cc;
                margin-left: 5px;
                font-size: 11px;
            }
            .bhc-detail-section {
                background-color: #f0f0f0;
                border-radius: 3px;
                padding: 8px;
                margin-top: 5px;
                font-family: monospace;
                font-size: 12px;
                max-height: 200px;
                overflow-y: auto;
                display: none;
            }
            .bhc-collapsible {
                margin-bottom: 8px;
            }
        `;
        
        // Assemble the panel
        panel.appendChild(style);
        panel.appendChild(header);
        panel.appendChild(content);
        document.body.appendChild(panel);
        
        // Add toggle functionality
        let isPanelCollapsed = false;
        document.getElementById('bhc-toggle-panel').addEventListener('click', function() {
            if (isPanelCollapsed) {
                content.style.display = 'block';
                this.textContent = '▼';
                isPanelCollapsed = false;
            } else {
                content.style.display = 'none';
                this.textContent = '▲';
                isPanelCollapsed = true;
            }
        });
        
        // Add functionality to buttons
        document.getElementById('bhc-run-tests').addEventListener('click', runAllTests);
        document.getElementById('bhc-check-notifications').addEventListener('click', checkNotifications);
        document.getElementById('bhc-check-users').addEventListener('click', checkUserManagement);
        document.getElementById('bhc-check-api').addEventListener('click', testApiEndpoints);
        document.getElementById('bhc-show-logs').addEventListener('click', showConsoleLogs);
        
        console.log('BHC Troubleshooting tool initialized');
        return panel;
    }
    
    // Run all diagnostic tests
    function runAllTests() {
        const results = document.getElementById('bhc-results');
        results.innerHTML = '<p>Running diagnostics, please wait...</p>';
        
        // Run tests in sequence
        setTimeout(() => {
            results.innerHTML = '';
            
            // Check for JavaScript errors
            checkJavaScriptErrors();
            
            // Check notification system
            checkNotifications();
            
            // Check user management
            checkUserManagement();
            
            // Test API endpoints
            testApiEndpoints();
            
            // Add summary
            results.innerHTML += `
                <div style="margin-top: 15px; padding-top: 10px; border-top: 1px solid #dee2e6;">
                    <h4 style="margin: 0 0 8px 0; font-size: 14px;">Troubleshooting Complete</h4>
                    <p style="margin: 0;">See above results for details and recommendations.</p>
                </div>
            `;
        }, 500);
    }
    
    // Check for JavaScript errors
    function checkJavaScriptErrors() {
        const results = document.getElementById('bhc-results');
        let errorSection = document.createElement('div');
        errorSection.className = 'bhc-collapsible';
        
        const originalConsoleError = console.error;
        let errors = [];
        
        // Override console.error temporarily to capture errors
        console.error = function() {
            errors.push(Array.from(arguments).join(' '));
            originalConsoleError.apply(console, arguments);
        };
        
        // Restore original console.error after a short delay
        setTimeout(() => {
            console.error = originalConsoleError;
            
            if (errors.length > 0) {
                errorSection.innerHTML = `
                    <div><span class="bhc-status-error"></span> <strong>JavaScript Errors Detected</strong> <span class="bhc-toggle-details">[Show Details]</span></div>
                    <div class="bhc-detail-section">
                        <strong>${errors.length} errors found:</strong><br>
                        ${errors.map(e => `• ${e}`).join('<br>')}
                    </div>
                `;
            } else {
                errorSection.innerHTML = `
                    <div><span class="bhc-status-ok"></span> <strong>No JavaScript Errors Detected</strong></div>
                `;
            }
            
            results.appendChild(errorSection);
            
            // Add toggle functionality
            const toggleDetails = errorSection.querySelector('.bhc-toggle-details');
            if (toggleDetails) {
                toggleDetails.addEventListener('click', function() {
                    const details = errorSection.querySelector('.bhc-detail-section');
                    if (details.style.display === 'block') {
                        details.style.display = 'none';
                        this.textContent = '[Show Details]';
                    } else {
                        details.style.display = 'block';
                        this.textContent = '[Hide Details]';
                    }
                });
            }
        }, 100);
        
        // Intentionally trigger some problematic code to check error handling
        try {
            // Test notification system error handling
            if (typeof checkNotificationSystem === 'function') {
                checkNotificationSystem();
            }
            
            // Test user management error handling
            if (typeof userManagementModule !== 'undefined' && userManagementModule !== null) {
                userManagementModule.loadUsers();
            }
        } catch (e) {
            // Ignore - this is just to test error handling
        }
    }
    
    // Check notifications
    function checkNotifications() {
        const results = document.getElementById('bhc-results');
        let notificationSection = document.createElement('div');
        notificationSection.className = 'bhc-collapsible';
        
        // Check for notification elements
        const navbarNotificationBadge = document.getElementById('headerNotificationBadge');
        const notificationDropdown = document.querySelector('.notification-dropdown');
        const notificationList = document.getElementById('adminNotificationList');
        
        results.appendChild(notificationSection);
        notificationSection.innerHTML = `
            <div><span class="bhc-status-warning">⏳</span> <strong>Checking Notification System...</strong></div>
        `;
        
        // Fetch notifications directly to test the API
        fetch('/api/Notification')
            .then(response => {
                if (!response.ok) {
                    throw new Error(`API returned status ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                let issues = [];
                
                // Check for expected structure
                if (!data || typeof data.count === 'undefined') {
                    issues.push('API response missing expected structure (count field)');
                }
                
                if (!Array.isArray(data.notifications)) {
                    issues.push('API response missing notifications array');
                }
                
                // Check UI elements
                if (!navbarNotificationBadge) {
                    issues.push('Notification badge element (headerNotificationBadge) not found in the DOM');
                }
                
                if (!notificationDropdown) {
                    issues.push('Notification dropdown element not found in the DOM');
                }
                
                if (!notificationList) {
                    issues.push('Notification list element (adminNotificationList) not found in the DOM');
                }
                
                // Update result
                if (issues.length > 0) {
                    notificationSection.innerHTML = `
                        <div><span class="bhc-status-error"></span> <strong>Notification System Issues Detected</strong> <span class="bhc-toggle-details">[Show Details]</span></div>
                        <div class="bhc-detail-section">
                            <strong>${issues.length} issues found:</strong><br>
                            ${issues.map(e => `• ${e}`).join('<br>')}
                            <hr>
                            <strong>API Response:</strong><br>
                            <pre>${JSON.stringify(data, null, 2)}</pre>
                        </div>
                    `;
                } else {
                    notificationSection.innerHTML = `
                        <div><span class="bhc-status-ok"></span> <strong>Notification System OK</strong> <span class="bhc-toggle-details">[Show Details]</span></div>
                        <div class="bhc-detail-section">
                            <strong>Notification Count:</strong> ${data.count}<br>
                            <strong>Notifications:</strong> ${data.notifications?.length || 0} items<br>
                            <hr>
                            <pre>${JSON.stringify(data, null, 2)}</pre>
                        </div>
                    `;
                }
                
                // Add toggle functionality
                const toggleDetails = notificationSection.querySelector('.bhc-toggle-details');
                if (toggleDetails) {
                    toggleDetails.addEventListener('click', function() {
                        const details = notificationSection.querySelector('.bhc-detail-section');
                        if (details.style.display === 'block') {
                            details.style.display = 'none';
                            this.textContent = '[Show Details]';
                        } else {
                            details.style.display = 'block';
                            this.textContent = '[Hide Details]';
                        }
                    });
                }
            })
            .catch(error => {
                notificationSection.innerHTML = `
                    <div><span class="bhc-status-error"></span> <strong>Notification API Error</strong> <span class="bhc-toggle-details">[Show Details]</span></div>
                    <div class="bhc-detail-section">
                        <strong>Error:</strong> ${error.message}
                    </div>
                `;
                
                // Add toggle functionality
                const toggleDetails = notificationSection.querySelector('.bhc-toggle-details');
                if (toggleDetails) {
                    toggleDetails.addEventListener('click', function() {
                        const details = notificationSection.querySelector('.bhc-detail-section');
                        if (details.style.display === 'block') {
                            details.style.display = 'none';
                            this.textContent = '[Show Details]';
                        } else {
                            details.style.display = 'block';
                            this.textContent = '[Hide Details]';
                        }
                    });
                }
            });
    }
    
    // Check user management
    function checkUserManagement() {
        const results = document.getElementById('bhc-results');
        let userSection = document.createElement('div');
        userSection.className = 'bhc-collapsible';
        
        results.appendChild(userSection);
        userSection.innerHTML = `
            <div><span class="bhc-status-warning">⏳</span> <strong>Checking User Management...</strong></div>
        `;
        
        // Check if we're on the user management page
        const isUserManagementPage = window.location.pathname.includes('/Admin/UserManagement');
        
        if (isUserManagementPage) {
            // Check for key elements on the User Management page
            const userTableBody = document.getElementById('userTableBody');
            const filterDropdown = document.getElementById('filterDropdown');
            const searchButton = document.getElementById('searchButton');
            
            let issues = [];
            
            if (!userTableBody) {
                issues.push('User table body element (userTableBody) not found');
            } else {
                // Check if there are rows or empty state message
                const rows = userTableBody.querySelectorAll('tr');
                const emptyState = userTableBody.querySelector('.empty-state');
                
                if (rows.length === 0 && !emptyState) {
                    issues.push('User table has no rows and no empty state message');
                }
            }
            
            if (!filterDropdown) {
                issues.push('Filter dropdown element not found');
            }
            
            if (!searchButton) {
                issues.push('Search button element not found');
            }
            
            // Update result
            if (issues.length > 0) {
                userSection.innerHTML = `
                    <div><span class="bhc-status-error"></span> <strong>User Management Issues Detected</strong> <span class="bhc-toggle-details">[Show Details]</span></div>
                    <div class="bhc-detail-section">
                        <strong>${issues.length} issues found:</strong><br>
                        ${issues.map(e => `• ${e}`).join('<br>')}
                    </div>
                `;
            } else {
                userSection.innerHTML = `
                    <div><span class="bhc-status-ok"></span> <strong>User Management UI Elements OK</strong></div>
                `;
            }
        } else {
            userSection.innerHTML = `
                <div><span class="bhc-status-warning">ℹ️</span> <strong>Not on User Management Page</strong></div>
                <div>Navigate to Admin/UserManagement to check the User Management page</div>
            `;
        }
        
        // Add toggle functionality
        const toggleDetails = userSection.querySelector('.bhc-toggle-details');
        if (toggleDetails) {
            toggleDetails.addEventListener('click', function() {
                const details = userSection.querySelector('.bhc-detail-section');
                if (details.style.display === 'block') {
                    details.style.display = 'none';
                    this.textContent = '[Show Details]';
                } else {
                    details.style.display = 'block';
                    this.textContent = '[Hide Details]';
                }
            });
        }
    }
    
    // Test API endpoints
    function testApiEndpoints() {
        const results = document.getElementById('bhc-results');
        let apiSection = document.createElement('div');
        apiSection.className = 'bhc-collapsible';
        
        results.appendChild(apiSection);
        apiSection.innerHTML = `
            <div><span class="bhc-status-warning">⏳</span> <strong>Testing API Endpoints...</strong></div>
        `;
        
        // Endpoints to test
        const endpoints = [
            { url: '/api/Notification', method: 'GET', name: 'Get Notifications' },
            { url: '/api/Notification/debug', method: 'GET', name: 'Notification Debug' }
        ];
        
        // Test each endpoint
        Promise.all(endpoints.map(endpoint => {
            return fetch(endpoint.url, { method: endpoint.method })
                .then(response => ({
                    endpoint: endpoint,
                    status: response.status,
                    ok: response.ok,
                    statusText: response.statusText
                }))
                .catch(error => ({
                    endpoint: endpoint,
                    error: error.message
                }));
        }))
        .then(results => {
            let issues = [];
            
            // Check for issues
            results.forEach(result => {
                if (result.error || !result.ok) {
                    issues.push(`API '${result.endpoint.name}' (${result.endpoint.url}): ${result.error || `Status ${result.status} - ${result.statusText}`}`);
                }
            });
            
            // Update result
            if (issues.length > 0) {
                apiSection.innerHTML = `
                    <div><span class="bhc-status-error"></span> <strong>API Issues Detected</strong> <span class="bhc-toggle-details">[Show Details]</span></div>
                    <div class="bhc-detail-section">
                        <strong>${issues.length} issues found:</strong><br>
                        ${issues.map(e => `• ${e}`).join('<br>')}
                        <hr>
                        <strong>API Test Results:</strong><br>
                        ${results.map(r => `• ${r.endpoint.name}: ${r.error ? 'Failed - ' + r.error : r.ok ? 'OK' : 'Failed - Status ' + r.status}`).join('<br>')}
                    </div>
                `;
            } else {
                apiSection.innerHTML = `
                    <div><span class="bhc-status-ok"></span> <strong>All API Endpoints OK</strong> <span class="bhc-toggle-details">[Show Details]</span></div>
                    <div class="bhc-detail-section">
                        <strong>API Test Results:</strong><br>
                        ${results.map(r => `• ${r.endpoint.name}: OK (${r.status})`).join('<br>')}
                    </div>
                `;
            }
            
            // Add toggle functionality
            const toggleDetails = apiSection.querySelector('.bhc-toggle-details');
            if (toggleDetails) {
                toggleDetails.addEventListener('click', function() {
                    const details = apiSection.querySelector('.bhc-detail-section');
                    if (details.style.display === 'block') {
                        details.style.display = 'none';
                        this.textContent = '[Show Details]';
                    } else {
                        details.style.display = 'block';
                        this.textContent = '[Hide Details]';
                    }
                });
            }
        });
    }
    
    // Show console logs
    function showConsoleLogs() {
        const results = document.getElementById('bhc-results');
        results.innerHTML = '';
        
        let logSection = document.createElement('div');
        logSection.className = 'bhc-collapsible';
        results.appendChild(logSection);
        
        // Create a log capture
        let logs = [];
        const originalConsoleLog = console.log;
        
        // Add header and details
        logSection.innerHTML = `
            <div><strong>Console Logs</strong> <button id="bhc-start-capture" class="bhc-button">Start Capture</button> <button id="bhc-stop-capture" class="bhc-button" disabled>Stop Capture</button></div>
            <div class="bhc-detail-section" style="display: block; margin-top: 10px; max-height: 300px; font-family: monospace;">
                <div id="bhc-log-content">Click "Start Capture" to begin logging...</div>
            </div>
        `;
        
        // Handle start capture
        document.getElementById('bhc-start-capture').addEventListener('click', function() {
            logs = [];
            document.getElementById('bhc-log-content').innerHTML = 'Capturing logs...<br>';
            
            // Override console methods
            console.log = function() {
                logs.push({ type: 'log', message: Array.from(arguments).join(' ') });
                document.getElementById('bhc-log-content').innerHTML += `<span style="color:#333;">[log] ${Array.from(arguments).join(' ')}</span><br>`;
                originalConsoleLog.apply(console, arguments);
            };
            
            this.disabled = true;
            document.getElementById('bhc-stop-capture').disabled = false;
        });
        
        // Handle stop capture
        document.getElementById('bhc-stop-capture').addEventListener('click', function() {
            // Restore original console
            console.log = originalConsoleLog;
            
            this.disabled = true;
            document.getElementById('bhc-start-capture').disabled = false;
            
            // Add summary
            document.getElementById('bhc-log-content').innerHTML += `<hr><strong>Logging stopped. Captured ${logs.length} log entries.</strong>`;
        });
    }
    
    // Initialize when DOM is fully loaded
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', createTroubleshootingPanel);
    } else {
        createTroubleshootingPanel();
    }
})(); 