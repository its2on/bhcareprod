/**
 * Admin Dashboard Enhanced UI/UX JavaScript
 * Provides interactive features for the admin dashboard
 */

// Global charts collection for theme toggling
window.charts = window.charts || {};

// Initialize charts function (fallback for pages that don't need charts)
function initializeCharts() {
    console.log('📊 Charts initialization not needed for this page');
}

// Initialize interactions function (fallback for pages that don't need interactions)
function initializeInteractions() {
    console.log('🔗 Interactions initialization not needed for this page');
}

document.addEventListener('DOMContentLoaded', function() {
    // Initialize all interactive elements
    initializeTooltips();
    initializeNotifications();
    // Guard optional initializers to avoid hard failures on pages that don't include all libs
    try { initializeCharts(); } catch (e) { console.warn('initializeCharts failed:', e); }
    try { initializeInteractions(); } catch (e) { console.warn('initializeInteractions failed:', e); }
    initializeDropdowns();
    initializeStatCards();
    initializeResponsiveUI();
    initializeAccessibilitySupport();
    initializeNotificationSystem();
    initializeViewToggle();
    initializeExportActions();
    initializeQuickActions();
    initializeUserDropdown();
    initializeNavGroups();
    try { initializeDashboardCharts(); } catch (e) { console.warn('Charts init skipped:', e); }
    try { initializeChartActions(); } catch (e) { console.warn('Chart actions init skipped:', e); }
    handleLogoutButton();
    initializeSubmenus();
    initializeMobileMenu();
});

/**
 * Initialize tooltips
 */
function initializeTooltips() {
    const tooltipElements = document.querySelectorAll('[data-tooltip]');
    
    tooltipElements.forEach(element => {
        element.addEventListener('mouseenter', function() {
            const tooltipText = this.getAttribute('data-tooltip');
            
            const tooltip = document.createElement('div');
            tooltip.className = 'tooltip';
            tooltip.textContent = tooltipText;
            
            document.body.appendChild(tooltip);
            
            const elementRect = this.getBoundingClientRect();
            const tooltipRect = tooltip.getBoundingClientRect();
            
            tooltip.style.top = `${elementRect.top - tooltipRect.height - 8}px`;
            tooltip.style.left = `${elementRect.left + (elementRect.width - tooltipRect.width) / 2}px`;
            
            this.addEventListener('mouseleave', function() {
                tooltip.remove();
            }, { once: true });
        });
    });
}

/**
 * Initialize notification system
 */
function initializeNotifications() {
    const notificationButton = document.querySelector('.notification-button');
    if (!notificationButton) return;
    
    // Add a notification container if it doesn't exist
    let notificationContainer = document.querySelector('.message-container');
    if (!notificationContainer) {
        notificationContainer = document.createElement('div');
        notificationContainer.className = 'message-container';
        document.body.appendChild(notificationContainer);
    }
    
    notificationButton.addEventListener('click', function(e) {
        e.stopPropagation();
        const dropdown = this.closest('.notification-bell').querySelector('.notification-dropdown');
        dropdown.classList.toggle('show');
        
        // Mark as "seen" when opened (not as "read" yet)
        if (dropdown.classList.contains('show')) {
            const newNotifications = dropdown.querySelectorAll('.notification-item.new');
            newNotifications.forEach(notification => {
                notification.classList.remove('new');
            });
        }
    });
    
    // Mark as read functionality with visual confirmation
    const markReadButtons = document.querySelectorAll('.notification-action');
    markReadButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const notificationItem = this.closest('.notification-item');
            const notificationId = notificationItem.dataset.id || '';
            
            if (notificationItem.classList.contains('unread')) {
                // Show loading state
                this.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
                
                // Try to mark as read via API
                markNotificationAsRead(notificationId)
                    .then(() => {
                        notificationItem.classList.remove('unread');
                        this.innerHTML = '<i class="fas fa-check"></i>';
                        updateNotificationBadge();
                    })
                    .catch(err => {
                        console.error('Error marking notification as read:', err);
                        // Fallback - just update UI
                        notificationItem.classList.remove('unread');
                        this.innerHTML = '<i class="fas fa-check"></i>';
                        updateNotificationBadge();
                    });
            }
        });
    });
    
    // Mark all as read with loading state and confirmation
    const markAllButton = document.querySelector('.mark-all-read');
    if (markAllButton) {
        markAllButton.addEventListener('click', function() {
            const unreadItems = document.querySelectorAll('.notification-item.unread');
            if (unreadItems.length === 0) return;
            
            // Show loading state
            const originalText = this.textContent;
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
            
            // Try to mark all as read via API
            markAllNotificationsAsRead()
                .then(() => {
                    unreadItems.forEach(item => {
                        item.classList.remove('unread');
                        const actionBtn = item.querySelector('.notification-action');
                        if (actionBtn) {
                            actionBtn.innerHTML = '<i class="fas fa-check"></i>';
                        }
                    });
                    this.innerHTML = originalText;
                    updateNotificationBadge();
                    showMessage('All notifications marked as read', 'success');
                })
                .catch(err => {
                    console.error('Error marking all notifications as read:', err);
                    // Fallback - just update UI
                    unreadItems.forEach(item => {
                        item.classList.remove('unread');
                        const actionBtn = item.querySelector('.notification-action');
                        if (actionBtn) {
                            actionBtn.innerHTML = '<i class="fas fa-check"></i>';
                        }
                    });
                    this.innerHTML = originalText;
                    updateNotificationBadge();
                    showMessage('All notifications marked as read', 'success');
                });
        });
    }
    
    // Notification tabs
    const notificationTabs = document.querySelectorAll('.notification-tab');
    notificationTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const tabType = this.getAttribute('data-tab');
            
            // Update active tab
            document.querySelectorAll('.notification-tab').forEach(t => t.classList.remove('active'));
            this.classList.add('active');
            
            // Show/hide notifications based on tab
            document.querySelectorAll('.notification-item').forEach(item => {
                if (tabType === 'all') {
                    item.style.display = 'flex';
                } else if (tabType === 'unread') {
                    item.style.display = item.classList.contains('unread') ? 'flex' : 'none';
                } else if (tabType === 'system') {
                    // System notifications have the system class or a specific icon
                    const isSystem = item.classList.contains('system') || 
                                     item.querySelector('.notification-icon.bg-warning') !== null;
                    item.style.display = isSystem ? 'flex' : 'none';
                }
            });
            
            // Show empty state message if no notifications for this tab
            const visibleNotifications = document.querySelectorAll(`.notification-item[style="display: flex"]`);
            const emptyStateMsg = document.querySelector('.notification-empty-state') || 
                                  createEmptyStateMessage();
                                  
            if (visibleNotifications.length === 0) {
                emptyStateMsg.style.display = 'flex';
                emptyStateMsg.querySelector('p').textContent = `No ${tabType} notifications`;
            } else {
                emptyStateMsg.style.display = 'none';
            }
        });
    });
    
    // Make notifications clickable to navigate
    document.querySelectorAll('.notification-item').forEach(item => {
        item.addEventListener('click', function(e) {
            // Don't trigger if clicking the mark as read button
            if (e.target.closest('.notification-action')) return;
            
            const link = this.getAttribute('href');
            if (link && link !== '#') {
                window.location.href = link;
            }
        });
    });
    
    // Close notification dropdown when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.notification-bell')) {
            document.querySelectorAll('.notification-dropdown.show').forEach(dropdown => {
                dropdown.classList.remove('show');
            });
        }
    });
    
    // Create empty state message for no notifications
    function createEmptyStateMessage() {
        const emptyState = document.createElement('div');
        emptyState.className = 'notification-empty-state';
        emptyState.innerHTML = `
            <i class="fas fa-bell-slash"></i>
            <p>No notifications</p>
        `;
        
        const notificationList = document.querySelector('.notification-list');
        if (notificationList) {
            notificationList.appendChild(emptyState);
        }
        
        return emptyState;
    }
    
    // Initial badge count
    updateNotificationBadge();
    
    // Poll for new notifications every 60 seconds
    setInterval(updateNotificationBadge, 60000);
}

/**
 * Mark a specific notification as read via API
 * @param {string} notificationId - The ID of the notification to mark as read
 * @returns {Promise<void>}
 */
function markNotificationAsRead(notificationId) {
    return new Promise((resolve, reject) => {
        if (!notificationId) {
            reject(new Error('No notification ID provided'));
            return;
        }
        
        fetch(`/api/notifications/${notificationId}/read`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            credentials: 'same-origin'
        })
        .then(response => {
            if (!response.ok) {
                throw new Error(`API response not ok: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            resolve(data);
        })
        .catch(err => {
            console.warn('Failed to mark notification as read via API:', err);
            // For demo/fallback, we'll resolve anyway to update the UI
            resolve();
        });
    });
}

/**
 * Mark all notifications as read via API
 * @returns {Promise<void>}
 */
function markAllNotificationsAsRead() {
    return new Promise((resolve, reject) => {
        fetch('/api/notifications/mark-all-read', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            credentials: 'same-origin'
        })
        .then(response => {
            if (!response.ok) {
                throw new Error(`API response not ok: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            resolve(data);
        })
        .catch(err => {
            console.warn('Failed to mark all notifications as read via API:', err);
            // For demo/fallback, we'll resolve anyway to update the UI
            resolve();
        });
    });
}

/**
 * Update notification badge count based on API data
 */
function updateNotificationBadge() {
    // First try to get the count from the API
    fetchUnreadNotificationCount()
        .then(count => {
            updateBadgeDisplay(count);
        })
        .catch(err => {
            console.error('Error fetching notification count:', err);
            // Fallback to counting DOM elements if API fails
            const unreadCount = document.querySelectorAll('.notification-item.unread').length;
            updateBadgeDisplay(unreadCount);
        });
    
    /**
     * Update the badge UI with the correct count
     * @param {number} count - The number of unread notifications
     */
    function updateBadgeDisplay(count) {
        const badge = document.querySelector('.notification-badge');
        
        if (badge) {
            // Store the previous count to detect changes
            const prevCount = parseInt(badge.getAttribute('data-count') || '0');
            const newCount = parseInt(count) || 0;
            
            // Update badge text and visibility
            badge.textContent = newCount;
            badge.setAttribute('data-count', newCount);
            
            if (newCount === 0) {
                badge.style.display = 'none';
            } else {
                badge.style.display = 'flex';
                
                // Animate badge if count increased
                if (newCount > prevCount) {
                    badge.classList.remove('pulse');
                    // Trigger reflow to restart animation
                    void badge.offsetWidth;
                    badge.classList.add('pulse');
                    
                    // Show notification toast if there are new notifications
                    if (newCount > prevCount) {
                        showMessage(`You have ${newCount - prevCount} new notification${newCount - prevCount > 1 ? 's' : ''}`, 'info');
                    }
                }
            }
        }
    }
}

/**
 * Fetch unread notification count from the API
 * @returns {Promise<number>} The number of unread notifications
 */
function fetchUnreadNotificationCount() {
    return new Promise((resolve, reject) => {
        // Try to fetch from API endpoint with timeout for reliability
        const timeoutDuration = 5000; // 5 seconds timeout
        
        // Create an AbortController for timeout management
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), timeoutDuration);
        
        fetch('/api/notifications/unread-count', {
            signal: controller.signal,
            credentials: 'same-origin', // Include cookies for auth
            headers: {
                'Accept': 'application/json'
            }
        })
            .then(response => {
                clearTimeout(timeoutId);
                if (!response.ok) {
                    throw new Error(`API response not ok: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                resolve(data.count || 0);
            })
            .catch(err => {
                clearTimeout(timeoutId);
                console.warn('Failed to fetch notifications from API, trying fallback', err);
                
                // First fallback - check if we have window data with notifications
                if (window.notificationData && Array.isArray(window.notificationData.notifications)) {
                    const unreadCount = window.notificationData.notifications.filter(n => !n.isRead).length;
                    resolve(unreadCount);
                    return;
                }
                
                // Second fallback - check ViewData from Razor page
                const viewDataElement = document.getElementById('notification-data');
                if (viewDataElement) {
                    try {
                        const notificationData = JSON.parse(viewDataElement.textContent);
                        const unreadCount = notificationData.count || 0;
                        resolve(unreadCount);
                        return;
                    } catch (e) {
                        console.error('Error parsing notification data:', e);
                    }
                }
                
                // Final fallback - count DOM elements with .unread class
                const unreadCount = document.querySelectorAll('.notification-item.unread').length;
                resolve(unreadCount);
            });
    });
}

/**
 * Initialize the dropdown menus in the action column
 */
function initializeDropdowns() {
    const dropdownButtons = document.querySelectorAll('.action-btn.more-actions-btn');
    
    dropdownButtons.forEach(button => {
        const dropdownMenu = button.nextElementSibling;
        
        // Show dropdown on click
        button.addEventListener('click', function(e) {
            e.stopPropagation();
            
            // Close all other open dropdowns first
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                if (menu !== dropdownMenu) {
                    menu.classList.remove('show');
                }
            });
            
            // Toggle current dropdown
            dropdownMenu.classList.toggle('show');
            
            // Position the dropdown
            const rect = button.getBoundingClientRect();
            const isRightAligned = rect.left + dropdownMenu.offsetWidth > window.innerWidth;
            
            if (isRightAligned) {
                dropdownMenu.style.right = '0';
                dropdownMenu.style.left = 'auto';
            } else {
                dropdownMenu.style.left = '0';
                dropdownMenu.style.right = 'auto';
            }
        });
    });
    
    // Close dropdowns when clicking outside
    document.addEventListener('click', function() {
        document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
            menu.classList.remove('show');
        });
    });
}

/**
 * Initialize search filter functionality
 */
function initializeSearchFilter() {
    const searchInput = document.getElementById('staff-search');
    const searchClear = document.querySelector('.search-clear');
    const tableRows = document.querySelectorAll('.staff-table tbody tr');
    
    if (!searchInput) return;
    
    searchInput.addEventListener('input', function() {
        const searchTerm = this.value.toLowerCase().trim();
        
        // Filter table rows
        tableRows.forEach(row => {
            const text = row.textContent.toLowerCase();
            if (text.includes(searchTerm)) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
        
        // Update filtered count
        updateFilteredCount();
    });
    
    // Clear search
    if (searchClear) {
        searchClear.addEventListener('click', function() {
            searchInput.value = '';
            searchInput.dispatchEvent(new Event('input'));
            searchInput.focus();
        });
    }
}

/**
 * Initialize filter controls
 */
function initializeFilterControls() {
    const roleFilter = document.getElementById('role-filter');
    const statusFilter = document.getElementById('status-filter');
    const resetFiltersBtn = document.getElementById('reset-filters');
    
    if (roleFilter) {
        roleFilter.addEventListener('change', applyFilters);
    }
    
    if (statusFilter) {
        statusFilter.addEventListener('change', applyFilters);
    }
    
    if (resetFiltersBtn) {
        resetFiltersBtn.addEventListener('click', resetFilters);
    }
    
    /**
     * Apply all active filters to the table
     */
    function applyFilters() {
        const roleValue = roleFilter ? roleFilter.value : 'all';
        const statusValue = statusFilter ? statusFilter.value : 'all';
        const rows = document.querySelectorAll('.staff-table tbody tr');
        
        rows.forEach(row => {
            const rowRole = row.dataset.role;
            const rowStatus = row.dataset.status;
            
            const roleMatch = roleValue === 'all' || rowRole === roleValue;
            const statusMatch = statusValue === 'all' || rowStatus === statusValue;
            
            if (roleMatch && statusMatch) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });
        
        // Update filtered count
        updateFilteredCount();
    }
    
    /**
     * Reset all filters to their default values
     */
    function resetFilters() {
        if (roleFilter) roleFilter.value = 'all';
        if (statusFilter) statusFilter.value = 'all';
        
        // Reset table
        const rows = document.querySelectorAll('.staff-table tbody tr');
        rows.forEach(row => {
            row.style.display = '';
        });
        
        // Clear search
        const searchInput = document.getElementById('staff-search');
        if (searchInput) {
            searchInput.value = '';
        }
        
        // Update filtered count
        updateFilteredCount();
    }
}

/**
 * Initialize bulk actions functionality
 */
function initializeBulkActions() {
    const selectAllCheckbox = document.getElementById('select-all');
    const rowCheckboxes = document.querySelectorAll('.row-checkbox');
    const bulkActionSelect = document.getElementById('bulk-action');
    const applyBulkActionBtn = document.getElementById('apply-bulk-action');
    const selectionInfo = document.querySelector('.selection-info');
    
    if (!selectAllCheckbox || !applyBulkActionBtn) return;
    
    // Toggle all checkboxes
    selectAllCheckbox.addEventListener('change', function() {
        const isChecked = this.checked;
        
        rowCheckboxes.forEach(checkbox => {
            checkbox.checked = isChecked;
        });
        
        updateBulkActionState();
    });
    
    // Individual checkbox change
    rowCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', updateBulkActionState);
    });
    
    // Bulk action selection
    bulkActionSelect.addEventListener('change', updateBulkActionState);
    
    // Apply bulk action
    applyBulkActionBtn.addEventListener('click', function() {
        const selectedAction = bulkActionSelect.value;
        const selectedRows = Array.from(document.querySelectorAll('.row-checkbox:checked'));
        
        if (!selectedAction || selectedRows.length === 0) {
            return;
        }
        
        const selectedIds = selectedRows.map(checkbox => checkbox.value);
        
        switch (selectedAction) {
            case 'activate':
                if (confirm(`Are you sure you want to activate ${selectedRows.length} staff member(s)?`)) {
                    // Here you would handle the activation, either via AJAX or form submission
                    console.log('Activating staff IDs:', selectedIds);
                }
                break;
            case 'deactivate':
                if (confirm(`Are you sure you want to deactivate ${selectedRows.length} staff member(s)?`)) {
                    // Here you would handle the deactivation, either via AJAX or form submission
                    console.log('Deactivating staff IDs:', selectedIds);
                }
                break;
            case 'export':
                // Export selected staff data
                console.log('Exporting staff IDs:', selectedIds);
                break;
            case 'delete':
                if (confirm(`Are you sure you want to delete ${selectedRows.length} staff member(s)? This action cannot be undone.`)) {
                    // Here you would handle the deletion, either via AJAX or form submission
                    console.log('Deleting staff IDs:', selectedIds);
                }
                break;
        }
    });
    
    /**
     * Update bulk action button state and selection info
     */
    function updateBulkActionState() {
        const selectedCheckboxes = document.querySelectorAll('.row-checkbox:checked');
        const selectedAction = bulkActionSelect.value;
        const count = selectedCheckboxes.length;
        
        // Check if all rows are selected
        const allSelected = count === rowCheckboxes.length;
        if (selectAllCheckbox) {
            selectAllCheckbox.checked = allSelected && count > 0;
            selectAllCheckbox.indeterminate = count > 0 && !allSelected;
        }
        
        // Update apply button state
        if (applyBulkActionBtn) {
            applyBulkActionBtn.disabled = count === 0 || !selectedAction;
        }
        
        // Update selection info
        if (selectionInfo) {
            selectionInfo.textContent = `${count} staff selected`;
            selectionInfo.style.display = count > 0 ? 'block' : 'none';
        }
    }
}

/**
 * Initialize stat cards with animations
 */
function initializeStatCards() {
    const statValues = document.querySelectorAll('.stat-value');
    
    statValues.forEach(stat => {
        const targetValue = parseInt(stat.textContent);
        animateNumber(stat, 0, targetValue, 1500);
    });
    
    /**
     * Animate number from start to end value
     * @param {Element} element - Element to update
     * @param {number} start - Starting value
     * @param {number} end - Ending value
     * @param {number} duration - Duration in milliseconds
     */
    function animateNumber(element, start, end, duration) {
        let startTimestamp = null;
        const step = (timestamp) => {
            if (!startTimestamp) startTimestamp = timestamp;
            const progress = Math.min((timestamp - startTimestamp) / duration, 1);
            const currentValue = Math.floor(progress * (end - start) + start);
            element.textContent = currentValue;
            
            if (progress < 1) {
                window.requestAnimationFrame(step);
            }
        };
        window.requestAnimationFrame(step);
    }
}

/**
 * Initialize responsive UI adjustments
 */
function initializeResponsiveUI() {
    const toggleSidebarBtn = document.getElementById('toggle-sidebar');
    const sidebar = document.querySelector('.sidebar');
    const mainContent = document.querySelector('.main-content');
    
    // Handle sidebar toggle
    if (toggleSidebarBtn && sidebar) {
        toggleSidebarBtn.addEventListener('click', function() {
            sidebar.classList.toggle('collapsed');
            mainContent.classList.toggle('expanded');
        });
    }
    
    // Handle responsive adjustments on window resize
    window.addEventListener('resize', function() {
        const width = window.innerWidth;
        
        // Automatically collapse sidebar on small screens
        if (width < 992 && sidebar) {
            sidebar.classList.add('collapsed');
            mainContent.classList.add('expanded');
        }
    });
    
    // Initial check for screen size
    if (window.innerWidth < 992 && sidebar) {
        sidebar.classList.add('collapsed');
        mainContent.classList.add('expanded');
    }
}

/**
 * Initialize accessibility support
 */
function initializeAccessibilitySupport() {
    // Add proper ARIA attributes to interactive elements
    
    // Make action buttons accessible by keyboard
    const actionButtons = document.querySelectorAll('.action-button');
    actionButtons.forEach(button => {
        button.setAttribute('aria-haspopup', 'true');
        button.setAttribute('aria-expanded', 'false');
        
        // Update ARIA state when dropdown opens/closes
        button.addEventListener('click', function() {
            const isExpanded = this.nextElementSibling.classList.contains('show');
            this.setAttribute('aria-expanded', isExpanded.toString());
        });
    });
    
    // Ensure all form controls have associated labels
    const formControls = document.querySelectorAll('select, input');
    formControls.forEach(control => {
        if (!control.id) {
            console.warn('Form control without ID:', control);
            return;
        }
        
        const label = document.querySelector(`label[for="${control.id}"]`);
        if (!label) {
            console.warn(`No label found for form control #${control.id}`);
        }
    });
    
    // Handle keyboard navigation in dropdowns
    document.querySelectorAll('.action-dropdown').forEach(dropdown => {
        const actionItems = dropdown.querySelectorAll('.action-item');
        
        actionItems.forEach((item, index) => {
            // Handle arrow keys
            item.addEventListener('keydown', function(e) {
                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    if (index < actionItems.length - 1) {
                        actionItems[index + 1].focus();
                    }
                } else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    if (index > 0) {
                        actionItems[index - 1].focus();
                    } else {
                        dropdown.previousElementSibling.focus();
                    }
                } else if (e.key === 'Escape') {
                    e.preventDefault();
                    dropdown.classList.remove('show');
                    dropdown.previousElementSibling.focus();
                }
            });
        });
    });
}

/**
 * Initialize the notification system
 */
function initializeNotificationSystem() {
    const notificationButton = document.querySelector('.notification-button');
    const notificationDropdown = document.querySelector('.notification-dropdown');
    const markAllReadButton = document.querySelector('.mark-all-read');
    const notificationTabs = document.querySelectorAll('.notification-tab');
    const notificationItems = document.querySelectorAll('.notification-item');
    const notificationActions = document.querySelectorAll('.notification-action');
    const alertCloseButtons = document.querySelectorAll('.alert-close');
    
    // Toggle notification dropdown
    if (notificationButton && notificationDropdown) {
        notificationButton.addEventListener('click', function(e) {
            e.stopPropagation();
            notificationDropdown.classList.toggle('show');
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!notificationDropdown.contains(e.target) && !notificationButton.contains(e.target)) {
                notificationDropdown.classList.remove('show');
            }
        });
    }
    
    // Mark all notifications as read
    if (markAllReadButton) {
        markAllReadButton.addEventListener('click', function() {
            document.querySelectorAll('.notification-item.unread').forEach(item => {
                markNotificationRead(item);
            });
            
            // Update badge count
            updateNotificationBadge();
        });
    }
    
    // Mark individual notification as read
    if (notificationActions) {
        notificationActions.forEach(action => {
            action.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                
                const notificationItem = this.closest('.notification-item');
                if (notificationItem && notificationItem.classList.contains('unread')) {
                    markNotificationRead(notificationItem);
                    
                    // Update badge count
                    updateNotificationBadge();
                }
            });
        });
    }
    
    // Filter notifications by type
    if (notificationTabs) {
        notificationTabs.forEach(tab => {
            tab.addEventListener('click', function() {
                // Update active tab
                notificationTabs.forEach(t => t.classList.remove('active'));
                this.classList.add('active');
                
                const filterType = this.dataset.tab;
                
                // Filter notification items
                notificationItems.forEach(item => {
                    if (filterType === 'all') {
                        item.style.display = '';
                    } else if (filterType === 'unread') {
                        item.style.display = item.classList.contains('unread') ? '' : 'none';
                    } else if (filterType === 'system') {
                        // Assuming system notifications have a specific class or data attribute
                        // Modify this condition to match your system notification identifier
                        const isSystemNotification = item.querySelector('.notification-icon.bg-warning') !== null;
                        item.style.display = isSystemNotification ? '' : 'none';
                    }
                });
            });
        });
    }
    
    // Close alert messages
    alertCloseButtons.forEach(button => {
        button.addEventListener('click', function() {
            const alert = this.closest('.alert');
            if (alert) {
                alert.style.opacity = '0';
                setTimeout(() => {
                    alert.style.display = 'none';
                }, 300);
            }
        });
    });
    
    /**
     * Mark a notification as read
     * @param {Element} notificationItem - The notification item element
     */
    function markNotificationRead(notificationItem) {
        notificationItem.classList.remove('unread');
        
        // Change the read indicator
        const indicator = notificationItem.querySelector('.notification-action i');
        if (indicator) {
            indicator.className = 'fas fa-check';
        }
        
        // In a real implementation, you would send an AJAX request to mark as read in the backend
        // Example:
        /*
        const notificationId = notificationItem.dataset.id;
        fetch('/api/notifications/markAsRead', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ id: notificationId })
        });
        */
    }
}

/**
 * Initialize the view toggle functionality (table/card view)
 */
function initializeViewToggle() {
    const viewToggleButtons = document.querySelectorAll('.view-toggle-btn');
    const tableContainer = document.querySelector('.table-responsive');
    const cardViewContainer = document.createElement('div');
    cardViewContainer.className = 'card-view-container';
    cardViewContainer.style.display = 'none';
    
    // Insert the card view container after the table
    if (tableContainer) {
        tableContainer.parentNode.insertBefore(cardViewContainer, tableContainer.nextSibling);
        
        // Generate card view based on table data
        generateCardView();
    }
    
    // Toggle between views when buttons are clicked
    viewToggleButtons.forEach(button => {
        button.addEventListener('click', function() {
            // Update active state of buttons
            viewToggleButtons.forEach(btn => btn.classList.remove('active'));
            this.classList.add('active');
            
            const viewType = this.dataset.view;
            
            if (viewType === 'table') {
                if (tableContainer) tableContainer.style.display = 'block';
                cardViewContainer.style.display = 'none';
            } else if (viewType === 'cards') {
                if (tableContainer) tableContainer.style.display = 'none';
                cardViewContainer.style.display = 'grid';
            }
        });
    });
    
    /**
     * Generate card view from table data
     */
    function generateCardView() {
        const tableRows = document.querySelectorAll('.staff-table tbody tr');
        
        tableRows.forEach(row => {
            const userId = row.querySelector('.row-checkbox').value;
            const name = row.querySelector('[data-column="name"] .fw-medium').textContent;
            const avatar = row.querySelector('.avatar-circle').textContent;
            const email = row.querySelector('[data-column="email"]').textContent;
            const role = row.querySelector('[data-column="role"] .role-badge').textContent;
            const roleBadgeClass = row.querySelector('[data-column="role"] .role-badge').className;
            const specialization = row.querySelector('[data-column="specialization"]').textContent;
            const workingHours = row.querySelector('[data-column="workingHours"]').textContent;
            const status = row.querySelector('[data-column="status"] .status-text').textContent;
            const statusClass = row.querySelector('[data-column="status"] .status-indicator').className;
            
            // Create card element
            const card = document.createElement('div');
            card.className = 'staff-card';
            card.dataset.role = row.dataset.role;
            card.dataset.status = row.dataset.status;
            
            card.innerHTML = `
                <div class="card-header">
                    <div class="staff-avatar">
                        ${avatar}
                    </div>
                    <div class="card-actions">
                        <div class="form-check">
                            <input class="form-check-input card-checkbox" type="checkbox" value="${userId}" aria-label="Select ${name}">
                        </div>
                        <div class="action-menu">
                            <button class="action-button" aria-label="Actions for ${name}">
                                <i class="fas fa-ellipsis-v"></i>
                            </button>
                            <div class="action-dropdown">
                                <a href="/Admin/EditStaff/${userId}" class="action-item edit">
                                    <i class="fas fa-edit"></i>
                                    <span>Edit</span>
                                </a>
                                <a href="/Admin/ManagePermissions?userId=${userId}" class="action-item permissions">
                                    <i class="fas fa-key"></i>
                                    <span>Permissions</span>
                                </a>
                                ${status.trim() === 'Active' ? 
                                `<a href="#" class="action-item deactivate" data-staff-name="${name}" 
                                   onclick="event.preventDefault(); document.getElementById('deactivate-${userId}').submit();">
                                    <i class="fas fa-user-times"></i>
                                    <span>Deactivate</span>
                                </a>` : 
                                `<a href="#" class="action-item activate" data-staff-name="${name}"
                                   onclick="event.preventDefault(); document.getElementById('activate-${userId}').submit();">
                                    <i class="fas fa-user-check"></i>
                                    <span>Activate</span>
                                </a>`}
                            </div>
                        </div>
                    </div>
                </div>
                <div class="card-body">
                    <h3 class="staff-name">${name}</h3>
                    <p class="staff-email">${email}</p>
                    <div class="staff-details">
                        <div class="detail-item">
                            <span class="detail-label">Role:</span>
                            <span class="${roleBadgeClass}">${role}</span>
                        </div>
                        <div class="detail-item">
                            <span class="detail-label">Status:</span>
                            <div class="${statusClass}">
                                <span class="status-dot"></span>
                                <span class="status-text">${status}</span>
                            </div>
                        </div>
                        <div class="detail-item">
                            <span class="detail-label">Specialization:</span>
                            <span>${specialization}</span>
                        </div>
                        <div class="detail-item">
                            <span class="detail-label">Working Hours:</span>
                            <span>${workingHours}</span>
                        </div>
                    </div>
                </div>
            `;
            
            cardViewContainer.appendChild(card);
        });
        
        // Synchronize checkboxes between views
        synchronizeCheckboxes();
    }
    
    /**
     * Synchronize checkboxes between table and card views
     */
    function synchronizeCheckboxes() {
        const tableCheckboxes = document.querySelectorAll('.row-checkbox');
        const cardCheckboxes = document.querySelectorAll('.card-checkbox');
        
        // Sync from table to cards
        tableCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', function() {
                const value = this.value;
                const isChecked = this.checked;
                
                cardCheckboxes.forEach(cardCheckbox => {
                    if (cardCheckbox.value === value) {
                        cardCheckbox.checked = isChecked;
                    }
                });
            });
        });
        
        // Sync from cards to table
        cardCheckboxes.forEach(checkbox => {
            checkbox.addEventListener('change', function() {
                const value = this.value;
                const isChecked = this.checked;
                
                tableCheckboxes.forEach(tableCheckbox => {
                    if (tableCheckbox.value === value) {
                        tableCheckbox.checked = isChecked;
                        tableCheckbox.dispatchEvent(new Event('change'));
                    }
                });
            });
        });
    }
}

/**
 * Initialize export actions for the table data (CSV, PDF, Print)
 */
function initializeExportActions() {
    const exportCSVBtn = document.querySelector('.export-csv');
    const exportPDFBtn = document.querySelector('.export-pdf');
    const printTableBtn = document.querySelector('.print-table');
    const refreshDataBtn = document.getElementById('refresh-data');
    const exportOptionsBtn = document.getElementById('export-options');
    
    // Toggle export options dropdown
    if (exportOptionsBtn) {
        exportOptionsBtn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const dropdown = this.nextElementSibling;
            dropdown.style.display = dropdown.style.display === 'block' ? 'none' : 'block';
            
            // Close dropdown when clicking outside
            document.addEventListener('click', function closeDropdown(event) {
                if (!dropdown.contains(event.target) && event.target !== exportOptionsBtn) {
                    dropdown.style.display = 'none';
                    document.removeEventListener('click', closeDropdown);
                }
            });
        });
    }
    
    // Export to CSV
    if (exportCSVBtn) {
        exportCSVBtn.addEventListener('click', function(e) {
            e.preventDefault();
            exportTableToCSV('staff_list.csv');
        });
    }
    
    // Export to PDF
    if (exportPDFBtn) {
        exportPDFBtn.addEventListener('click', function(e) {
            e.preventDefault();
            exportTableToPDF();
        });
    }
    
    // Print table
    if (printTableBtn) {
        printTableBtn.addEventListener('click', function(e) {
            e.preventDefault();
            printTable();
        });
    }
    
    // Refresh data
    if (refreshDataBtn) {
        refreshDataBtn.addEventListener('click', function(e) {
            e.preventDefault();
            // Show loading animation
            this.classList.add('rotating');
            
            // In a real implementation, you would fetch fresh data from the server
            // For demo purposes, we'll just simulate a data refresh with a timeout
            setTimeout(() => {
                this.classList.remove('rotating');
                
                // Show success message
                showMessage('Data refreshed successfully', 'success');
            }, 1000);
        });
    }
    
    /**
     * Export the table data to CSV format
     * @param {string} filename - The filename for the downloaded file
     */
    function exportTableToCSV(filename) {
        const table = document.querySelector('.staff-table');
        if (!table) return;
        
        const rows = Array.from(table.querySelectorAll('tr'));
        
        // Prepare CSV data
        const csvData = [];
        
        rows.forEach(row => {
            const rowData = [];
            const cells = Array.from(row.querySelectorAll('th, td'));
            
            cells.forEach(cell => {
                // Skip checkbox cells and action cells
                if (cell.querySelector('.form-check') || cell.querySelector('.action-menu')) {
                    return;
                }
                
                // Get text content, clean it up and quote it
                let text = cell.textContent.trim().replace(/\s+/g, ' ');
                rowData.push(`"${text.replace(/"/g, '""')}"`);
            });
            
            csvData.push(rowData.join(','));
        });
        
        // Create a hidden link element to trigger download
        const csvContent = 'data:text/csv;charset=utf-8,' + encodeURIComponent(csvData.join('\n'));
        const link = document.createElement('a');
        link.setAttribute('href', csvContent);
        link.setAttribute('download', filename);
        link.style.display = 'none';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        // Show success message
        showMessage('Staff list exported successfully as CSV', 'success');
    }
    
    /**
     * Export the table data to PDF format
     * Uses a simplified approach for this example
     */
    function exportTableToPDF() {
        // In a real implementation, you would use a PDF library like jsPDF
        // For this example, we'll just simulate PDF export
        
        showMessage('Generating PDF...', 'info');
        
        // Simulate PDF generation with a timeout
        setTimeout(() => {
            showMessage('Staff list exported successfully as PDF', 'success');
        }, 1000);
        
        // Actual implementation would look something like this:
        /*
        const table = document.querySelector('.staff-table');
        const doc = new jsPDF();
        
        // Add title
        doc.text('Staff List', 14, 16);
        
        // Convert table to PDF
        doc.autoTable({ html: table });
        
        // Save PDF
        doc.save('staff_list.pdf');
        */
    }
    
    /**
     * Print the table data
     */
    function printTable() {
        // Create a new window with only the table
        const table = document.querySelector('.staff-table');
        const printWindow = window.open('', '_blank');
        
        // Setup the print document
        printWindow.document.write(`
            <html>
                <head>
                    <title>Staff List - Print</title>
                    <style>
                        body { font-family: Arial, sans-serif; }
                        table { width: 100%; border-collapse: collapse; }
                        th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }
                        th { background-color: #f2f2f2; }
                        .header { margin-bottom: 20px; }
                        .header h1 { margin-bottom: 5px; }
                        .header p { color: #666; }
                        @media print {
                            .no-print { display: none; }
                            button { display: none; }
                        }
                    </style>
                </head>
                <body>
                    <div class="header">
                        <h1>Barangay Health Center - Staff List</h1>
                        <p>Printed on ${new Date().toLocaleDateString()} at ${new Date().toLocaleTimeString()}</p>
                    </div>
                    <div>
                        <button class="no-print" onclick="window.print()">Print</button>
                        <button class="no-print" onclick="window.close()">Close</button>
                    </div>
                    <table id="printTable">
                        ${table.innerHTML}
                    </table>
                    <script>
                        // Remove action column and checkboxes from print
                        document.querySelectorAll('th:first-child, td:first-child, th:last-child, td:last-child').forEach(el => el.style.display = 'none');
                    </script>
                </body>
            </html>
        `);
        
        printWindow.document.close();
    }
    
    /**
     * Show a message to the user
     * @param {string} message - The message to display
     * @param {string} type - The type of message (success, error, info)
     */
    function showMessage(message, type) {
        // Check if a message container already exists
        let messageContainer = document.querySelector('.message-container');
        
        // Create one if it doesn't exist
        if (!messageContainer) {
            messageContainer = document.createElement('div');
            messageContainer.className = 'message-container';
            document.body.appendChild(messageContainer);
        }
        
        // Create the message element
        const messageElement = document.createElement('div');
        messageElement.className = `message message-${type}`;
        messageElement.innerHTML = `
            <i class="fas ${type === 'success' ? 'fa-check-circle' : type === 'error' ? 'fa-exclamation-circle' : 'fa-info-circle'}"></i>
            <span>${message}</span>
        `;
        
        // Add to container
        messageContainer.appendChild(messageElement);
        
        // Remove after a delay
        setTimeout(() => {
            messageElement.classList.add('fade-out');
            setTimeout(() => {
                messageContainer.removeChild(messageElement);
            }, 300);
        }, 3000);
    }
}

/**
 * Initialize quick actions dropdown
 */
function initializeQuickActions() {
    const quickActionsButton = document.querySelector('.quick-actions-button');
    const quickActionsDropdown = document.querySelector('.quick-actions-dropdown');
    const batchActivateBtn = document.querySelector('.batch-activate');
    const exportStaffDataBtn = document.querySelector('.export-staff-data');
    
    // Toggle quick actions dropdown
    if (quickActionsButton && quickActionsDropdown) {
        quickActionsButton.addEventListener('click', function(e) {
            e.stopPropagation();
            
            // Close all other dropdowns
            document.querySelectorAll('.notification-dropdown.show, .user-dropdown.show').forEach(dropdown => {
                dropdown.classList.remove('show');
            });
            
            // Toggle current dropdown
            quickActionsDropdown.classList.toggle('show');
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!quickActionsDropdown.contains(e.target) && !quickActionsButton.contains(e.target)) {
                quickActionsDropdown.classList.remove('show');
            }
        });
    }
    
    // Handle batch activate action
    if (batchActivateBtn) {
        batchActivateBtn.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Close dropdown
            quickActionsDropdown.classList.remove('show');
            
            // Select the bulk action dropdown and set it to "activate"
            const bulkActionSelect = document.getElementById('bulk-action');
            if (bulkActionSelect) {
                bulkActionSelect.value = 'activate';
                
                // Scroll to the bulk actions section
                const tableToolbar = document.querySelector('.table-toolbar');
                if (tableToolbar) {
                    tableToolbar.scrollIntoView({ behavior: 'smooth' });
                    
                    // Highlight the section briefly
                    tableToolbar.classList.add('highlight-section');
                    setTimeout(() => {
                        tableToolbar.classList.remove('highlight-section');
                    }, 2000);
                }
                
                showMessage('Please select the staff members to activate', 'info');
            }
        });
    }
    
    // Handle export staff data action
    if (exportStaffDataBtn) {
        exportStaffDataBtn.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Close dropdown
            quickActionsDropdown.classList.remove('show');
            
            // Trigger the export CSV function
            const exportCSVBtn = document.querySelector('.export-csv');
            if (exportCSVBtn) {
                exportCSVBtn.click();
            } else {
                // Fallback if export button not found
                showMessage('Exporting staff data...', 'info');
                
                setTimeout(() => {
                    showMessage('Staff data exported successfully', 'success');
                }, 1000);
            }
        });
    }
}

/**
 * Initialize user dropdown functionality
 */
function initializeUserDropdown() {
    const userAvatar = document.querySelector('.user-avatar');
    const userDropdown = document.querySelector('.user-dropdown');
    
    // Toggle user dropdown
    if (userAvatar && userDropdown) {
        userAvatar.addEventListener('click', function(e) {
            e.stopPropagation();
            
            // Close all other dropdowns
            document.querySelectorAll('.notification-dropdown.show, .quick-actions-dropdown.show').forEach(dropdown => {
                dropdown.classList.remove('show');
            });
            
            // Toggle current dropdown
            userDropdown.classList.toggle('show');
        });
        
        // Close dropdown when clicking outside
        document.addEventListener('click', function(e) {
            if (!userDropdown.contains(e.target) && !userAvatar.contains(e.target)) {
                userDropdown.classList.remove('show');
            }
        });
    }
}

/**
 * Initialize collapsible navigation groups in the sidebar
 */
function initializeNavGroups() {
    const navGroups = document.querySelectorAll('.nav-group');
    const collapseSidebarBtn = document.getElementById('collapse-sidebar');
    
    // Add collapse/expand functionality to navigation groups
    navGroups.forEach(group => {
        const label = group.querySelector('.nav-group-label');
        const navList = group.querySelector('.nav-list');
        
        // Add expand/collapse indicator
        if (label) {
            // Add toggle indicator
            const indicator = document.createElement('i');
            indicator.className = 'fas fa-chevron-down nav-group-toggle';
            label.appendChild(indicator);
            
            // Make the label clickable
            label.style.cursor = 'pointer';
            
            // Toggle nav list visibility when label is clicked
            label.addEventListener('click', function() {
                // Toggle the nav list
                navList.classList.toggle('collapsed');
                
                // Update the indicator
                indicator.className = navList.classList.contains('collapsed') 
                    ? 'fas fa-chevron-right nav-group-toggle' 
                    : 'fas fa-chevron-down nav-group-toggle';
            });
        }
    });
    
    // Handle manual sidebar collapsing (only if not already handled by layout)
    if (collapseSidebarBtn && !collapseSidebarBtn.hasAttribute('data-handler-attached')) {
        collapseSidebarBtn.setAttribute('data-handler-attached', 'true');
        collapseSidebarBtn.addEventListener('click', function() {
            const sidebar = document.querySelector('.sidebar');
            const mainContent = document.querySelector('.main-content');
            
            if (sidebar && mainContent) {
                sidebar.classList.toggle('collapsed');
                mainContent.classList.toggle('expanded');
                
                // Update the collapse button icon
                const icon = this.querySelector('i');
                if (icon) {
                    icon.className = sidebar.classList.contains('collapsed')
                        ? 'fas fa-chevron-right'
                        : 'fas fa-chevron-left';
                }
                
                // Save state to localStorage
                localStorage.setItem('adminSidebarCollapsed', sidebar.classList.contains('collapsed').toString());
            }
        });
    }
}

/**
 * Initialize dashboard charts and analytics
 */
function initializeDashboardCharts() {
    // Skip entirely if Chart.js is not present
    if (typeof window.Chart === 'undefined') {
        console.warn('Chart.js not found on page; skipping chart initialization');
        return;
    }
    // Check if the chart container exists
    const staffDistributionChart = document.getElementById('staffDistributionChart');
    const activityTrendChart = document.getElementById('activityTrendChart');
    
    if (!staffDistributionChart || !activityTrendChart) return;
    
    // Initialize staff distribution chart
    window.charts.staffDistributionChart = new Chart(staffDistributionChart, {
        type: 'doughnut',
        data: {
            labels: ['Doctors', 'Nurses', 'Admin', 'Other Staff'],
            datasets: [{
                data: [
                    parseInt(staffDistributionChart.dataset.doctors || 0),
                    parseInt(staffDistributionChart.dataset.nurses || 0),
                    parseInt(staffDistributionChart.dataset.admins || 0),
                    parseInt(staffDistributionChart.dataset.others || 0)
                ],
                backgroundColor: [
                    'rgba(66, 133, 244, 0.8)',  // Doctor (blue)
                    'rgba(52, 168, 83, 0.8)',   // Nurse (green)
                    'rgba(234, 67, 53, 0.8)',   // Admin (red)
                    'rgba(251, 188, 5, 0.8)'    // Other (yellow)
                ],
                borderColor: [
                    'rgba(66, 133, 244, 1)',
                    'rgba(52, 168, 83, 1)',
                    'rgba(234, 67, 53, 1)',
                    'rgba(251, 188, 5, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'right',
                    labels: {
                        color: '#333',
                        padding: 15,
                        usePointStyle: true,
                        pointStyle: 'circle'
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(255, 255, 255, 0.9)',
                    titleColor: '#333',
                    bodyColor: '#333',
                    borderColor: '#ddd',
                    borderWidth: 1,
                    padding: 12,
                    displayColors: true,
                    callbacks: {
                        label: function(context) {
                            const total = context.dataset.data.reduce((acc, val) => acc + val, 0);
                            const value = context.raw;
                            const percentage = Math.round((value / total) * 100);
                            return `${context.label}: ${value} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });
    
    // Initialize activity trend chart
    window.charts.activityTrendChart = new Chart(activityTrendChart, {
        type: 'line',
        data: {
            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
            datasets: [{
                label: 'Staff Activity',
                data: JSON.parse(activityTrendChart.dataset.activity || '[0,0,0,0,0,0,0,0,0,0,0,0]'),
                borderColor: 'rgba(25, 118, 210, 1)',
                backgroundColor: 'rgba(25, 118, 210, 0.1)',
                borderWidth: 2,
                fill: true,
                tension: 0.4
            }, {
                label: 'New Staff',
                data: JSON.parse(activityTrendChart.dataset.newStaff || '[0,0,0,0,0,0,0,0,0,0,0,0]'),
                borderColor: 'rgba(46, 125, 50, 1)',
                backgroundColor: 'rgba(46, 125, 50, 0.1)',
                borderWidth: 2,
                fill: true,
                tension: 0.4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                    labels: {
                        color: '#333'
                    }
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    backgroundColor: 'rgba(255, 255, 255, 0.9)',
                    titleColor: '#333',
                    bodyColor: '#333',
                    borderColor: '#ddd',
                    borderWidth: 1
                }
            },
            scales: {
                x: {
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)'
                    },
                    ticks: {
                        color: '#666'
                    }
                },
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)'
                    },
                    ticks: {
                        color: '#666'
                    }
                }
            }
        }
    });
}

/**
 * Initialize chart actions (download, expand, report)
 */
function initializeChartActions() {
    document.querySelectorAll('.chart-action-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            const chartType = this.dataset.chart;
            const action = this.dataset.action;
            
            switch (action) {
                case 'download':
                    downloadChart(chartType);
                    break;
                case 'expand':
                    expandChart(chartType);
                    break;
                case 'report':
                    viewDetailedReport(chartType);
                    break;
            }
        });
    });
    
    /**
     * Download chart as PNG
     * @param {string} chartType - Type of chart to download
     */
    function downloadChart(chartType) {
        const canvas = document.getElementById(
            chartType === 'distribution' ? 'staffDistributionChart' : 'activityTrendChart'
        );
        
        if (canvas) {
            // Create a temporary link to download the image
            const link = document.createElement('a');
            link.download = `${chartType}-chart.png`;
            link.href = canvas.toDataURL('image/png');
            link.click();
            
            showMessage(`Chart downloaded successfully`, 'success');
        }
    }
    
    /**
     * Expand chart to full screen
     * @param {string} chartType - Type of chart to expand
     */
    function expandChart(chartType) {
        // This would typically open a modal with a larger version of the chart
        alert(`Expand ${chartType} chart functionality would be implemented here`);
    }
    
    /**
     * View detailed report for chart
     * @param {string} chartType - Type of chart to view report for
     */
    function viewDetailedReport(chartType) {
        // This would typically navigate to a detailed report page
        alert(`View detailed report for ${chartType} functionality would be implemented here`);
    }
}

/**
 * Initialize database migration functionality
 */
function initializeDatabaseMigration() {
    const manualFixBtn = document.querySelector('.manual-fix-btn');
    const manualFixInstructions = document.getElementById('manualFixInstructions');
    
    if (manualFixBtn && manualFixInstructions) {
        // Toggle manual fix instructions visibility
        manualFixBtn.addEventListener('click', function() {
            manualFixInstructions.classList.toggle('show');
            
            // Update button text
            const isExpanded = manualFixInstructions.classList.contains('show');
            this.innerHTML = isExpanded 
                ? '<i class="fas fa-code mr-2"></i> Hide Manual Fix' 
                : '<i class="fas fa-code mr-2"></i> Show Manual Fix';
        });
    }
    
    // Handle database fix confirmation
    const databaseFixBtn = document.querySelector('.database-fix');
    if (databaseFixBtn) {
        databaseFixBtn.addEventListener('click', function(e) {
            if (!confirm('Are you sure you want to run the database fix? This will modify your database structure.')) {
                e.preventDefault();
            }
        });
    }
    
    // Copy code to clipboard functionality
    const codeBlock = document.querySelector('.code-block');
    if (codeBlock) {
        // Add copy button
        const copyBtn = document.createElement('button');
        copyBtn.className = 'copy-code-btn';
        copyBtn.innerHTML = '<i class="fas fa-copy"></i> Copy';
        copyBtn.title = 'Copy to clipboard';
        
        // Insert the button before the code block
        codeBlock.parentNode.insertBefore(copyBtn, codeBlock);
        
        // Handle copy click
        copyBtn.addEventListener('click', function() {
            const textToCopy = codeBlock.textContent.trim();
            
            // Copy to clipboard
            navigator.clipboard.writeText(textToCopy)
                .then(() => {
                    // Provide visual feedback
                    this.innerHTML = '<i class="fas fa-check"></i> Copied!';
                    this.classList.add('copied');
                    
                    // Reset after delay
                    setTimeout(() => {
                        this.innerHTML = '<i class="fas fa-copy"></i> Copy';
                        this.classList.remove('copied');
                    }, 2000);
                    
                    showMessage('SQL code copied to clipboard!', 'success');
                })
                .catch(err => {
                    console.error('Failed to copy: ', err);
                    showMessage('Failed to copy code. Please try again.', 'error');
                });
        });
    }
}

/**
 * Initialize items per page selector
 */
function initializeItemsPerPage() {
    const itemsPerPageSelect = document.getElementById('items-per-page');
    if (!itemsPerPageSelect) return;
    
    itemsPerPageSelect.addEventListener('change', function() {
        const itemsPerPage = parseInt(this.value);
        // This would typically trigger a reload or re-render of the table with the new items per page
        console.log(`Changed items per page to ${itemsPerPage}`);
        
        // For a real implementation, you would either:
        // 1. Reload the page with a query parameter for items per page
        // 2. Make an AJAX request to get the new data and update the table
        // 3. If all data is already loaded, update the pagination client-side
    });
}

/**
 * Handle the logout button functionality
 */
function handleLogoutButton() {
    const logoutButton = document.getElementById('logoutButton');
    if (logoutButton) {
        logoutButton.addEventListener('click', function() {
            // Submit the logout form
            document.getElementById('logoutForm').submit();
        });
    }
}

/**
 * Helper function to show temporary messages
 * @param {string} message - Message to display
 * @param {string} type - Message type (success, error, info)
 */
function showMessage(message, type = 'info') {
    const messageContainer = document.querySelector('.message-container') || createMessageContainer();
    
    const messageElement = document.createElement('div');
    messageElement.className = `message message-${type}`;
    
    let icon;
    switch (type) {
        case 'success':
            icon = 'fa-check-circle';
            break;
        case 'error':
            icon = 'fa-exclamation-circle';
            break;
        default:
            icon = 'fa-info-circle';
    }
    
    messageElement.innerHTML = `
        <i class="fas ${icon}"></i>
        <span>${message}</span>
    `;
    
    messageContainer.appendChild(messageElement);
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        messageElement.classList.add('fade-out');
        setTimeout(() => {
            messageContainer.removeChild(messageElement);
        }, 300);
    }, 5000);
    
    /**
     * Create message container if it doesn't exist
     * @returns {HTMLElement} Message container element
     */
    function createMessageContainer() {
        const container = document.createElement('div');
        container.className = 'message-container';
        document.body.appendChild(container);
        return container;
    }
}

/**
 * Initialize submenu toggling
 */
function initializeSubmenus() {
    const submenuToggles = document.querySelectorAll('.nav-link[data-toggle="submenu"]');
    
    submenuToggles.forEach(toggle => {
        toggle.addEventListener('click', function(e) {
            e.preventDefault();
            
            const listItem = this.closest('.has-submenu');
            const submenu = listItem.querySelector('.submenu');
            
            // Toggle submenu visibility with animation
            submenu.style.transition = 'max-height 0.3s ease';
            submenu.classList.toggle('show');
            this.classList.toggle('active');
            
            // Add an aria-expanded attribute for accessibility
            const isExpanded = submenu.classList.contains('show');
            this.setAttribute('aria-expanded', isExpanded);
            
            // If submenu is now visible, ensure all parent submenus are also visible
            if (submenu.classList.contains('show')) {
                let parent = listItem.parentElement;
                while (parent) {
                    if (parent.classList.contains('submenu')) {
                        parent.classList.add('show');
                        const parentToggle = parent.previousElementSibling;
                        if (parentToggle && parentToggle.hasAttribute('data-toggle')) {
                            parentToggle.classList.add('active');
                            parentToggle.setAttribute('aria-expanded', 'true');
                        }
                    }
                    parent = parent.parentElement;
                }
            }
        });
        
        // Initialize aria attributes for accessibility
        toggle.setAttribute('aria-expanded', toggle.classList.contains('active'));
        toggle.setAttribute('aria-controls', toggle.nextElementSibling.id || 'submenu-' + Math.random().toString(36).substr(2, 9));
    });
    
    // Expand submenus that contain the current page
    document.querySelectorAll('.submenu-link.active').forEach(activeLink => {
        let parent = activeLink.closest('.submenu');
        while (parent) {
            parent.classList.add('show');
            const toggle = parent.previousElementSibling;
            if (toggle && toggle.hasAttribute('data-toggle')) {
                toggle.classList.add('active');
                toggle.setAttribute('aria-expanded', 'true');
            }
            parent = parent.parentElement.closest('.submenu');
        }
    });
}

/**
 * Fix Add Staff button functionality
 * This ensures the Add Staff button in the sidebar and toolbar work correctly
 */
function fixAddStaffButton() {
    // Fix for sidebar Add Staff link
    const sidebarAddStaffLink = document.querySelector('.submenu-link[href="/Admin/AddStaff"]');
    if (sidebarAddStaffLink) {
        sidebarAddStaffLink.addEventListener('click', function(e) {
            e.preventDefault();
            window.location.href = '/Admin/AddStaffMember';
        });
    }
    
    // Fix for toolbar Add Staff button
    const toolbarAddStaffButton = document.querySelector('.add-staff-btn, .btn-primary[href="/Admin/AddStaff"]');
    if (toolbarAddStaffButton) {
        toolbarAddStaffButton.addEventListener('click', function(e) {
            e.preventDefault();
            window.location.href = '/Admin/AddStaffMember';
        });
    }
}

/**
 * Initialize mobile menu functionality
 */
function initializeMobileMenu() {
    const sidebar = document.querySelector('.sidebar');
    const mainContent = document.querySelector('.main-content');
    const menuToggle = document.createElement('button');
    
    menuToggle.className = 'menu-toggle';
    menuToggle.innerHTML = '<i class="fas fa-bars"></i>';
    menuToggle.style.display = 'none';
    menuToggle.setAttribute('aria-label', 'Toggle navigation menu');
    
    // Add the menu toggle button to the main content
    if (mainContent) {
        // Create a top bar for mobile if it doesn't exist
        let topbar = mainContent.querySelector('.mobile-topbar');
        if (!topbar) {
            topbar = document.createElement('div');
            topbar.className = 'mobile-topbar';
            topbar.style.display = 'none';
            topbar.style.padding = 'var(--spacing-3)';
            topbar.style.display = 'flex';
            topbar.style.justifyContent = 'space-between';
            topbar.style.alignItems = 'center';
            topbar.style.borderBottom = '1px solid var(--border-light)';
            topbar.style.marginBottom = 'var(--spacing-4)';
            
            // Logo for mobile
            const logo = document.createElement('div');
            logo.className = 'mobile-logo';
            logo.innerHTML = '<i class="fas fa-clinic-medical"></i> <span>Barangay Health</span>';
            
            topbar.appendChild(logo);
            topbar.appendChild(menuToggle);
            
            // Insert at the beginning of main content
            mainContent.insertBefore(topbar, mainContent.firstChild);
        } else {
            topbar.appendChild(menuToggle);
        }
    }
    
    // Handle menu toggle click
    menuToggle.addEventListener('click', function() {
        if (sidebar) {
            sidebar.classList.toggle('mobile-visible');
        }
    });
    
    // Add close button to sidebar for mobile
    if (sidebar) {
        const closeBtn = document.createElement('button');
        closeBtn.className = 'mobile-close-btn';
        closeBtn.innerHTML = '<i class="fas fa-times"></i>';
        closeBtn.style.position = 'absolute';
        closeBtn.style.top = '15px';
        closeBtn.style.right = '15px';
        closeBtn.style.display = 'none';
        closeBtn.style.background = 'none';
        closeBtn.style.border = 'none';
        closeBtn.style.color = 'var(--text-primary)';
        closeBtn.style.fontSize = '1.5rem';
        closeBtn.style.cursor = 'pointer';
        closeBtn.style.zIndex = '10';
        closeBtn.setAttribute('aria-label', 'Close navigation menu');
        
        closeBtn.addEventListener('click', function() {
            sidebar.classList.remove('mobile-visible');
        });
        
        sidebar.appendChild(closeBtn);
        
        // Close menu when clicking on a link (for mobile)
        const sidebarLinks = sidebar.querySelectorAll('a');
        sidebarLinks.forEach(link => {
            link.addEventListener('click', function() {
                if (window.innerWidth <= 768) {
                    sidebar.classList.remove('mobile-visible');
                }
            });
        });
        
        // Close menu when clicking outside
        document.addEventListener('click', function(event) {
            if (window.innerWidth <= 768 && 
                sidebar.classList.contains('mobile-visible') && 
                !sidebar.contains(event.target) && 
                event.target !== menuToggle) {
                sidebar.classList.remove('mobile-visible');
            }
        });
    }
    
    // Handle window resize
    function handleResize() {
        if (window.innerWidth <= 768) {
            menuToggle.style.display = 'flex';
            if (sidebar && sidebar.querySelector('.mobile-close-btn')) {
                sidebar.querySelector('.mobile-close-btn').style.display = 'block';
            }
            if (document.querySelector('.mobile-topbar')) {
                document.querySelector('.mobile-topbar').style.display = 'flex';
            }
        } else {
            menuToggle.style.display = 'none';
            if (sidebar) {
                sidebar.classList.remove('mobile-visible');
                if (sidebar.querySelector('.mobile-close-btn')) {
                    sidebar.querySelector('.mobile-close-btn').style.display = 'none';
                }
            }
            if (document.querySelector('.mobile-topbar')) {
                document.querySelector('.mobile-topbar').style.display = 'none';
            }
        }
    }
    
    // Initial check
    handleResize();
    
    // Listen for window resize
    window.addEventListener('resize', handleResize);
} 