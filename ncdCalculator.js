document.addEventListener('DOMContentLoaded', function() {
    // Get input elements
    const weightInput = document.getElementById('weight');
    const heightInput = document.getElementById('height');
    const bmiInput = document.getElementById('bmi');
    const waistInput = document.getElementById('waist');
    const hipInput = document.getElementById('hip');
    const whRatioInput = document.getElementById('whRatio');
    
    // Get patient gender - try to detect from patient info or use hidden input
    function getPatientGender() {
        // Try to get from hidden input if available
        const genderInput = document.getElementById('gender');
        if (genderInput && genderInput.value) {
            return genderInput.value.toLowerCase();
        }
        
        // Try to get from patient info text
        const patientInfo = document.querySelector('.patient-info') || 
                          document.querySelector('[data-patient-info]');
        
        if (patientInfo) {
            const infoText = patientInfo.textContent.toLowerCase();
            if (infoText.includes('female')) return 'female';
            if (infoText.includes('male')) return 'male';
        }
        
        // Check patient name for common indicators (less reliable)
        const patientName = document.querySelector('h1, h2, h3')?.textContent || '';
        if (patientName.includes('Mr.')) return 'male';
        if (patientName.includes('Ms.') || patientName.includes('Mrs.')) return 'female';
        
        // Default to male if cannot determine
        return 'male';
    }
    
    // Calculate BMI and select appropriate status
    function calculateBMI() {
        if (!weightInput || !heightInput || !bmiInput) return;
        
        const weight = parseFloat(weightInput.value);
        const heightCm = parseFloat(heightInput.value);
        
        if (isNaN(weight) || isNaN(heightCm) || weight <= 0 || heightCm <= 0) {
            bmiInput.value = '';
            return;
        }
        
        // Calculate BMI (weight in kg / height in m²)
        const heightM = heightCm / 100;
        const bmi = weight / (heightM * heightM);
        bmiInput.value = bmi.toFixed(2);
        
        // Select appropriate BMI status radio
        selectBMIStatus(bmi);
    }
    
    // Select the appropriate BMI status radio button
    function selectBMIStatus(bmi) {
        // Get all BMI status radio buttons
        const underweightRadio = document.querySelector('input[name="bmiStatus"][value="Underweight"]');
        const normalRadio = document.querySelector('input[name="bmiStatus"][value="Normal"]');
        const overweightRadio = document.querySelector('input[name="bmiStatus"][value="Overweight"]');
        const obeseRadio = document.querySelector('input[name="bmiStatus"][value="Obese"]');
        
        // Clear all selections first
        [underweightRadio, normalRadio, overweightRadio, obeseRadio].forEach(radio => {
            if (radio) radio.checked = false;
        });
        
        // Select based on BMI thresholds
        if (bmi < 18.5) {
            if (underweightRadio) underweightRadio.checked = true;
        } else if (bmi >= 18.5 && bmi < 23) {
            if (normalRadio) normalRadio.checked = true;
        } else if (bmi >= 23 && bmi < 25) {
            if (overweightRadio) overweightRadio.checked = true;
        } else if (bmi >= 25) {
            if (obeseRadio) obeseRadio.checked = true;
        }
    }
    
    // Calculate Waist-Hip Ratio and select appropriate status
    function calculateWHRatio() {
        if (!waistInput || !hipInput || !whRatioInput) return;
        
        const waist = parseFloat(waistInput.value);
        const hip = parseFloat(hipInput.value);
        
        if (isNaN(waist) || isNaN(hip) || waist <= 0 || hip <= 0) {
            whRatioInput.value = '';
            return;
        }
        
        // Calculate Waist-Hip Ratio
        const whRatio = waist / hip;
        whRatioInput.value = whRatio.toFixed(2);
        
        // Select appropriate WH status radio
        selectWHStatus(whRatio);
    }
    
    // Select the appropriate WH status radio button
    function selectWHStatus(whRatio) {
        // Get gender to determine appropriate threshold
        const gender = getPatientGender();
        
        // Get all WH status radio buttons
        const noRiskRadio = document.querySelector('input[name="whStatus"][value="No risk"]');
        const atRiskRadio = document.querySelector('input[name="whStatus"][value="At risk"]');
        
        // Clear all selections first
        [noRiskRadio, atRiskRadio].forEach(radio => {
            if (radio) radio.checked = false;
        });
        
        // Select based on WH ratio thresholds per gender
        if ((gender === 'male' && whRatio < 1.0) || 
            (gender === 'female' && whRatio < 0.85)) {
            if (noRiskRadio) noRiskRadio.checked = true;
        } else {
            if (atRiskRadio) atRiskRadio.checked = true;
        }
    }
    
    // Set up event listeners for real-time calculation
    function setupEventListeners() {
        // For BMI calculation
        if (weightInput) {
            weightInput.addEventListener('input', calculateBMI);
        }
        if (heightInput) {
            heightInput.addEventListener('input', calculateBMI);
        }
        
        // For Waist-Hip Ratio calculation
        if (waistInput) {
            waistInput.addEventListener('input', calculateWHRatio);
        }
        if (hipInput) {
            hipInput.addEventListener('input', calculateWHRatio);
        }
    }
    
    // Initialize calculations if fields already have values
    function initialize() {
        calculateBMI();
        calculateWHRatio();
        setupEventListeners();
    }
    
    // Start the calculations
    initialize();
});

