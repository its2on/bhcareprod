// Test script to verify NCD Risk Assessment form submission
// This script can be run in the browser console on the NCD form page

async function testNCDSubmission() {
    console.log("=== Testing NCD Risk Assessment Submission ===");

    try {
        // Test data that matches the form structure
        const testData = {
            AppointmentId: "1-1416", // Use the appointment ID from your URL
            UserId: "test-user-id", // This should be the actual user ID
            HealthFacility: "Barangay Health Center",
            FamilyNo: "TEST-001",
            Address: "123 Test Street",
            Barangay: "122",
            Birthday: "1990-01-01T00:00:00.000Z",
            Telepono: "1234567890",
            Edad: 34,
            Kasarian: "Male",
            Relihiyon: "Catholic",
            CivilStatus: "Single",
            FirstName: "Test",
            MiddleName: "User",
            LastName: "Name",
            Occupation: "Software Developer",
            AppointmentType: "General Checkup",

            // Medical History
            HasDiabetes: false,
            DiabetesYear: "",
            DiabetesMedication: "",
            HasHypertension: false,
            HypertensionYear: "",
            HypertensionMedication: "",
            HasCancer: false,
            CancerType: "",
            CancerYear: "",
            CancerMedication: "",
            HasCOPD: false,
            HasLungDisease: false,
            LungDiseaseYear: "",
            LungDiseaseMedication: "",
            HasEyeDisease: false,

            // Family History
            FamilyHasHypertension: false,
            FamilyHasHeartDisease: false,
            FamilyHasStroke: false,
            FamilyHasDiabetes: false,
            FamilyHasCancer: false,
            FamilyHasKidneyDisease: false,
            FamilyHasOtherDisease: false,
            FamilyOtherDiseaseDetails: "",

            // Lifestyle Factors
            SmokingStatus: "Non-smoker",
            HighSaltIntake: false,
            AlcoholFrequency: "None",
            AlcoholConsumption: "None",
            ExerciseDuration: "30 minutes daily",
            HasNoRegularExercise: false,

            // Health Conditions
            HasDifficultyBreathing: false,
            HasAsthma: false,

            // Risk Status
            RiskStatus: "Low Risk"
        };

        console.log("Test data prepared:", testData);

        // Encrypt the data (simplified version - you'll need the actual encryption function)
        const jsonData = JSON.stringify(testData);
        console.log("JSON data:", jsonData);

        // For testing, we'll send the data directly without encryption first
        const response = await fetch('/User/NCDRiskAssessment?handler=SubmitAssessment', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: `encryptedData=${encodeURIComponent(jsonData)}`
        });

        const result = await response.json();
        console.log("Response:", result);

        if (result.success) {
            console.log("NCD Assessment submitted successfully!");
            console.log("Assessment ID:", result.assessmentId);
            console.log("Rows affected:", result.rowsAffected);
        } else {
            console.error("NCD Assessment submission failed:", result.error);
        }

    } catch (error) {
        console.error("Test failed:", error);
    }
}

// Run the test
testNCDSubmission();
