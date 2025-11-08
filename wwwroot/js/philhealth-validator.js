/**
 * PhilHealth ID Validation Module
 * 
 * Validates PhilHealth ID format: XX-XXXXXXXXX-X (12 digits with hyphens)
 * Example: 02-027851766-8
 * 
 * @version 1.0.0
 * @author BHCARE Development Team
 */

(function(global) {
    'use strict';

    /**
     * Validate PhilHealth ID format
     * Format: XX-XXXXXXXXX-X (2 digits, hyphen, 9 digits, hyphen, 1 digit)
     * Example: 02-027851766-8
     * 
     * @param {string} philhealthId - PhilHealth ID number to validate
     * @returns {boolean} - True if valid format, false otherwise
     */
    function validatePhilHealthId(philhealthId) {
        if (!philhealthId) return false;
        
        // Remove any leading/trailing whitespace
        const cleanId = philhealthId.trim();
        
        // Check format: XX-XXXXXXXXX-X
        // 2 digits, hyphen, 9 digits, hyphen, 1 check digit
        const philhealthPattern = /^\d{2}-\d{9}-\d{1}$/;
        
        if (!philhealthPattern.test(cleanId)) {
            return false;
        }
        
        // Additional validation: Check if it's all zeros (invalid)
        if (cleanId.replace(/-/g, '') === '000000000000') {
            return false;
        }
        
        return true;
    }

    /**
     * Format PhilHealth ID with hyphens
     * Converts input like "020278517668" to "02-027851766-8"
     * 
     * @param {string} input - Raw PhilHealth ID input (with or without hyphens)
     * @returns {string} - Formatted PhilHealth ID
     */
    function formatPhilHealthId(input) {
        if (!input) return '';
        
        // Remove all non-digit characters
        const digitsOnly = input.replace(/\D/g, '');
        
        // Check if we have exactly 12 digits
        if (digitsOnly.length !== 12) {
            return input; // Return as-is if not 12 digits
        }
        
        // Format: XX-XXXXXXXXX-X
        return `${digitsOnly.substring(0, 2)}-${digitsOnly.substring(2, 11)}-${digitsOnly.substring(11, 12)}`;
    }

    /**
     * Real-time PhilHealth ID input formatter
     * Automatically adds hyphens as user types
     * 
     * @param {HTMLInputElement} inputElement - PhilHealth ID input field
     */
    function setupPhilHealthFormatter(inputElement) {
        if (!inputElement) return;
        
        inputElement.addEventListener('input', function(e) {
            let value = e.target.value.replace(/\D/g, ''); // Remove non-digits
            let formatted = '';
            
            // Add first 2 digits
            if (value.length > 0) {
                formatted = value.substring(0, 2);
            }
            
            // Add hyphen and next 9 digits
            if (value.length > 2) {
                formatted += '-' + value.substring(2, 11);
            }
            
            // Add hyphen and last digit
            if (value.length > 11) {
                formatted += '-' + value.substring(11, 12);
            }
            
            e.target.value = formatted;
        });
        
        // Limit to 14 characters (12 digits + 2 hyphens)
        inputElement.setAttribute('maxlength', '14');
        inputElement.setAttribute('placeholder', 'XX-XXXXXXXXX-X');
        inputElement.setAttribute('pattern', '\\d{2}-\\d{9}-\\d{1}');
    }

    /**
     * Add validation to PhilHealth ID input field
     * Shows error message if invalid format
     * 
     * @param {HTMLInputElement} inputElement - PhilHealth ID input field
     * @param {string} errorElementId - ID of error message element (optional)
     */
    function setupPhilHealthValidation(inputElement, errorElementId) {
        if (!inputElement) return;
        
        const errorElement = errorElementId ? document.getElementById(errorElementId) : null;
        
        // Setup formatter
        setupPhilHealthFormatter(inputElement);
        
        // Validation on blur (when user leaves the field)
        inputElement.addEventListener('blur', function(e) {
            const value = e.target.value.trim();
            
            // Skip validation if field is empty
            if (value.length === 0) {
                e.target.classList.remove('is-invalid', 'is-valid');
                if (errorElement) errorElement.style.display = 'none';
                return;
            }
            
            // Validate format
            if (validatePhilHealthId(value)) {
                e.target.classList.remove('is-invalid');
                e.target.classList.add('is-valid');
                if (errorElement) {
                    errorElement.style.display = 'none';
                }
            } else {
                e.target.classList.remove('is-valid');
                e.target.classList.add('is-invalid');
                if (errorElement) {
                    errorElement.textContent = 'Invalid PhilHealth ID format. Use: XX-XXXXXXXXX-X (e.g., 02-027851766-8)';
                    errorElement.style.display = 'block';
                }
            }
        });
        
        // Clear validation on focus
        inputElement.addEventListener('focus', function(e) {
            if (errorElement) errorElement.style.display = 'none';
        });
    }

    /**
     * Get validation message for PhilHealth ID
     * 
     * @param {string} philhealthId - PhilHealth ID to validate
     * @returns {string|null} - Error message if invalid, null if valid
     */
    function getValidationMessage(philhealthId) {
        if (!philhealthId || philhealthId.trim().length === 0) {
            return null; // Empty is valid (not required field)
        }
        
        const cleanId = philhealthId.trim();
        
        // Check format
        if (!/^\d{2}-\d{9}-\d{1}$/.test(cleanId)) {
            return 'Invalid PhilHealth ID format. Use: XX-XXXXXXXXX-X (e.g., 02-027851766-8)';
        }
        
        // Check if all zeros
        if (cleanId.replace(/-/g, '') === '000000000000') {
            return 'PhilHealth ID cannot be all zeros';
        }
        
        return null; // Valid
    }

    // Export functions
    if (typeof module !== 'undefined' && module.exports) {
        // Node.js environment
        module.exports = {
            validate: validatePhilHealthId,
            format: formatPhilHealthId,
            setupFormatter: setupPhilHealthFormatter,
            setupValidation: setupPhilHealthValidation,
            getValidationMessage: getValidationMessage
        };
    } else {
        // Browser environment - attach to global object
        global.PhilHealthValidator = {
            validate: validatePhilHealthId,
            format: formatPhilHealthId,
            setupFormatter: setupPhilHealthFormatter,
            setupValidation: setupPhilHealthValidation,
            getValidationMessage: getValidationMessage
        };
        
        console.log('PhilHealth Validator loaded successfully');
    }

})(typeof window !== 'undefined' ? window : global);
