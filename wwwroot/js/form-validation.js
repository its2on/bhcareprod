/**
 * Enhanced Form Validation for Barangay Health Center signup
 * Provides stricter validation for contact numbers and name fields
 */

document.addEventListener('DOMContentLoaded', function() {
    // Get all form elements that need enhanced validation
    const form = document.getElementById('signupForm');
    const contactNumberInput = document.querySelector('input[name="Input.ContactNumber"]');
    const guardianContactNumberInput = document.querySelector('input[name="Input.GuardianContactNumber"]');
    const firstNameInput = document.querySelector('input[name="Input.FirstName"]');
    const middleNameInput = document.querySelector('input[name="Input.MiddleName"]');
    const lastNameInput = document.querySelector('input[name="Input.LastName"]');
    const suffixInput = document.querySelector('input[name="Input.Suffix"]');
    const usernameInput = document.querySelector('input[name="Input.Username"]');
    
    // Regular expressions for validation
    const patterns = {
        // Allows only 09XXXXXXXXX or +639XXXXXXXXX formats
        contactNumber: /^(09\d{9}|\+639\d{9})$/,
        
        // Allows letters, spaces, hyphens, apostrophes, periods, and digits 1-9
        name: /^[A-Za-z\s\-'.1-9]+$/,
        nameCharFilter: /[^A-Za-z\s\-'.1-9]/g,
        nameDigitRepeat: /([1-9]).*\1/,
        
        // Checks for repeating characters (same character repeated 5+ times)
        repeatingChars: /(.)\1{4,}/,
        
        // Username validation - allow any character combination
        username: /^[a-zA-Z0-9!@#$%^&*()_+\-=\[\]{};':\"\\|,.<>\/?]{3,15}$/
    };
    
    // Error messages
    const errorMessages = {
        contactNumber: 'Contact number must be in format 09XXXXXXXXX or +639XXXXXXXXX',
        nameFormat: 'Only letters, spaces, hyphens, apostrophes, periods, and digits 1-9 are allowed',
        nameDigitRepeat: 'Each digit 1-9 can only appear once in the name',
        nameRepeating: 'Name contains too many repeating characters',
        username: 'Username must be between 3-15 characters and can contain letters, numbers, and special characters'
    };
    
    // Apply username validation
    if (usernameInput) {
        // Validate on blur (when user leaves the field)
        usernameInput.addEventListener('blur', function(e) {
            validateUsername(this);
        });
    }

    // Apply contact number validation to both contact number fields
    [contactNumberInput, guardianContactNumberInput].forEach(input => {
        if (input) {
            // Format and validate on input
            input.addEventListener('input', function(e) {
                formatContactNumber(this);
            });
            
            // Validate on blur (when user leaves the field)
            input.addEventListener('blur', function(e) {
                validateContactNumber(this);
            });
        }
    });
    
    // Apply name validation to all name fields
    [firstNameInput, middleNameInput, lastNameInput, suffixInput].forEach(input => {
        if (input) {
            // Filter input in real-time
            input.addEventListener('input', function(e) {
                filterNameInput(this);
            });
            
            // Validate on blur
            input.addEventListener('blur', function(e) {
                validateNameField(this);
            });
        }
    });
    
    // Form submission validation
    if (form) {
        form.addEventListener('submit', function(e) {
            let isValid = true;
            
            // Validate username
            if (usernameInput && usernameInput.value.trim()) {
                if (!validateUsername(usernameInput)) {
                    isValid = false;
                }
            }
            
            // Validate contact number fields
            [contactNumberInput, guardianContactNumberInput].forEach(input => {
                if (input && input.value.trim()) {
                    if (!validateContactNumber(input)) {
                        isValid = false;
                    }
                }
            });
            
            // Validate name fields
            [firstNameInput, middleNameInput, lastNameInput, suffixInput].forEach(input => {
                if (input && input.value.trim()) {
                    if (!validateNameField(input)) {
                        isValid = false;
                    }
                }
            });
            
            // Prevent form submission if validation fails
            if (!isValid) {
                e.preventDefault();
                
                // Scroll to the first error for better UX
                const firstErrorField = document.querySelector('.is-invalid');
                if (firstErrorField) {
                    firstErrorField.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    firstErrorField.focus();
                }
            }
        });
    }
    
    /**
     * Format the contact number as the user types
     * @param {HTMLInputElement} input - The contact number input field
     */
    function formatContactNumber(input) {
        let value = input.value.trim();
        
        // Remove non-digit characters, except + at beginning
        if (value.startsWith('+')) {
            value = '+' + value.substring(1).replace(/[^\d]/g, '');
        } else {
            value = value.replace(/[^\d]/g, '');
            
            // Automatically format to 09 prefix if user starts typing digits
            if (value && !value.startsWith('09') && !value.startsWith('+639')) {
                if (value.startsWith('9') && value.length <= 10) {
                    // If user starts with 9, prepend 0
                    value = '0' + value;
                } else if (!value.startsWith('0') && value.length > 0) {
                    // If user starts with another digit, prepend 09
                    value = '09' + value;
                }
            }
        }
        
        // Enforce length limits based on format
        if (value.startsWith('+639')) {
            value = value.substring(0, 13); // +639 + 9 digits
        } else if (value.startsWith('09')) {
            value = value.substring(0, 11); // 09 + 9 digits
        }
        
        // Update the input value
        input.value = value;
        
        // Real-time validation feedback
        if (value) {
            const isValid = patterns.contactNumber.test(value);
            updateValidationState(input, isValid, isValid ? '' : errorMessages.contactNumber);
        } else {
            // Clear validation state if field is empty
            clearValidationState(input);
        }
    }
    
    /**
     * Validate the contact number format
     * @param {HTMLInputElement} input - The contact number input field
     * @returns {boolean} - Whether the number is valid
     */
    function validateContactNumber(input) {
        const value = input.value.trim();
        
        // Skip validation if the field is empty and not required
        if (!value) {
            if (input.hasAttribute('required')) {
                updateValidationState(input, false, 'Contact number is required');
                return false;
            }
            clearValidationState(input);
            return true;
        }
        
        // Check against the pattern
        const isValid = patterns.contactNumber.test(value);
        updateValidationState(input, isValid, isValid ? '' : errorMessages.contactNumber);
        
        return isValid;
    }
    
    /**
     * Filter name input to only allow valid characters
     * @param {HTMLInputElement} input - The name input field
     */
    function filterNameInput(input) {
        const value = input.value;
        let filteredValue = value;
        
        // Remove characters that aren't letters, spaces, hyphens or apostrophes
        if (patterns.nameCharFilter.test(value)) {
            filteredValue = value.replace(patterns.nameCharFilter, '');
            
            // Only update if we made changes to avoid cursor jumping
            if (filteredValue !== value) {
                input.value = filteredValue;
            }
        }
        
        // Real-time validation for repeating characters
        if (patterns.repeatingChars.test(filteredValue)) {
            updateValidationState(input, false, errorMessages.nameRepeating);
        } else if (patterns.nameDigitRepeat.test(filteredValue)) {
            updateValidationState(input, false, errorMessages.nameDigitRepeat);
        } else if (filteredValue) {
            clearValidationState(input);
        }
    }
    
    /**
     * Validate name fields format
     * @param {HTMLInputElement} input - The name input field
     * @returns {boolean} - Whether the name is valid
     */
    function validateNameField(input) {
        const value = input.value.trim();
        
        // Skip validation if the field is empty and not required
        if (!value) {
            if (input.hasAttribute('required')) {
                updateValidationState(input, false, 'This field is required');
                return false;
            }
            clearValidationState(input);
            return true;
        }
        
        // Check format (letters, spaces, hyphens, apostrophes, periods, digits 1-9)
        if (!patterns.name.test(value)) {
            updateValidationState(input, false, errorMessages.nameFormat);
            return false;
        }

        // Ensure digits 1-9 are not repeated
        if (patterns.nameDigitRepeat.test(value)) {
            updateValidationState(input, false, errorMessages.nameDigitRepeat);
            return false;
        }
        
        // Check for repeating characters (like "aaaaaa")
        if (patterns.repeatingChars.test(value)) {
            updateValidationState(input, false, errorMessages.nameRepeating);
            return false;
        }
        
        // Check for minimum length (at least 2 characters)
        if (value.length < 2) {
            updateValidationState(input, false, 'Name must be at least 2 characters');
            return false;
        }
        
        // Successful validation
        updateValidationState(input, true, '');
        return true;
    }
    
    /**
     * Validate the username format
     * @param {HTMLInputElement} input - The username input field
     * @returns {boolean} - Whether the username is valid
     */
    function validateUsername(input) {
        const value = input.value.trim();
        
        // Skip validation if the field is empty and not required
        if (!value) {
            if (input.hasAttribute('required')) {
                updateValidationState(input, false, 'Username is required');
                return false;
            }
            clearValidationState(input);
            return true;
        }
        
        // Check length (between 3 and 15 characters)
        if (value.length < 3 || value.length > 15) {
            updateValidationState(input, false, 'Username must be between 3 and 15 characters');
            return false;
        }
        
        // Check against the pattern
        const isValid = patterns.username.test(value);
        updateValidationState(input, isValid, isValid ? '' : errorMessages.username);
        
        return isValid;
    }
    
    /**
     * Update the validation state of an input field
     * @param {HTMLInputElement} input - The input field
     * @param {boolean} isValid - Whether the input is valid
     * @param {string} errorMessage - The error message to display
     */
    function updateValidationState(input, isValid, errorMessage) {
        // Find or create error container
        let errorContainer = findErrorContainer(input);
        
        if (isValid) {
            // Valid state
            input.classList.remove('is-invalid');
            input.classList.add('is-valid');
            
            if (errorContainer) {
                errorContainer.textContent = '';
                errorContainer.style.display = 'none';
            }
        } else {
            // Invalid state
            input.classList.add('is-invalid');
            input.classList.remove('is-valid');
            
            // Create error container if it doesn't exist
            if (!errorContainer) {
                errorContainer = document.createElement('div');
                errorContainer.className = 'text-danger validation-message';
                input.parentNode.appendChild(errorContainer);
            }
            
            // Show error message
            errorContainer.textContent = errorMessage;
            errorContainer.style.display = 'block';
        }
    }
    
    /**
     * Clear validation state of an input field
     * @param {HTMLInputElement} input - The input field
     */
    function clearValidationState(input) {
        input.classList.remove('is-invalid');
        input.classList.remove('is-valid');
        
        const errorContainer = findErrorContainer(input);
        if (errorContainer) {
            errorContainer.textContent = '';
            errorContainer.style.display = 'none';
        }
    }
    
    /**
     * Find the error container for an input
     * @param {HTMLInputElement} input - The input field
     * @returns {HTMLElement|null} - The error container or null if not found
     */
    function findErrorContainer(input) {
        // First try to find an existing error container that is either a sibling or child of parent
        let errorContainer = input.parentNode.querySelector('.text-danger.validation-message');
        
        // If not found, look for the ASP.NET validation span
        if (!errorContainer) {
            errorContainer = input.parentNode.querySelector('span[data-valmsg-for]');
        }
        
        // Also look for any .invalid-feedback elements
        if (!errorContainer) {
            errorContainer = input.parentNode.querySelector('.invalid-feedback');
        }
        
        return errorContainer;
    }
    
    // Initialize validation for prefilled fields
    function initializeValidation() {
        // Validate contact number fields if they have values
        [contactNumberInput, guardianContactNumberInput].forEach(input => {
            if (input && input.value.trim()) {
                validateContactNumber(input);
            }
        });
        
        // Validate name fields if they have values
        [firstNameInput, middleNameInput, lastNameInput, suffixInput].forEach(input => {
            if (input && input.value.trim()) {
                validateNameField(input);
            }
        });
    }
    
    // Run initial validation for prefilled fields
    initializeValidation();
}); 