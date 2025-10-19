/**
 * User Management JavaScript
 * Handles document migration and preview functionality
 */

// Variables to track state
let currentPage = 1;
let currentFilter = 'all';
let currentSearch = '';
let batchModeActive = false;
let selectedUserIds = [];
let batchOperation = ''; // 'approve' or 'reject'

/**
 * Shows a toast notification with a message
 * @param {string} type - The type of toast: 'success', 'error', 'warning', or 'info'
 * @param {string} message - The message to display in the toast
 * @param {number} duration - How long to show the toast in milliseconds
 */
function showToast(type, message, duration = 3000) {
    // Check if there's already a toast container
    let toastContainer = document.getElementById('toast-container');
    
    // Create container if it doesn't exist
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.style.position = 'fixed';
        toastContainer.style.top = '20px';
        toastContainer.style.right = '20px';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }
    
    // Create the toast element
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.style.minWidth = '250px';
    toast.style.margin = '0 0 10px 0';
    toast.style.padding = '15px';
    toast.style.borderRadius = '5px';
    toast.style.boxShadow = '0 2px 10px rgba(0,0,0,0.2)';
    toast.style.animation = 'fadeIn 0.3s, fadeOut 0.3s ' + (duration/1000 - 0.3) + 's';
    toast.style.opacity = '0';
    
    // Set background color based on type
    switch (type) {
        case 'success':
            toast.style.backgroundColor = '#28a745';
            toast.style.borderLeft = '5px solid #1e7e34';
            break;
        case 'error':
            toast.style.backgroundColor = '#dc3545';
            toast.style.borderLeft = '5px solid #bd2130';
            break;
        case 'warning':
            toast.style.backgroundColor = '#ffc107';
            toast.style.borderLeft = '5px solid #d39e00';
            toast.style.color = '#333';
            break;
        case 'info':
        default:
            toast.style.backgroundColor = '#17a2b8';
            toast.style.borderLeft = '5px solid #138496';
            break;
    }
    
    // Set toast content
    toast.style.color = (type === 'warning') ? '#333' : '#fff';
    toast.textContent = message;
    
    // Add animation styles if they don't exist
    if (!document.getElementById('toast-animations')) {
        const style = document.createElement('style');
        style.id = 'toast-animations';
        style.textContent = `
            @keyframes fadeIn {
                from { opacity: 0; transform: translateY(-20px); }
                to { opacity: 1; transform: translateY(0); }
            }
            @keyframes fadeOut {
                from { opacity: 1; transform: translateY(0); }
                to { opacity: 0; transform: translateY(-20px); }
            }
        `;
        document.head.appendChild(style);
    }
    
    // Add the toast to the container
    toastContainer.appendChild(toast);
    
    // Trigger animation and make toast visible
    setTimeout(() => {
        toast.style.opacity = '1';
    }, 10);
    
    // Remove the toast after duration
    setTimeout(() => {
        if (toast && toast.parentNode) {
            toast.style.opacity = '0';
            setTimeout(() => {
                if (toast && toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        }
    }, duration);
}

// Initialize when document is ready
$(document).ready(function() {
    console.log('User Management script initialized');
    
    // Setup event handlers
    setupUserManagementEventHandlers();
    
    // Check for errors that need handling
    handleUpdatePaginationError();
    
    // Setup batch operations
    setupBatchOperations();
    
    // Guardian modal initialization
    setupGuardianModalHandlers();
    
    // Document preview
    const previewButtons = document.querySelectorAll('.preview-document');
    if (previewButtons.length > 0) {
        previewButtons.forEach(button => {
            button.addEventListener('click', function() {
                const docId = this.getAttribute('data-id');
                const docPath = this.getAttribute('data-path');
                const docName = this.getAttribute('data-name');
                const docType = this.getAttribute('data-type');
                const docUser = this.getAttribute('data-user');
                
                // Update modal content
                document.getElementById('fileName').textContent = docName;
                document.getElementById('fileType').textContent = docType;
                document.getElementById('fileOwner').textContent = docUser;
                
                const previewArea = document.getElementById('documentPreviewArea');
                
                // Clear previous preview
                previewArea.innerHTML = '';
                
                // Check file type and create appropriate preview
                if (docType.startsWith('image/')) {
                    // Image preview
                    const img = document.createElement('img');
                    img.src = docPath;
                    img.alt = docName;
                    img.className = 'img-fluid';
                    previewArea.appendChild(img);
                } else if (docType === 'application/pdf') {
                    // PDF preview
                    const iframe = document.createElement('iframe');
                    iframe.src = docPath;
                    iframe.width = '100%';
                    iframe.height = '500';
                    iframe.className = 'pdf-preview';
                    previewArea.appendChild(iframe);
                } else {
                    // Generic file preview
                    const fileIcon = document.createElement('div');
                    fileIcon.className = 'file-icon text-center py-5';
                    fileIcon.innerHTML = `
                        <i class="fas fa-file fa-5x mb-3"></i>
                        <p>This file type cannot be previewed</p>
                    `;
                    previewArea.appendChild(fileIcon);
                }
                
                // Update view button link
                const viewFileBtn = document.getElementById('viewFileBtn');
                viewFileBtn.setAttribute('onclick', `window.open('${docPath}', '_blank')`);
            });
        });
    }
    
    // Document migration
    const migrateButtons = document.querySelectorAll('.migrate-document');
    if (migrateButtons.length > 0) {
        migrateButtons.forEach(button => {
            button.addEventListener('click', function() {
                const userId = this.getAttribute('data-id');
                const filePath = this.getAttribute('data-path');
                
                if (confirm('Do you want to migrate this legacy document to the new system?')) {
                    // Disable button to prevent multiple clicks
                    this.disabled = true;
                    this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Migrating...';
                    
                    // Get anti-forgery token
                    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                    if (!tokenInput) {
                        alert('Could not find anti-forgery token. Please refresh the page and try again.');
                        return;
                    }
                    
                    // Make the migration request
                    fetch('/Admin/UserManagement?handler=MigrateDocument', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/x-www-form-urlencoded',
                            'RequestVerificationToken': tokenInput.value
                        },
                        body: `id=${userId}`
                    })
                    .then(response => response.json())
                    .then(data => {
                        if (data.success) {
                            // Replace the button with success message
                            const parentDiv = this.closest('.document-actions');
                            parentDiv.innerHTML = `
                                <div class="alert alert-success py-1 mb-0">
                                    <i class="fas fa-check-circle me-1"></i> 
                                    Document migrated successfully
                                </div>
                            `;
                            
                            // Update UI or reload page after a delay
                            setTimeout(() => {
                                location.reload();
                            }, 2000);
                        } else {
                            // Re-enable button and show error
                            this.disabled = false;
                            this.innerHTML = '<i class="fas fa-exchange-alt"></i> Retry';
                            alert('Migration failed: ' + data.message);
                        }
                    })
                    .catch(error => {
                        console.error('Error during migration:', error);
                        this.disabled = false;
                        this.innerHTML = '<i class="fas fa-exchange-alt"></i> Retry';
                        alert('An error occurred during migration. Please try again.');
                    });
                }
            });
        });
    }
    
    // Filter dropdown
    const filterItems = document.querySelectorAll('.filter-item');
    if (filterItems.length > 0) {
        filterItems.forEach(item => {
            item.addEventListener('click', function(e) {
                e.preventDefault();
                
                // Update active class
                filterItems.forEach(i => i.classList.remove('active'));
                this.classList.add('active');
                
                // Update dropdown text
                const filterText = this.textContent;
                document.getElementById('filterDropdown').innerHTML = `<i class="fas fa-filter me-1"></i> ${filterText}`;
                
                // Get filter value
                const filterValue = this.getAttribute('data-filter');
                currentFilter = filterValue;
                
                // Apply filter
                applyFilter();
            });
        });
    }
    
    // Search functionality
    const searchInput = document.getElementById('userSearchInput');
    const searchButton = document.getElementById('searchButton');
    
    if (searchInput && searchButton) {
        // Search on button click
        searchButton.addEventListener('click', function() {
            currentSearch = searchInput.value.toLowerCase().trim();
            applyFilter();
        });
        
        // Search on Enter key
        searchInput.addEventListener('keyup', function(e) {
            if (e.key === 'Enter') {
                currentSearch = this.value.toLowerCase().trim();
                applyFilter();
            }
        });
    }
});

// Setup batch operations
function setupBatchOperations() {
    const toggleBatchBtn = document.getElementById('toggleBatchMode');
    const batchOperationsPanel = document.getElementById('batchOperations');
    const selectAllCheckbox = document.getElementById('selectAllUsers');
    const batchApproveBtn = document.getElementById('batchApproveBtn');
    const batchRejectBtn = document.getElementById('batchRejectBtn');
    const cancelBatchBtn = document.getElementById('cancelBatchBtn');
    const confirmBatchBtn = document.getElementById('confirmBatchBtn');
    const batchConfirmationModal = new bootstrap.Modal(document.getElementById('batchConfirmationModal'));
    
    if (!toggleBatchBtn || !batchOperationsPanel) return;
    
    // Toggle batch mode
    toggleBatchBtn.addEventListener('click', function() {
        batchModeActive = !batchModeActive;
        toggleBatchMode(batchModeActive);
    });
    
    // Select all users
    selectAllCheckbox.addEventListener('change', function() {
        const checkboxes = document.querySelectorAll('.user-checkbox:not([disabled])');
        checkboxes.forEach(checkbox => {
            checkbox.checked = this.checked;
            updateCardSelection(checkbox);
        });
        updateSelectedCount();
    });
    
    // Handle individual checkbox changes
    document.addEventListener('change', function(e) {
        if (e.target.classList.contains('user-checkbox')) {
            updateCardSelection(e.target);
            updateSelectedCount();
        }
    });
    
    // Handle batch approve button
    batchApproveBtn.addEventListener('click', function() {
        if (selectedUserIds.length === 0) return;
        
        batchOperation = 'approve';
        document.getElementById('batchConfirmationText').textContent = 
            `Are you sure you want to approve ${selectedUserIds.length} users?`;
        document.getElementById('confirmBatchBtn').className = 'btn btn-approve-minimal';
        batchConfirmationModal.show();
    });
    
    // Handle batch reject button
    batchRejectBtn.addEventListener('click', function() {
        if (selectedUserIds.length === 0) return;
        
        batchOperation = 'reject';
        document.getElementById('batchConfirmationText').textContent = 
            `Are you sure you want to reject ${selectedUserIds.length} users?`;
        document.getElementById('confirmBatchBtn').className = 'btn btn-reject-minimal';
        batchConfirmationModal.show();
    });
    
    // Handle cancel batch button
    cancelBatchBtn.addEventListener('click', function() {
        batchModeActive = false;
        toggleBatchMode(false);
    });
    
    // Handle confirm batch button
    confirmBatchBtn.addEventListener('click', function() {
        processBatchOperation();
        batchConfirmationModal.hide();
    });
}

// Toggle batch mode
function toggleBatchMode(enable) {
    const toggleBatchBtn = document.getElementById('toggleBatchMode');
    const batchOperationsPanel = document.getElementById('batchOperations');
    const checkboxContainers = document.querySelectorAll('.user-select-checkbox');
    
    if (enable) {
        toggleBatchBtn.classList.add('active');
        batchOperationsPanel.style.display = 'block';
        checkboxContainers.forEach(container => {
            container.style.display = 'block';
        });
        
        // Disable checkboxes for users that are not pending
        document.querySelectorAll('.user-checkbox').forEach(checkbox => {
            const status = checkbox.getAttribute('data-status');
            if (status !== 'pending') {
                checkbox.disabled = true;
                checkbox.title = 'Only pending users can be selected';
            }
        });
    } else {
        toggleBatchBtn.classList.remove('active');
        batchOperationsPanel.style.display = 'none';
        checkboxContainers.forEach(container => {
            container.style.display = 'none';
        });
        
        // Clear all selections
        document.querySelectorAll('.user-card').forEach(card => {
            card.classList.remove('selected');
        });
        document.querySelectorAll('.user-checkbox').forEach(checkbox => {
            checkbox.checked = false;
        });
        document.getElementById('selectAllUsers').checked = false;
        selectedUserIds = [];
        updateSelectedCount();
    }
}

// Update card selection
function updateCardSelection(checkbox) {
    const card = checkbox.closest('.user-card');
    if (card) {
        if (checkbox.checked) {
            card.classList.add('selected');
            // Add to selected user IDs if not already in the array
            const userId = checkbox.getAttribute('data-user-id');
            if (!selectedUserIds.includes(userId)) {
                selectedUserIds.push(userId);
            }
        } else {
            card.classList.remove('selected');
            // Remove from selected user IDs
            const userId = checkbox.getAttribute('data-user-id');
            selectedUserIds = selectedUserIds.filter(id => id !== userId);
        }
    }
}

// Update selected count
function updateSelectedCount() {
    const countElement = document.getElementById('selectedCount');
    if (countElement) {
        countElement.textContent = selectedUserIds.length;
    }
    
    // Update button states
    const batchApproveBtn = document.getElementById('batchApproveBtn');
    const batchRejectBtn = document.getElementById('batchRejectBtn');
    
    if (batchApproveBtn && batchRejectBtn) {
        if (selectedUserIds.length > 0) {
            batchApproveBtn.disabled = false;
            batchRejectBtn.disabled = false;
        } else {
            batchApproveBtn.disabled = true;
            batchRejectBtn.disabled = true;
        }
    }
}

// Process batch operation
function processBatchOperation() {
    if (selectedUserIds.length === 0) return;
    
    showLoading(true);
    
    // Get anti-forgery token
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenInput) {
        alert('Could not find anti-forgery token. Please refresh the page and try again.');
        showLoading(false);
        return;
    }
    
    // Determine handler
    const handler = batchOperation === 'approve' ? 'BatchApprove' : 'BatchReject';
    
    // Make the request
    fetch(`/Admin/UserManagement?handler=${handler}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': tokenInput.value
        },
        body: JSON.stringify({ userIds: selectedUserIds })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Show success message
            const message = batchOperation === 'approve' 
                ? `Successfully approved ${data.processedCount} users` 
                : `Successfully rejected ${data.processedCount} users`;
            
            // Show toast notification if available
            if (typeof showToast === 'function') {
                showToast('success', message);
            } else {
                // Show alert if no toast function is available
                alert(message);
            }
            
            // Reload the page after a delay
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            // Show error
            const errorMessage = data.message || 'Operation failed';
            if (data.errors && data.errors.length > 0) {
                console.error('Batch operation errors:', data.errors);
            }
            
            if (typeof showToast === 'function') {
                showToast('error', errorMessage);
            } else {
                alert(`Operation failed: ${errorMessage}`);
            }
            showLoading(false);
        }
    })
    .catch(error => {
        console.error('Error during batch operation:', error);
        if (typeof showToast === 'function') {
            showToast('error', 'An error occurred. Please try again.');
        } else {
            alert('An error occurred. Please try again.');
        }
        showLoading(false);
    });
}

// Function to apply both filter and search simultaneously
function applyFilter() {
    // Show loading overlay
    showLoading(true);
    
    setTimeout(() => {
        const cards = document.querySelectorAll('.user-card-col');
        let visibleCount = 0;
        const emptyState = document.querySelector('.empty-state');
        
        cards.forEach(card => {
            const statusBadge = card.querySelector('.status-badge');
            const status = statusBadge ? statusBadge.textContent.toLowerCase().trim() : '';
            
            // Check if card matches the filter
            let matchesFilter = currentFilter === 'all' || status === currentFilter;
            
            // Check if card matches the search term
            let matchesSearch = true;
            if (currentSearch !== '') {
                const username = card.querySelector('.card-header span').textContent.toLowerCase();
                const fullName = card.querySelector('.user-info-group:nth-child(1) .fw-medium').textContent.toLowerCase();
                const email = card.querySelector('.user-info-group:nth-child(2) .fw-medium').textContent.toLowerCase();
                
                matchesSearch = username.includes(currentSearch) || 
                                fullName.includes(currentSearch) || 
                                email.includes(currentSearch);
            }
            
            // Apply visibility based on both filter and search
            if (matchesFilter && matchesSearch) {
                card.style.display = '';
                visibleCount++;
            } else {
                card.style.display = 'none';
            }
        });
        
        // Show/hide empty state based on results
        if (emptyState) {
            if (visibleCount === 0) {
                emptyState.style.display = 'flex';
            } else {
                emptyState.style.display = 'none';
            }
        }
        
        // Update pending count in alert
        updatePendingAlert();
        
        // Hide loading overlay
        showLoading(false);
    }, 400); // Short delay for better UX
}

// Function to show/hide loading overlay
function showLoading(show) {
    const overlay = document.getElementById('loadingOverlay');
    if (!overlay) return;
    
    if (show) {
        overlay.classList.add('active');
    } else {
        setTimeout(() => {
            overlay.classList.remove('active');
        }, 300);
    }
}

// Function to update the pending alert
function updatePendingAlert() {
    const pendingAlert = document.getElementById('pendingUsersAlert');
    if (!pendingAlert) return;
    
    const pendingBadges = document.querySelectorAll('.status-badge:not([style*="display: none"])');
    let pendingCount = 0;
    
    pendingBadges.forEach(badge => {
        if (badge.textContent.trim().toLowerCase() === 'pending') {
            pendingCount++;
        }
    });
    
    if (pendingCount > 0) {
        const pendingCountElement = pendingAlert.querySelector('.pending-count');
        if (pendingCountElement) {
            pendingCountElement.textContent = pendingCount;
        }
        pendingAlert.style.display = 'block';
    } else {
        pendingAlert.style.display = 'none';
    }
}

// Set up event handlers for the user management page
function setupUserManagementEventHandlers() {
    // Reload button click for error handling
    $('#reloadPageBtn').on('click', function() {
        location.reload();
    });
    
    // Search functionality
    $('#searchButton').on('click', function() {
        const searchTerm = $('#userSearchInput').val();
        currentSearch = searchTerm.toLowerCase().trim();
        applyFilter();
    });
    
    // Search on enter key
    $('#userSearchInput').on('keypress', function(e) {
        if (e.which === 13) {
            const searchTerm = $(this).val();
            currentSearch = searchTerm.toLowerCase().trim();
            applyFilter();
        }
    });
    
    // Add card animation on page load
    animateCards();
}

// Animate cards with a staggered entry effect
function animateCards() {
    const cards = document.querySelectorAll('.user-card-col');
    cards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        card.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
        
        setTimeout(() => {
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, 100 + (index * 50)); // Staggered delay
    });
}

// Function to update pagination
function updatePagination(totalPages, currentPage = 1, filter = 'all', search = '') {
    console.log(`Updating pagination: pages=${totalPages}, current=${currentPage}, filter=${filter}, search=${search}`);
    
    const paginationElement = document.getElementById('userPagination');
    if (!paginationElement) {
        console.error('Pagination element not found!');
        return;
    }
    
    // Clear existing pagination
    paginationElement.innerHTML = '';
    
    // Don't show pagination if there's only one page or no pages
    if (totalPages <= 1) {
        console.log('No pagination needed (1 or fewer pages)');
        return;
    }
    
    // Add Previous button
    const prevLi = document.createElement('li');
    prevLi.className = `page-item ${currentPage <= 1 ? 'disabled' : ''}`;
    
    const prevLink = document.createElement('a');
    prevLink.className = 'page-link';
    prevLink.href = '#';
    prevLink.innerHTML = '&laquo;';
    prevLink.setAttribute('aria-label', 'Previous');
    if (currentPage > 1) {
        prevLink.dataset.page = currentPage - 1;
    }
    
    prevLi.appendChild(prevLink);
    paginationElement.appendChild(prevLi);
    
    // Determine which page numbers to show
    let startPage = Math.max(1, currentPage - 2);
    let endPage = Math.min(totalPages, startPage + 4);
    
    // Adjust if we're near the end
    if (endPage - startPage < 4) {
        startPage = Math.max(1, endPage - 4);
    }
    
    // Add page numbers
    for (let i = startPage; i <= endPage; i++) {
        const pageLi = document.createElement('li');
        pageLi.className = `page-item ${i === currentPage ? 'active' : ''}`;
        
        const pageLink = document.createElement('a');
        pageLink.className = 'page-link';
        pageLink.href = '#';
        pageLink.textContent = i;
        pageLink.dataset.page = i;
        
        pageLi.appendChild(pageLink);
        paginationElement.appendChild(pageLi);
    }
    
    // Add Next button
    const nextLi = document.createElement('li');
    nextLi.className = `page-item ${currentPage >= totalPages ? 'disabled' : ''}`;
    
    const nextLink = document.createElement('a');
    nextLink.className = 'page-link';
    nextLink.href = '#';
    nextLink.innerHTML = '&raquo;';
    nextLink.setAttribute('aria-label', 'Next');
    if (currentPage < totalPages) {
        nextLink.dataset.page = currentPage + 1;
    }
    
    nextLi.appendChild(nextLink);
    paginationElement.appendChild(nextLi);
    
    // Save current state
    currentPage = currentPage;
    currentFilter = filter;
    currentSearch = search;
    
    console.log('Pagination updated successfully');
}

// Handle the error for missing updatePagination function
function handleUpdatePaginationError() {
    const errorElement = document.querySelector('.alert-danger');
    if (errorElement && errorElement.textContent.includes('updatePagination is not defined')) {
        console.log('Detected updatePagination error, providing reload button');
        
        // Replace the error message with a better one that includes a reload button
        errorElement.innerHTML = `
            <i class="fas fa-exclamation-circle"></i> 
            Error loading users. The page needs to be reloaded to fix this issue.
            <button id="reloadPageBtn" class="btn btn-sm btn-primary ms-3">Reload Page</button>
        `;
        
        // Attach event listener to the reload button
        document.getElementById('reloadPageBtn').addEventListener('click', function() {
            location.reload();
        });
    }
}

// Function to update user status
function updateUserStatus(userId, status) {
    // Get the anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    // Show loading state
    const button = document.querySelector(`button[data-id="${userId}"]`);
    const originalText = button.innerHTML;
    button.disabled = true;
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
    
    // Make API call
    fetch(`/Admin/UserManagement?handler=UpdateUserStatus`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ userId, status })
    })
    .then(resp => resp.json().catch(() => ({ success: false, message: 'Unexpected response from server.' })))
    .then(data => {
        if (!data || data.success !== true) {
            const msg = (data && data.message) ? data.message : 'Failed to update user status. Please try again.';
            if (typeof showToast === 'function') {
                showToast('error', msg);
            } else {
                showErrorMessage(msg);
            }
            button.disabled = false;
            button.innerHTML = originalText;
            return;
        }

        // Show success message
        if (typeof showToast === 'function') {
            showToast('success', data.message || `User status updated to ${status}`);
        } else {
            showSuccessMessage(data.message || `User status updated to ${status}`);
        }

        // Update UI
        const row = button.closest('tr');
        const statusCell = row.querySelector('.status-badge');
        statusCell.textContent = status;
        statusCell.className = `status-badge ${status.toLowerCase()}`;

        // Remove action buttons
        const actionButtons = row.querySelector('.action-buttons');
        actionButtons.innerHTML = '';

        // Refresh the page after a short delay
        setTimeout(() => {
            window.location.reload();
        }, 1200);
    })
    .catch(error => {
        console.error('Error:', error);
        if (typeof showToast === 'function') {
            showToast('error', 'Failed to update user status. Please try again.');
        } else {
            showErrorMessage('Failed to update user status. Please try again.');
        }
        button.disabled = false;
        button.innerHTML = originalText;
    });
}

// Function to show success message
function showSuccessMessage(message) {
    const successModal = document.getElementById('successModal');
    if (successModal) {
        document.getElementById('successMessage').textContent = message;
        new bootstrap.Modal(successModal).show();
    } else {
        alert(message);
    }
}

// Function to show error message
function showErrorMessage(message) {
    alert(message);
}

// Attach event listeners when document is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Get DOM elements
    const searchInput = document.getElementById('userSearchInput');
    const statusFilter = document.getElementById('statusFilter');
    const userTable = document.querySelector('.table tbody');

    // Initialize search functionality
    if (searchInput) {
        searchInput.addEventListener('input', filterUsers);
    }

    // Initialize status filter
    if (statusFilter) {
        statusFilter.addEventListener('change', filterUsers);
    }

    // Initialize action buttons
    initializeActionButtons();

    // Function to filter users based on search and status
    function filterUsers() {
        const searchText = searchInput.value.toLowerCase();
        const selectedStatus = statusFilter.value.toLowerCase();
        const rows = userTable.getElementsByTagName('tr');

        Array.from(rows).forEach(row => {
            const name = row.cells[1].textContent.toLowerCase();
            const email = row.cells[2].textContent.toLowerCase();
            const status = row.querySelector('.status-badge').textContent.toLowerCase();
            
            const matchesSearch = name.includes(searchText) || email.includes(searchText);
            const matchesStatus = selectedStatus === 'all' || status === selectedStatus;

            row.style.display = matchesSearch && matchesStatus ? '' : 'none';
        });
    }

    // Function to show custom confirmation modal
    function showConfirmModal(title, message, onConfirm, type = 'approve') {
        const modal = document.getElementById('confirmationModal');
        const modalHeader = document.getElementById('confirmModalHeader');
        const modalTitle = document.getElementById('confirmModalTitle');
        const modalMessage = document.getElementById('confirmModalMessage');
        const okBtn = document.getElementById('confirmModalOkBtn');
        
        // Set modal content
        modalTitle.textContent = title;
        modalMessage.textContent = message;
        
        // Style based on action type
        if (type === 'approve') {
            modalHeader.className = 'modal-header text-white';
            modalHeader.style.backgroundColor = '#FF8C42';
            okBtn.className = 'btn';
            okBtn.style.backgroundColor = '#FF8C42';
            okBtn.style.color = '#fff';
            okBtn.style.border = 'none';
            okBtn.innerHTML = '<i class="fa-solid fa-check me-1"></i> Approve';
        } else if (type === 'reject') {
            modalHeader.className = 'modal-header text-white';
            modalHeader.style.backgroundColor = '#6c757d';
            okBtn.className = 'btn btn-reject-minimal';
            okBtn.innerHTML = '<i class="fa-solid fa-xmark me-1"></i> Reject';
        }
        
        // Remove any existing event listeners
        const newOkBtn = okBtn.cloneNode(true);
        okBtn.parentNode.replaceChild(newOkBtn, okBtn);
        
        // Add new event listener
        newOkBtn.addEventListener('click', function() {
            const bsModal = bootstrap.Modal.getInstance(modal);
            bsModal.hide();
            onConfirm();
        });
        
        // Show modal
        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();
    }

    // Function to initialize action buttons
    function initializeActionButtons() {
        // Approve buttons
        document.querySelectorAll('.btn-action.approve').forEach(button => {
            button.addEventListener('click', async function() {
                const userId = this.getAttribute('data-id');
                showConfirmModal(
                    'Confirm Approval',
                    'Are you sure you want to approve this user?',
                    async () => {
                        await updateUserStatus(userId, 'Verified');
                    },
                    'approve'
                );
            });
        });

        // Reject buttons
        document.querySelectorAll('.btn-action.reject').forEach(button => {
            button.addEventListener('click', async function() {
                const userId = this.getAttribute('data-id');
                showConfirmModal(
                    'Confirm Rejection',
                    'Are you sure you want to reject this user?',
                    async () => {
                        await updateUserStatus(userId, 'Rejected');
                    },
                    'reject'
                );
            });
        });
    }
});

// Add this function to handle viewing user details
function viewUserDetails(userId) {
    // Redirect to user details page
    window.location.href = `/Admin/UserDetails?id=${userId}`;
}

/**
 * Sets up event handlers for the guardian information modal
 */
function setupGuardianModalHandlers() {
    let currentUserId = null;

    // Guardian modal shown event
    $('#guardianModal').on('show.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        currentUserId = button.data('userid');
        
        if (!currentUserId) {
            console.error('No user ID provided for guardian information');
            return;
        }
        
        // Show loading state
        $('#proofPlaceholder').html(`
            <div class="text-center">
                <i class="fas fa-spinner fa-spin fa-2x mb-2"></i>
                <p>Loading guardian information...</p>
            </div>
        `);
        
        $('#residencyProofImage').addClass('d-none');
        $('#viewProofDocumentBtn').addClass('disabled');
        
        // Get the anti-forgery token
        const token = $('input[name="__RequestVerificationToken"]').val();
        
        // Fetch guardian information via AJAX
        fetch(`/Admin/UserManagement?handler=GuardianProof&userId=${currentUserId}`, {
            headers: {
                'RequestVerificationToken': token
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data && data.success) {
                // Update guardian name
                $('#guardianName').text(`${data.guardianFirstName || ''} ${data.guardianLastName || ''}`.trim());
                
                // Update consent status badge
                const statusBadge = $('#consentStatusBadge');
                statusBadge.removeClass('bg-success bg-warning bg-danger');
                
                switch (data.consentStatus?.toLowerCase()) {
                    case 'approved':
                        statusBadge.addClass('bg-success').text('Approved');
                        $('#approveConsentBtn').prop('disabled', true);
                        $('#rejectConsentBtn').prop('disabled', false);
                        break;
                    case 'rejected':
                        statusBadge.addClass('bg-danger').text('Rejected');
                        $('#approveConsentBtn').prop('disabled', false);
                        $('#rejectConsentBtn').prop('disabled', true);
                        break;
                    default:
                        statusBadge.addClass('bg-warning').text('Pending');
                        $('#approveConsentBtn').prop('disabled', false);
                        $('#rejectConsentBtn').prop('disabled', false);
                }
                
                // Update proof type
                $('#proofType').text(data.proofType || 'Unknown');
                
                // Update consent date if available
                if (data.createdAt) {
                    const consentDate = new Date(data.createdAt);
                    $('#consentDate').text(consentDate.toLocaleDateString());
                } else {
                    $('#consentDate').text('Not available');
                }
                
                // Update proof document
                if (data.hasProof && data.proofPath) {
                    $('#proofPlaceholder').addClass('d-none');
                    $('#residencyProofImage').attr('src', data.proofPath).removeClass('d-none');
                    $('#viewProofDocumentBtn').removeClass('disabled').attr('href', data.proofPath);
                } else {
                    $('#proofPlaceholder').html(`
                        <div class="text-center text-muted">
                            <i class="fas fa-file-excel fa-3x mb-2"></i>
                            <p>No proof document provided</p>
                        </div>
                    `);
                    $('#residencyProofImage').addClass('d-none');
                    $('#viewProofDocumentBtn').addClass('disabled');
                }
            } else {
                // Handle error or no data
                $('#proofPlaceholder').html(`
                    <div class="text-center text-warning">
                        <i class="fas fa-exclamation-triangle fa-3x mb-2"></i>
                        <p>No guardian information available</p>
                    </div>
                `);
                $('#guardianName').text('Not provided');
                $('#consentDate').text('Not available');
                $('#proofType').text('Not available');
                $('#consentStatusBadge').removeClass('bg-success bg-warning bg-danger').addClass('bg-danger').text('Missing');
                $('#approveConsentBtn').prop('disabled', true);
                $('#rejectConsentBtn').prop('disabled', true);
            }
        })
        .catch(error => {
            console.error('Error fetching guardian information:', error);
            
            $('#proofPlaceholder').html(`
                <div class="text-center text-danger">
                    <i class="fas fa-exclamation-circle fa-3x mb-2"></i>
                    <p>Error loading guardian information</p>
                </div>
            `);
            $('#approveConsentBtn').prop('disabled', true);
            $('#rejectConsentBtn').prop('disabled', true);
        });
    });
    
    // Handle approve consent button
    $('#approveConsentBtn').on('click', function() {
        if (!currentUserId) {
            showToast('error', 'Missing user ID for approval', 3000);
            return;
        }
        
        updateGuardianConsentStatus(currentUserId, 'Approved');
    });
    
    // Handle reject consent button
    $('#rejectConsentBtn').on('click', function() {
        if (!currentUserId) {
            showToast('error', 'Missing user ID for rejection', 3000);
            return;
        }
        
        updateGuardianConsentStatus(currentUserId, 'Rejected');
    });
    
    // Function to update guardian consent status
    function updateGuardianConsentStatus(userId, status) {
        // Show loading
        const approveBtn = $('#approveConsentBtn');
        const rejectBtn = $('#rejectConsentBtn');
        const originalApproveHtml = approveBtn.html();
        const originalRejectHtml = rejectBtn.html();
        
        if (status === 'Approved') {
            approveBtn.html('<i class="fas fa-spinner fa-spin"></i> Processing...').prop('disabled', true);
            rejectBtn.prop('disabled', true);
        } else {
            rejectBtn.html('<i class="fas fa-spinner fa-spin"></i> Processing...').prop('disabled', true);
            approveBtn.prop('disabled', true);
        }
        
        // Get the anti-forgery token
        const token = $('input[name="__RequestVerificationToken"]').val();
        
        // Make API call to update status
        fetch('/Admin/UserManagement?handler=UpdateGuardianConsent', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ userId: userId, status: status })
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            if (data && data.success) {
                // Update UI to reflect the new status
                const statusBadge = $('#consentStatusBadge');
                statusBadge.removeClass('bg-success bg-warning bg-danger');
                
                if (status === 'Approved') {
                    statusBadge.addClass('bg-success').text('Approved');
                    approveBtn.prop('disabled', true);
                    rejectBtn.prop('disabled', false);
                } else {
                    statusBadge.addClass('bg-danger').text('Rejected');
                    approveBtn.prop('disabled', false);
                    rejectBtn.prop('disabled', true);
                }
                
                // Show success message
                showToast('success', `Guardian consent ${status.toLowerCase()} successfully`, 3000);
                
                // Update the table row status badge after a delay
                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                // Handle error
                showToast('error', data.message || 'Failed to update consent status', 3000);
                
                // Reset buttons
                approveBtn.html(originalApproveHtml).prop('disabled', false);
                rejectBtn.html(originalRejectHtml).prop('disabled', false);
            }
        })
        .catch(error => {
            console.error('Error updating guardian consent:', error);
            showToast('error', 'Error updating guardian consent status', 3000);
            
            // Reset buttons
            approveBtn.html(originalApproveHtml).prop('disabled', false);
            rejectBtn.html(originalRejectHtml).prop('disabled', false);
        });
    }
} 