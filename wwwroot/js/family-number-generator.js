/**
 * Family Number Generator JavaScript Module
 * Provides atomic, thread-safe family number generation for BHCARE application
 */

class FamilyNumberGenerator {
    constructor(baseUrl = '') {
        this.baseUrl = baseUrl;
        this.isGenerating = false;
    }

    /**
     * Generates a family number using the atomic API
     * @param {string} lastName - Patient's last name
     * @param {string} healthFacility - Health facility name (optional)
     * @param {string} patientCategory - Patient category (optional)
     * @returns {Promise<Object>} Generation result
     */
    async generateFamilyNumber(lastName, healthFacility = null, patientCategory = null) {
        if (this.isGenerating) {
            throw new Error('Family number generation already in progress');
        }

        this.isGenerating = true;
        
        try {
            console.log('=== FAMILY NUMBER GENERATION STARTED ===');
            console.log('Parameters:', { lastName, healthFacility, patientCategory });

            const requestBody = {
                LastName: lastName,
                HealthFacility: healthFacility,
                PatientCategory: patientCategory
            };

            console.log('Request body:', requestBody);

            const response = await fetch(`${this.baseUrl}/api/FamilyNumber/generate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify(requestBody)
            });

            console.log('Response status:', response.status);
            console.log('Response ok:', response.ok);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Error response:', errorText);
                throw new Error(`Server error: ${response.status} - ${errorText}`);
            }

            const result = await response.json();
            console.log('Response result:', result);

            if (result.success) {
                console.log('=== FAMILY NUMBER GENERATION COMPLETED SUCCESSFULLY ===');
                console.log('Generated family number:', result.familyNumber);
                console.log('Prefix:', result.prefix);
                console.log('Sequence number:', result.sequenceNumber);
                
                return {
                    success: true,
                    familyNumber: result.familyNumber,
                    prefix: result.prefix,
                    sequenceNumber: result.sequenceNumber,
                    isPreexisting: result.isPreexisting || false
                };
            } else {
                console.error('Generation failed:', result.error);
                return {
                    success: false,
                    error: result.error || 'Unknown error occurred'
                };
            }
        } catch (error) {
            console.error('Family number generation error:', error);
            return {
                success: false,
                error: error.message || 'Network error occurred'
            };
        } finally {
            this.isGenerating = false;
        }
    }

    /**
     * Gets the next family number for a specific prefix
     * @param {string} prefix - The prefix to get the next number for
     * @returns {Promise<string>} Next family number
     */
    async getNextFamilyNumber(prefix) {
        try {
            console.log('Getting next family number for prefix:', prefix);

            const response = await fetch(`${this.baseUrl}/api/FamilyNumber/next/${encodeURIComponent(prefix)}`, {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                throw new Error(`Server error: ${response.status}`);
            }

            const familyNumber = await response.text();
            console.log('Next family number:', familyNumber);
            return familyNumber;
        } catch (error) {
            console.error('Error getting next family number:', error);
            throw error;
        }
    }

    /**
     * Validates a family number format and existence
     * @param {string} familyNumber - Family number to validate
     * @returns {Promise<boolean>} Validation result
     */
    async validateFamilyNumber(familyNumber) {
        try {
            console.log('Validating family number:', familyNumber);

            const response = await fetch(`${this.baseUrl}/api/FamilyNumber/validate/${encodeURIComponent(familyNumber)}`, {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                throw new Error(`Server error: ${response.status}`);
            }

            const isValid = await response.json();
            console.log('Validation result:', isValid);
            return isValid;
        } catch (error) {
            console.error('Error validating family number:', error);
            throw error;
        }
    }

    /**
     * Shows success message using SweetAlert2
     * @param {string} familyNumber - Generated family number
     * @param {Object} options - Additional options
     */
    showSuccessMessage(familyNumber, options = {}) {
        const defaultOptions = {
            title: 'Success!',
            text: `Family number generated successfully: ${familyNumber}`,
            icon: 'success',
            confirmButtonText: 'OK',
            confirmButtonColor: '#28a745',
            timer: 3000,
            timerProgressBar: true
        };

        const finalOptions = { ...defaultOptions, ...options };

        if (typeof Swal !== 'undefined') {
            Swal.fire(finalOptions);
        } else {
            alert(finalOptions.text);
        }
    }

    /**
     * Shows error message using SweetAlert2
     * @param {string} error - Error message
     * @param {Object} options - Additional options
     */
    showErrorMessage(error, options = {}) {
        const defaultOptions = {
            title: 'Error',
            text: error,
            icon: 'error',
            confirmButtonText: 'OK',
            confirmButtonColor: '#dc3545'
        };

        const finalOptions = { ...defaultOptions, ...options };

        if (typeof Swal !== 'undefined') {
            Swal.fire(finalOptions);
        } else {
            alert(error);
        }
    }

    /**
     * Shows info message using SweetAlert2
     * @param {string} message - Info message
     * @param {Object} options - Additional options
     */
    showInfoMessage(message, options = {}) {
        const defaultOptions = {
            title: 'Family Number Found',
            text: message,
            icon: 'info',
            confirmButtonText: 'OK',
            confirmButtonColor: '#17a2b8'
        };

        const finalOptions = { ...defaultOptions, ...options };

        if (typeof Swal !== 'undefined') {
            Swal.fire(finalOptions);
        } else {
            alert(message);
        }
    }
}

// Usage example for the NCD Risk Assessment form
document.addEventListener('DOMContentLoaded', function() {
    // Initialize the family number generator
    const familyNumberGen = new FamilyNumberGenerator();

    // Get the generate button and form elements
    const generateBtn = document.getElementById('generate-family-no');
    const familyNoInput = document.getElementById('family-no');
    const lastNameInput = document.getElementById('last-name');
    const healthFacilityInput = document.getElementById('health-facility');

    if (generateBtn && familyNoInput && lastNameInput) {
        generateBtn.addEventListener('click', async function() {
            console.log('=== GENERATE FAMILY NUMBER BUTTON CLICKED ===');
            
            const lastName = lastNameInput.value.trim();
            const healthFacility = healthFacilityInput ? healthFacilityInput.value.trim() : null;

            if (!lastName) {
                familyNumberGen.showErrorMessage('Please enter your last name first to generate a family number.');
                return;
            }

            // Disable button during generation
            generateBtn.disabled = true;
            generateBtn.textContent = 'Generating...';

            try {
                const result = await familyNumberGen.generateFamilyNumber(
                    lastName, 
                    healthFacility, 
                    null // Don't override prefix with PatientCategory - use last name for first-come-first-serve
                );

                if (result.success) {
                    // Update the form field
                    familyNoInput.value = result.familyNumber;
                    
                    // Update ID No. field if it exists
                    const idNoInput = document.getElementById('id-no');
                    if (idNoInput) {
                        idNoInput.value = result.familyNumber;
                    }

                    if (result.isPreexisting) {
                        familyNumberGen.showInfoMessage(`You already have a family number: ${result.familyNumber}`);
                    } else {
                        familyNumberGen.showSuccessMessage(result.familyNumber);
                    }
                } else {
                    familyNumberGen.showErrorMessage(result.error);
                }
            } catch (error) {
                console.error('Family number generation error:', error);
                familyNumberGen.showErrorMessage('Error generating family number. Please try again.');
            } finally {
                // Re-enable button
                generateBtn.disabled = false;
                generateBtn.textContent = 'Generate';
            }
        });
    }
});

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FamilyNumberGenerator;
}
