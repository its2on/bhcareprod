// Sign-up page functionality

// Simulated database for storing user registrations
let usersDatabase = [];

document.addEventListener('DOMContentLoaded', function() {
    // Multi-step form navigation
    const section1 = document.getElementById('section1');
    const section2 = document.getElementById('section2');
    const section3 = document.getElementById('section3');
    
    const step1Indicator = document.getElementById('step1Indicator');
    const step2Indicator = document.getElementById('step2Indicator');
    const step3Indicator = document.getElementById('step3Indicator');
    
    const progressBar = document.getElementById('registrationProgress');
    const progressText = document.getElementById('progressText');
    
    // Next and back buttons
    const nextToSecurity = document.getElementById('nextToSecurity');
    const backToPersonal = document.getElementById('backToPersonal');
    const nextToVerification = document.getElementById('nextToVerification');
    const backToSecurity = document.getElementById('backToSecurity');
    
    // Checkbox validation - updated with new IDs
    const privacyTerms = document.getElementById('privacyTerms');
    const residencyConfirm = document.getElementById('residencyConfirm');
    const signupButton = document.getElementById('signupButton');
    
    // Modal accept terms button
    const acceptTermsButton = document.getElementById('acceptTerms');
    
    // Success modal
    const registrationSuccessModal = document.getElementById('registrationSuccessModal');
    const successCloseBtn = document.getElementById('successCloseBtn');
    
    // File upload handling
    const fileInput = document.getElementById('residencyProofFile');
    const checkboxErrorContainer = document.getElementById('checkboxErrorContainer');
    const checkboxErrorMessage = document.getElementById('checkboxErrorMessage');
    const fileNameDisplay = document.getElementById('fileNameDisplay');
    
    // Password visibility toggles
    const togglePassword = document.getElementById('togglePassword');
    const toggleConfirmPassword = document.getElementById('toggleConfirmPassword');
    
    // Password fields
    const passwordInput = document.getElementById('password');
    const confirmPasswordInput = document.getElementById('confirmPassword');
    
    // Required form fields for validation
    const requiredFields = document.getElementById('signupForm') ? 
        document.getElementById('signupForm').querySelectorAll('input[required]') : [];
    
    // Password strength elements
    const passwordStrengthBar = document.getElementById('passwordStrength');
    const passwordStrengthText = document.querySelector('.password-strength-text');
    const passwordFeedback = document.getElementById('passwordFeedback');
    
    // Age field
    const ageInput = document.getElementById('age');
    
    // Birth date handling for guardian info
    const birthDateInput = document.getElementById('birthDate');
    const guardianInfoSection = document.getElementById('guardianInfoSection');
    const guardianFullNameInput = document.getElementById('guardianFullName');
    const guardianContactNumberInput = document.getElementById('guardianContactNumber');
    
    // Always check the privacy terms and residency confirmation checkboxes
    if (privacyTerms) {
        // Remove the auto-checking behavior
        privacyTerms.addEventListener('change', function() {
            validateFormSubmission();
        });
    }
    
    if (residencyConfirm) {
        // Remove the auto-checking behavior
        residencyConfirm.addEventListener('change', function() {
            validateFormSubmission();
        });
    }
    
    // Hide any server-side validation errors for the checkboxes
    document.querySelectorAll('[data-valmsg-for="Input.AgreeToTerms"], [data-valmsg-for="Input.ConfirmResidency"]').forEach(function(element) {
        element.style.display = 'none';
    });
    
    // Hide server validation errors on page load (guard if function is not defined)
    if (typeof hideServerValidationErrors === 'function') {
        hideServerValidationErrors();
    }
    
    // Function to check if both checkboxes are checked and file is uploaded
    function validateFormSubmission() {
        // Use the new checkbox IDs
        const privacyTermsChecked = privacyTerms && privacyTerms.checked;
        const residencyConfirmChecked = residencyConfirm && residencyConfirm.checked;
        const fileUploaded = fileInput && fileInput.files && fileInput.files.length > 0;
        
        // Track error messages
        let errorMessages = [];
        let isValid = true;
        
        // Show privacy terms checkbox error message
        if (privacyTerms) {
            const errorElement = document.getElementById('privacyTermsError');
            if (errorElement) {
                if (!privacyTermsChecked) {
                    errorElement.style.display = 'block';
                    if (!errorElement.classList.contains('fade-in')) {
                        errorElement.classList.add('fade-in');
                    }
                    // Add to error messages
                    errorMessages.push("You must agree to the Data Privacy Terms");
                    isValid = false;
                } else {
                    errorElement.style.display = 'none';
                    errorElement.classList.remove('fade-in');
                }
            }
            
            // Add validation styling with transition
            if (privacyTermsChecked) {
                privacyTerms.classList.remove('is-invalid');
                privacyTerms.classList.add('is-valid');
                // Add visual feedback for check
                const label = document.querySelector('label[for="privacyTerms"]');
                if (label) {
                    label.style.color = '#1E88E5';
                    label.style.fontWeight = '500';
                    label.style.transition = 'all 0.3s ease';
                }
            } else {
                privacyTerms.classList.add('is-invalid');
                privacyTerms.classList.remove('is-valid');
                // Remove visual feedback
                const label = document.querySelector('label[for="privacyTerms"]');
                if (label) {
                    label.style.color = '';
                    label.style.fontWeight = '';
                }
            }
        }
        
        // Show residency confirmation checkbox error message
        if (residencyConfirm) {
            const errorElement = document.getElementById('residencyConfirmError');
            if (errorElement) {
                if (!residencyConfirmChecked) {
                    errorElement.style.display = 'block';
                    if (!errorElement.classList.contains('fade-in')) {
                        errorElement.classList.add('fade-in');
                    }
                    // Add to error messages
                    errorMessages.push("You must confirm your residency");
                    isValid = false;
                } else {
                    errorElement.style.display = 'none';
                    errorElement.classList.remove('fade-in');
                }
            }
            
            // Add validation styling with transition
            if (residencyConfirmChecked) {
                residencyConfirm.classList.remove('is-invalid');
                residencyConfirm.classList.add('is-valid');
                // Add visual feedback for check
                const label = document.querySelector('label[for="residencyConfirm"]');
                if (label) {
                    label.style.color = '#1E88E5';
                    label.style.fontWeight = '500';
                    label.style.transition = 'all 0.3s ease';
                }
            } else {
                residencyConfirm.classList.add('is-invalid');
                residencyConfirm.classList.remove('is-valid');
                // Remove visual feedback
                const label = document.querySelector('label[for="residencyConfirm"]');
                if (label) {
                    label.style.color = '';
                    label.style.fontWeight = '';
                }
            }
        }
        
        // File upload error with animation
        if (fileInput) {
            const errorElement = document.getElementById('fileError');
            if (errorElement) {
                if (!fileUploaded) {
                    errorElement.style.display = 'block';
                    if (!errorElement.classList.contains('fade-in')) {
                        errorElement.classList.add('fade-in');
                    }
                    // Add to error messages
                    errorMessages.push("You must upload a residency proof document");
                    isValid = false;
                } else {
                    errorElement.style.display = 'none';
                    errorElement.classList.remove('fade-in');
                }
            }
            
            // Add validation styling
            if (fileUploaded) {
                fileInput.classList.remove('is-invalid');
                fileInput.classList.add('is-valid');
            } else {
                fileInput.classList.add('is-invalid');
                fileInput.classList.remove('is-valid');
            }
        }
        
        // Show combined error message if any errors
        if (checkboxErrorContainer && checkboxErrorMessage) {
            if (errorMessages.length > 0) {
                checkboxErrorContainer.classList.remove('d-none');
                checkboxErrorMessage.textContent = errorMessages.join(', ');
            } else {
                checkboxErrorContainer.classList.add('d-none');
            }
        }
        
        return isValid;
    }
    
    // Add event listeners for real-time validation
    if (document.getElementById('signupForm')) {
        // Validate required text/email/number inputs
        requiredFields.forEach(field => {
            if (field.type !== 'checkbox' && field.type !== 'file') {
                field.addEventListener('blur', function() {
                    if (!this.value) {
                        this.classList.add('is-invalid');
                    } else {
                        this.classList.remove('is-invalid');
                        this.classList.add('is-valid');
                    }
                    validateFormSubmission();
                });
                
                field.addEventListener('input', function() {
                    if (this.classList.contains('is-invalid')) {
                        if (this.value) {
                            this.classList.remove('is-invalid');
                            this.classList.add('is-valid');
                        }
                    }
                    validateFormSubmission();
                });
            }
        });
        
        // Add checkbox change listeners with animation - updated with new IDs
        if (privacyTerms) {
            privacyTerms.addEventListener('change', function() {
                validateFormSubmission();
                
                // Add subtle animation to checkbox label
                const label = document.querySelector('label[for="privacyTerms"]');
                if (label) {
                    label.style.transition = 'all 0.3s ease';
                    if (this.checked) {
                        label.style.color = '#1E88E5';
                        label.style.fontWeight = '500';
                    } else {
                        label.style.color = '';
                        label.style.fontWeight = '';
                    }
                }
                
                // Force enable submit button if both checkboxes are checked
                if (this.checked && residencyConfirm && residencyConfirm.checked) {
                    if (signupButton) {
                        signupButton.disabled = false;
                    }
                }
            });
        }
        
        if (residencyConfirm) {
            residencyConfirm.addEventListener('change', function() {
                validateFormSubmission();
                
                // Add subtle animation to checkbox label
                const label = document.querySelector('label[for="residencyConfirm"]');
                if (label) {
                    label.style.transition = 'all 0.3s ease';
                    if (this.checked) {
                        label.style.color = '#1E88E5';
                        label.style.fontWeight = '500';
                    } else {
                        label.style.color = '';
                        label.style.fontWeight = '';
                    }
                }
                
                // Force enable submit button if both checkboxes are checked
                if (this.checked && privacyTerms && privacyTerms.checked) {
                    if (signupButton) {
                        signupButton.disabled = false;
                    }
                }
            });
        }
        
        // Form submission handling with database integration
        document.getElementById('signupForm').addEventListener('submit', function(e) {
            // Validate the form
            const isValid = validateFormSubmission();
            
            if (!isValid) {
                e.preventDefault(); // Prevent submission only if validation fails
                
                // Add shake animation to form on invalid submission
                this.classList.add('shake-animation');
                setTimeout(() => {
                    this.classList.remove('shake-animation');
                }, 500);
                
                // Scroll to the first error
                const firstError = document.querySelector('.is-invalid');
                if (firstError) {
                    firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    firstError.focus();
                }
                
                return false;
            }
            
            // Ensure the checkbox values are correctly set before submission
            if (privacyTerms && privacyTerms.checked) {
                // Find all hidden inputs with name="Input.AgreeToTerms" and value="false"
                const hiddenAgreeInputs = document.querySelectorAll('input[type="hidden"][name="Input.AgreeToTerms"][value="false"]');
                hiddenAgreeInputs.forEach(input => {
                    // Remove these hidden inputs to prevent conflict with the checked value
                    input.parentNode.removeChild(input);
                });
            }
            
            if (residencyConfirm && residencyConfirm.checked) {
                // Find all hidden inputs with name="Input.ConfirmResidency" and value="false"
                const hiddenResidencyInputs = document.querySelectorAll('input[type="hidden"][name="Input.ConfirmResidency"][value="false"]');
                hiddenResidencyInputs.forEach(input => {
                    // Remove these hidden inputs to prevent conflict with the checked value
                    input.parentNode.removeChild(input);
                });
            }
            
            // Show the loading state UI
            const signupButton = document.getElementById('signupButton');
            if (signupButton) {
                signupButton.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span> Registering...';
                signupButton.disabled = false; // Important: Keep the button enabled so the form can submit
            }
            
            // Log the checkbox states before submission
            console.log('Submitting form with checkbox states:');
            console.log('Agreement to terms:', privacyTerms ? privacyTerms.checked : 'N/A');
            console.log('Residency confirmation:', residencyConfirm ? residencyConfirm.checked : 'N/A');
            
            // Let the form submit naturally to the server
            return true;
        });
    }
    
    if (nextToSecurity) {
        nextToSecurity.addEventListener('click', function() {
            // Validate personal info fields
            // Textual/number/date/email inputs (exclude radios; handle radios separately)
            const textRequiredFields = section1.querySelectorAll('input[required]:not([type="radio"])');
            let isValid = true;

            textRequiredFields.forEach(field => {
                const value = (field.value || '').trim();
                if (!value) {
                    field.classList.add('is-invalid');
                    isValid = false;
                } else {
                    field.classList.remove('is-invalid');
                }
            });

            // Validate Gender radio group
            const genderChecked = section1.querySelector('input[name="Input.Gender"]:checked');
            if (!genderChecked) {
                const genderRadios = section1.querySelectorAll('input[name="Input.Gender"]');
                genderRadios.forEach(r => r.classList.add('is-invalid'));
                isValid = false;
            } else {
                const genderRadios = section1.querySelectorAll('input[name="Input.Gender"]');
                genderRadios.forEach(r => r.classList.remove('is-invalid'));
            }

            if (!isValid) {
                // Find first invalid element and focus
                const firstInvalid = section1.querySelector('.is-invalid');
                if (firstInvalid) {
                    firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    try { firstInvalid.focus(); } catch {}
                }
                showValidationError('Please fill in all required fields in Step 1');
                return;
            }

            // Move to next section
            section1.classList.add('d-none');
            section2.classList.remove('d-none');
            section3.classList.add('d-none');

            // Update indicators
            step1Indicator.classList.remove('active');
            step2Indicator.classList.add('active');
            step3Indicator.classList.remove('active');

            // Update progress bar
            progressBar.style.width = '33%';
            progressBar.setAttribute('aria-valuenow', '33');
            progressText.textContent = 'Step 2: Account Security';
        });
    }
    
    if (backToPersonal) {
        backToPersonal.addEventListener('click', function() {
            section1.classList.remove('d-none');
            section2.classList.add('d-none');
            section3.classList.add('d-none');
            
            step1Indicator.classList.add('active');
            step2Indicator.classList.remove('active');
            step3Indicator.classList.remove('active');
            
            progressBar.style.width = '0%';
            progressBar.setAttribute('aria-valuenow', '0');
            progressText.textContent = 'Step 1: Personal Information';
        });
    }
    
    if (nextToVerification) {
        nextToVerification.addEventListener('click', function() {
            // Validate password fields
            const password = document.getElementById('password');
            const confirmPassword = document.getElementById('confirmPassword');
            
            if (!password.value) {
                password.classList.add('is-invalid');
                showValidationError('Please enter a password');
                return;
            }
            
            if (!confirmPassword.value) {
                confirmPassword.classList.add('is-invalid');
                showValidationError('Please confirm your password');
                return;
            }
            
            if (password.value !== confirmPassword.value) {
                password.classList.add('is-invalid');
                confirmPassword.classList.add('is-invalid');
                showValidationError('Passwords do not match');
                return;
            }
            
            // Check password strength
            const passwordStrength = getPasswordStrength(password.value);
            if (passwordStrength.score < 2) {
                if (!confirm('Your password is weak. Are you sure you want to continue?')) {
                    return;
                }
            }
            
            // Move to next section
            section1.classList.add('d-none');
            section2.classList.add('d-none');
            section3.classList.remove('d-none');
            
            // Update indicators
            step1Indicator.classList.remove('active');
            step2Indicator.classList.remove('active');
            step3Indicator.classList.add('active');
            
            // Update progress bar
            progressBar.style.width = '66%';
            progressBar.setAttribute('aria-valuenow', '66');
            progressText.textContent = 'Step 3: Verification';
        });
    }
    
    if (backToSecurity) {
        backToSecurity.addEventListener('click', function() {
            section1.classList.add('d-none');
            section2.classList.remove('d-none');
            section3.classList.add('d-none');
            
            step1Indicator.classList.remove('active');
            step2Indicator.classList.add('active');
            step3Indicator.classList.remove('active');
            
            progressBar.style.width = '33%';
            progressBar.setAttribute('aria-valuenow', '33');
            progressText.textContent = 'Step 2: Account Security';
        });
    }
    
    // Password toggle functionality
    if (togglePassword && passwordInput) {
        togglePassword.addEventListener('click', function() {
            togglePasswordVisibility(passwordInput, this);
        });
    }
    
    if (toggleConfirmPassword && confirmPasswordInput) {
        toggleConfirmPassword.addEventListener('click', function() {
            togglePasswordVisibility(confirmPasswordInput, this);
        });
    }
    
    // Password strength checking
    if (passwordInput) {
        passwordInput.addEventListener('input', function() {
            const password = this.value;
            
            // Check requirements for responsive UI
            const lengthRequirement = document.getElementById('length');
            const uppercaseRequirement = document.getElementById('uppercase');
            const lowercaseRequirement = document.getElementById('lowercase');
            const numberRequirement = document.getElementById('number');
            const specialRequirement = document.getElementById('special');
            
            if (lengthRequirement) {
                // Length check
                const hasLength = password.length >= 8;
                updateRequirement(lengthRequirement, hasLength, 'At least 8 characters');
            }
            
            if (uppercaseRequirement) {
                // Uppercase check
                const hasUppercase = /[A-Z]/.test(password);
                updateRequirement(uppercaseRequirement, hasUppercase, 'At least 1 uppercase letter');
            }
            
            if (lowercaseRequirement) {
                // Lowercase check
                const hasLowercase = /[a-z]/.test(password);
                updateRequirement(lowercaseRequirement, hasLowercase, 'At least 1 lowercase letter');
            }
            
            if (numberRequirement) {
                // Number check
                const hasNumber = /[0-9]/.test(password);
                updateRequirement(numberRequirement, hasNumber, 'At least 1 number');
            }
            
            if (specialRequirement) {
                // Special character check
                const hasSpecial = /[^A-Za-z0-9]/.test(password);
                updateRequirement(specialRequirement, hasSpecial, 'At least 1 special character');
            }
            
            // Calculate password strength
            const strength = getPasswordStrength(password);
            updatePasswordStrengthIndicator(strength);
            
            // Validate confirm password match
            if (confirmPasswordInput && confirmPasswordInput.value) {
                validatePasswordMatch();
            }
        });
    }
    
    // Password confirmation check
    if (confirmPasswordInput && passwordInput) {
        confirmPasswordInput.addEventListener('input', validatePasswordMatch);
    }
    
    // Function to validate password match
    function validatePasswordMatch() {
        const password = passwordInput.value;
        const confirmPassword = confirmPasswordInput.value;
        
        const passwordMatch = document.getElementById('passwordMatch');
        const passwordMismatch = document.getElementById('passwordMismatch');
        
        if (!confirmPassword) {
            // If empty, hide both messages
            if (passwordMatch) passwordMatch.classList.add('d-none');
            if (passwordMismatch) passwordMismatch.classList.add('d-none');
            confirmPasswordInput.classList.remove('is-valid');
            confirmPasswordInput.classList.remove('is-invalid');
        } else if (password === confirmPassword) {
            if (passwordMatch) passwordMatch.classList.remove('d-none');
            if (passwordMismatch) passwordMismatch.classList.add('d-none');
            confirmPasswordInput.classList.remove('is-invalid');
            confirmPasswordInput.classList.add('is-valid');
        } else {
            if (passwordMatch) passwordMatch.classList.add('d-none');
            if (passwordMismatch) passwordMismatch.classList.remove('d-none');
            confirmPasswordInput.classList.add('is-invalid');
            confirmPasswordInput.classList.remove('is-valid');
        }
    }
    
    // Helper functions
    
    // Show validation error message
    function showValidationError(message) {
        // Remove any existing error alert
        const existingAlert = document.getElementById('validationErrorAlert');
        if (existingAlert) {
            existingAlert.remove();
        }
        
        // Create new error alert
        const alertDiv = document.createElement('div');
        alertDiv.id = 'validationErrorAlert';
        alertDiv.className = 'alert alert-danger alert-dismissible fade show mt-3';
        alertDiv.setAttribute('role', 'alert');
        alertDiv.innerHTML = `
            <i class="fas fa-exclamation-triangle me-2"></i>
            <strong>Validation Error:</strong> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;
        
        // Insert at the top of the current visible section
        const visibleSection = document.querySelector('.section:not(.d-none)');
        if (visibleSection) {
            visibleSection.insertBefore(alertDiv, visibleSection.firstChild);
            
            // Scroll to the alert
            alertDiv.scrollIntoView({ behavior: 'smooth', block: 'start' });
            
            // Auto-hide after 5 seconds
            setTimeout(() => {
                if (alertDiv && alertDiv.parentNode) {
                    alertDiv.classList.remove('show');
                    setTimeout(() => alertDiv.remove(), 150);
                }
            }, 5000);
        }
    }
    
    // Toggle password visibility
    function togglePasswordVisibility(inputElement, buttonElement) {
        const type = inputElement.getAttribute('type') === 'password' ? 'text' : 'password';
        inputElement.setAttribute('type', type);
        
        const icon = buttonElement.querySelector('i');
        icon.classList.toggle('fa-eye');
        icon.classList.toggle('fa-eye-slash');
        
        // Toggle active class for styling
        buttonElement.classList.toggle('active');
    }
    
    // Update requirement item with check or cross icon
    function updateRequirement(element, isValid, text) {
        if (isValid) {
            element.innerHTML = `<i class="fas fa-check text-success me-1"></i> ${text}`;
            element.classList.add('valid');
            element.classList.remove('invalid');
        } else {
            element.innerHTML = `<i class="fas fa-times text-danger me-1"></i> ${text}`;
            element.classList.add('invalid');
            element.classList.remove('valid');
        }
    }
    
    // Password strength evaluation
    function getPasswordStrength(password) {
        if (!password) {
            return { score: 0, feedback: { warning: "No password provided", suggestions: ["Enter a password"] } };
        }
        
        // Use zxcvbn library if available
        if (typeof zxcvbn === 'function') {
            const result = zxcvbn(password);
            
            // zxcvbn scores from 0-4, which matches our scale
            return {
                score: result.score,
                feedback: {
                    warning: result.feedback.warning || "",
                    suggestions: result.feedback.suggestions || []
                }
            };
        }
        
        // Fallback to basic implementation if zxcvbn is not available
        let score = 0;
        const feedback = { warning: "", suggestions: [] };
        
        // Length check (0-2 points)
        if (password.length >= 8) score += 1;
        if (password.length >= 12) score += 1;
        
        // Character variety checks
        const hasUppercase = /[A-Z]/.test(password);
        const hasLowercase = /[a-z]/.test(password);
        const hasNumber = /[0-9]/.test(password);
        const hasSpecial = /[^A-Za-z0-9]/.test(password);
        
        // Award points for character variety
        if (hasUppercase) score += 1;
        if (hasLowercase) score += 1;
        if (hasNumber) score += 1;
        if (hasSpecial) score += 1;
        
        // Check for common patterns
        const commonPatterns = [
            /^123456/, /password/i, /qwerty/i, /admin/i, 
            /welcome/i, /letmein/i, /abc123/i, /monkey/i,
            /^12345/, /football/i, /baseball/i, /dragon/i
        ];
        
        const hasCommonPattern = commonPatterns.some(pattern => pattern.test(password));
        if (hasCommonPattern) {
            score = Math.max(0, score - 2);
            feedback.warning = "Common password pattern detected";
            feedback.suggestions.push("Avoid using common words or patterns");
        }
        
        // Generate feedback based on score
        if (score <= 2) {
            feedback.warning = "This password is too weak";
            if (!feedback.suggestions.length) {
                feedback.suggestions.push("Add more variety with uppercase, numbers, and special characters");
                feedback.suggestions.push("Make your password longer");
            }
        } else if (score <= 4) {
            feedback.warning = "This password could be stronger";
            if (!feedback.suggestions.length) {
                feedback.suggestions.push("Consider adding more variety or length");
            }
        }
        
        // Normalize score to 0-4 range
        score = Math.min(4, score);
        
        return { score, feedback };
    }
    
    // Update password strength indicator
    function updatePasswordStrengthIndicator(strengthResult) {
        if (!passwordStrengthBar || !passwordStrengthText) return;
        
        const score = strengthResult.score;
        const feedback = strengthResult.feedback;
        const strengthPercentage = (score / 4) * 100;
        
        // Update progress bar
        passwordStrengthBar.style.width = `${strengthPercentage}%`;
        passwordStrengthBar.setAttribute('aria-valuenow', strengthPercentage);
        
        // Update color and text based on score
        if (score <= 1) {
            passwordStrengthBar.className = 'progress-bar bg-danger';
            passwordStrengthText.textContent = 'Very Weak';
            passwordStrengthText.className = 'password-strength-text text-danger';
        } else if (score === 2) {
            passwordStrengthBar.className = 'progress-bar bg-warning';
            passwordStrengthText.textContent = 'Weak';
            passwordStrengthText.className = 'password-strength-text text-warning';
        } else if (score === 3) {
            passwordStrengthBar.className = 'progress-bar bg-info';
            passwordStrengthText.textContent = 'Good';
            passwordStrengthText.className = 'password-strength-text text-info';
        } else {
            passwordStrengthBar.className = 'progress-bar bg-success';
            passwordStrengthText.textContent = 'Strong';
            passwordStrengthText.className = 'password-strength-text text-success';
        }
        
        // Display feedback if available
        if (passwordFeedback) {
            if (feedback.warning || feedback.suggestions.length > 0) {
                let feedbackHtml = '';
                
                if (feedback.warning) {
                    feedbackHtml += `<div class="alert alert-warning py-1 mb-2">${feedback.warning}</div>`;
                }
                
                if (feedback.suggestions.length > 0) {
                    feedbackHtml += '<ul class="mb-0 ps-3">';
                    feedback.suggestions.forEach(suggestion => {
                        feedbackHtml += `<li>${suggestion}</li>`;
                    });
                    feedbackHtml += '</ul>';
                }
                
                passwordFeedback.innerHTML = feedbackHtml;
                passwordFeedback.classList.remove('d-none');
            } else {
                passwordFeedback.classList.add('d-none');
            }
        }
    }
    
    // Accept terms button event listener
    if (acceptTermsButton && privacyTerms) {
        acceptTermsButton.addEventListener('click', function() {
            privacyTerms.checked = true;
            
            // Add visual feedback
            const label = document.querySelector('label[for="privacyTerms"]');
            if (label) {
                label.style.color = '#1E88E5';
                label.style.fontWeight = '500';
                label.style.transition = 'all 0.3s ease';
            }
            
            // Remove any error messages
            const errorElement = document.getElementById('privacyTermsError');
            if (errorElement) {
                errorElement.style.display = 'none';
            }
            
            // Add valid class to the checkbox
            privacyTerms.classList.remove('is-invalid');
            privacyTerms.classList.add('is-valid');
            
            // Validate the form
            validateFormSubmission();
        });
    }
    
    // Run initial validation on page load
    validateFormSubmission();
    
    // Add CSS for animations
    const style = document.createElement('style');
    style.textContent = `
        .shake-animation {
            animation: shake 0.5s cubic-bezier(.36,.07,.19,.97) both;
        }
        @keyframes shake {
            10%, 90% { transform: translate3d(-1px, 0, 0); }
            20%, 80% { transform: translate3d(2px, 0, 0); }
            30%, 50%, 70% { transform: translate3d(-3px, 0, 0); }
            40%, 60% { transform: translate3d(3px, 0, 0); }
        }
        .pulse-animation {
            animation: pulse 0.3s ease;
        }
        @keyframes pulse {
            0% { transform: scale(1); }
            50% { transform: scale(1.1); }
            100% { transform: scale(1); }
        }
    `;
    document.head.appendChild(style);
    
    // Function to save users to localStorage
    function saveUsersToLocalStorage() {
        localStorage.setItem('bhcUsers', JSON.stringify(usersDatabase));
    }
    
    // Function to load users from localStorage
    function loadUsersFromLocalStorage() {
        const storedUsers = localStorage.getItem('bhcUsers');
        if (storedUsers) {
            usersDatabase = JSON.parse(storedUsers);
        }
    }
    
    // Load any existing users on page load
    loadUsersFromLocalStorage();
    
    if (birthDateInput) {
        birthDateInput.addEventListener('change', function() {
            const birthDate = new Date(this.value);
            const today = new Date();
            
            // Calculate age
            let age = today.getFullYear() - birthDate.getFullYear();
            const monthDiff = today.getMonth() - birthDate.getMonth();
            
            // Adjust age if birth month hasn't occurred yet this year
            if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
                age--;
            }
            
            console.log('Calculated age:', age);
            
            // Show/hide guardian information based on age
            if (guardianInfoSection) {
                if (age < 18) {
                    // User is under 18, show guardian section
                    guardianInfoSection.classList.remove('d-none');
                    
                    // Make guardian fields required
                    if (guardianFullNameInput) {
                        guardianFullNameInput.setAttribute('required', 'required');
                        document.querySelectorAll('.guardian-required').forEach(span => {
                            span.classList.remove('d-none');
                        });
                    }
                    
                    if (guardianContactNumberInput) {
                        guardianContactNumberInput.setAttribute('required', 'required');
                    }
                } else {
                    // User is 18 or older, hide guardian section
                    guardianInfoSection.classList.add('d-none');
                    
                    // Remove required attribute
                    if (guardianFullNameInput) {
                        guardianFullNameInput.removeAttribute('required');
                        guardianFullNameInput.value = ''; // Clear the value
                        document.querySelectorAll('.guardian-required').forEach(span => {
                            span.classList.add('d-none');
                        });
                    }
                    
                    if (guardianContactNumberInput) {
                        guardianContactNumberInput.removeAttribute('required');
                        guardianContactNumberInput.value = ''; // Clear the value
                    }
                }
            }
        });
        
        // Trigger the change event to initialize the form state
        birthDateInput.dispatchEvent(new Event('change'));
    }
});