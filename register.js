/**
 * Barangay Health Center - Registration Form Debugging Script
 * 
 * This script helps debug issues with the user registration form.
 * Add this to your register.html or register.cshtml page to monitor form submissions.
 */

(function() {
    // Wait for DOM content to load
    document.addEventListener('DOMContentLoaded', function() {
        console.log('Registration form debugging script loaded');
        
        // Find the registration form
        const form = document.querySelector('form[action*="Register"]') || 
                     document.querySelector('form[id="registrationForm"]') ||
                     document.querySelector('form'); // Last resort: get first form
        
        // Exit if no form found
        if (!form) {
            console.error('No registration form found on this page');
            return;
        }
        
        console.log('Found registration form:', form);
        console.log('Form method:', form.method);
        console.log('Form action:', form.action);
        console.log('Form enctype:', form.enctype);
        
        // Check for proper form configuration
        if (form.method.toLowerCase() !== 'post') {
            console.error('Warning: Form method should be POST, but got:', form.method);
        }
        
        if (!form.enctype || form.enctype.toLowerCase() !== 'multipart/form-data') {
            console.warn('Warning: Form enctype should be multipart/form-data for file uploads');
        }
        
        // Inspect all form elements
        const formElements = form.elements;
        console.log('Form elements:', formElements.length);
        
        // Check for required fields
        const requiredFields = ['Email', 'Password', 'ConfirmPassword'];
        const missingRequired = [];
        
        requiredFields.forEach(fieldName => {
            const field = form.querySelector(`[name$="${fieldName}"]`);
            if (!field) {
                missingRequired.push(fieldName);
            }
        });
        
        if (missingRequired.length > 0) {
            console.error('Missing required fields:', missingRequired.join(', '));
        }
        
        // Check for file upload field
        const fileUpload = form.querySelector('input[type="file"]');
        if (fileUpload) {
            console.log('Found file upload field:', fileUpload.name);
            
            // Monitor file selection
            fileUpload.addEventListener('change', function() {
                if (this.files.length > 0) {
                    const file = this.files[0];
                    console.log('File selected:', file.name);
                    console.log('File type:', file.type);
                    console.log('File size:', file.size, 'bytes');
                } else {
                    console.log('No file selected');
                }
            });
        } else {
            console.warn('No file upload field found - residency proof may be missing');
        }
        
        // Monitor form submission
        form.addEventListener('submit', function(event) {
            // Optional: Uncomment to prevent actual submission for debugging
            // event.preventDefault();
            
            console.log('Form submission triggered');
            
            // Log form data
            const formData = new FormData(form);
            console.log('Form data entries:');
            for (let [key, value] of formData.entries()) {
                // Don't log password values
                if (key.toLowerCase().includes('password')) {
                    console.log(`${key}: (value hidden for security)`);
                } else if (value instanceof File) {
                    console.log(`${key}: File - ${value.name} (${value.size} bytes, ${value.type})`);
                } else {
                    console.log(`${key}: ${value}`);
                }
            }
            
            // Check for status field
            if (!formData.has('Status') && !formData.has('Input.Status')) {
                console.warn('No Status field found in form - ensure it is set during registration');
            }
            
            // Validate all required fields
            let isValid = true;
            for (let i = 0; i < form.elements.length; i++) {
                const element = form.elements[i];
                
                if (element.required && !element.value) {
                    console.error(`Required field is empty: ${element.name}`);
                    isValid = false;
                }
            }
            
            // Check anti-forgery token presence (important for ASP.NET forms)
            const antiForgeryToken = form.querySelector('input[name="__RequestVerificationToken"]');
            if (!antiForgeryToken) {
                console.warn('No anti-forgery token found in form - this might cause submission issues');
            }
            
            // Continue with form submission if not prevented above
            if (event.defaultPrevented) {
                console.log('Form submission was prevented for debugging. Remove event.preventDefault() to enable actual submission.');
            } else {
                console.log('Form is being submitted...');
                
                // Monitor server response
                const originalAction = form.action;
                const originalMethod = form.method;
                
                // Log submission details for manual checking
                console.log(`Submission target: ${originalMethod.toUpperCase()} ${originalAction}`);
            }
        });
        
        // Add visual indicator that script is loaded
        const debugBadge = document.createElement('div');
        debugBadge.style.cssText = `
            position: fixed;
            bottom: 10px;
            right: 10px;
            background-color: #f8f9fa;
            border: 1px solid #6c757d;
            border-radius: 4px;
            padding: 5px 10px;
            font-size: 12px;
            color: #212529;
            z-index: 9999;
        `;
        debugBadge.innerHTML = ' Form Debug Active';
        document.body.appendChild(debugBadge);
        
        // Print conclusion
        console.log('Form debugging setup complete. Submit the form to see detailed logs.');
    });
    
    // Also listen to load event for cases where DOMContentLoaded already fired
    if (document.readyState === 'complete') {
        console.log('Document already loaded, checking for registration form...');
        const event = new Event('DOMContentLoaded');
        document.dispatchEvent(event);
    }
})(); 