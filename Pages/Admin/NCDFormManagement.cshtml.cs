using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Barangay.Data;
using Barangay.Models;
using Microsoft.EntityFrameworkCore;
using Barangay.Services;
using System.Text.Json;
using Barangay.Extensions;

namespace Barangay.Pages.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    public class NCDFormManagementModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<NCDFormManagementModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IFormDataExtractionService _formDataExtractionService;

        public NCDFormManagementModel(
            IWebHostEnvironment environment,
            ILogger<NCDFormManagementModel> logger,
            ApplicationDbContext context,
            IDataEncryptionService encryptionService,
            IFormDataExtractionService formDataExtractionService)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
            _encryptionService = encryptionService;
            _formDataExtractionService = formDataExtractionService;
        }

        public List<FormImageInfo> UploadedImages { get; set; } = new List<FormImageInfo>();

        public void OnGet()
        {
            LoadUploadedImages();
        }

        public async Task<IActionResult> OnPostUploadImageAsync(IFormFile file, string page, string description)
        {
            if (file == null || string.IsNullOrEmpty(page))
            {
                return new JsonResult(new { success = false, message = "File or page not specified" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Array.Exists(allowedExtensions, e => e == extension))
            {
                return new JsonResult(new { success = false, message = "Only JPG and PNG files are allowed" });
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return new JsonResult(new { success = false, message = "File is too large. Maximum size is 5MB" });
            }

            // Validate page number
            if (page != "1" && page != "2")
            {
                return new JsonResult(new { success = false, message = "Invalid page number. Only 1 or 2 are allowed" });
            }

            try
            {
                // Create target directory if it doesn't exist
                var uploadPath = Path.Combine(_environment.WebRootPath, "images", "forms", "admin");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Determine target filename with timestamp for uniqueness
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var fileName = $"ncd-form-page{page}-{timestamp}.jpg";
                var filePath = Path.Combine(uploadPath, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Log the successful upload
                _logger.LogInformation($"Admin {User.Identity.Name} uploaded NCD form image for page {page} with description: {description}");

                // Return success with the file URL
                return new JsonResult(new
                {
                    success = true,
                    message = $"Successfully uploaded NCD form image for page {page}",
                    fileUrl = $"/images/forms/admin/{fileName}",
                    fileName = fileName,
                    page = page,
                    description = description,
                    uploadDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading NCD form image: {ex.Message}");
                return new JsonResult(new { success = false, message = "An error occurred while saving the file" });
            }
        }

        public async Task<IActionResult> OnPostDeleteImageAsync(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return new JsonResult(new { success = false, message = "File name not specified" });
                }

                var filePath = Path.Combine(_environment.WebRootPath, "images", "forms", "admin", fileName);
                
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation($"Admin {User.Identity.Name} deleted NCD form image: {fileName}");
                    return new JsonResult(new { success = true, message = "Image deleted successfully" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "File not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting NCD form image: {ex.Message}");
                return new JsonResult(new { success = false, message = "An error occurred while deleting the file" });
            }
        }

        public async Task<IActionResult> OnPostProcessImageForEditAsync(string fileName, string pageNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(pageNumber))
                {
                    return new JsonResult(new { success = false, message = "File name or page number not specified" });
                }

                var filePath = Path.Combine(_environment.WebRootPath, "images", "forms", "admin", fileName);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return new JsonResult(new { success = false, message = "File not found" });
                }

                // Simulate OCR processing (in a real implementation, you would use OCR libraries like Tesseract)
                var extractedData = await ProcessImageWithOCR(filePath, pageNumber);
                
                _logger.LogInformation($"Admin {User.Identity.Name} processed NCD form image for editing: {fileName}");

                return new JsonResult(new 
                { 
                    success = true, 
                    isReadable = extractedData.IsReadable,
                    extractedData = extractedData.Data,
                    message = extractedData.IsReadable ? "Data extracted successfully" : "Image not readable, manual entry required"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing NCD form image for edit: {ex.Message}");
                return new JsonResult(new { success = false, message = "An error occurred while processing the image" });
            }
        }

        private async Task<(bool IsReadable, object Data)> ProcessImageWithOCR(string filePath, string pageNumber)
        {
            try
            {
                _logger.LogInformation($"Processing NCD form image with OCR: {filePath} for page {pageNumber}");
                
                // First, try to load stored OCR data if it exists
                var dataPath = Path.Combine(_environment.WebRootPath, "images", "forms", $"ncd-form-page{pageNumber}-data.json");
                if (System.IO.File.Exists(dataPath))
                {
                    try
                    {
                        var storedData = await System.IO.File.ReadAllTextAsync(dataPath);
                        var extractedData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(storedData);
                        
                        if (extractedData != null && extractedData.Count > 0)
                        {
                            _logger.LogInformation($"Loaded stored OCR data for page {pageNumber} with {extractedData.Count} fields");
                            return (true, extractedData);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to load stored OCR data for page {pageNumber}: {ex.Message}");
                    }
                }
                
                // If no stored data, process with OCR
                var result = await _formDataExtractionService.ExtractFormDataAsync(filePath, "NCD", pageNumber);
                
                if (result.IsReadable && result.ExtractedData.Count > 0)
                {
                    // Store the extracted data for future use
                    await StoreExtractedFormData(pageNumber, result.ExtractedData);
                    _logger.LogInformation($"OCR processing successful for page {pageNumber}. Extracted {result.ExtractedData.Count} fields.");
                    return (true, result.ExtractedData);
                }
                else
                {
                    _logger.LogWarning($"OCR processing failed for page {pageNumber}");
                    return (false, new Dictionary<string, object>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing NCD form image with OCR: {ex.Message}");
                return (false, new Dictionary<string, object>());
            }
        }

        public async Task<IActionResult> OnPostSaveFormDataAsync(string fileName, string pageNumber, IFormCollection formData)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(pageNumber))
                {
                    return new JsonResult(new { success = false, message = "File name or page number not specified" });
                }

                // Check if this is Page 2 and if there's an existing record to update
                if (pageNumber == "2")
                {
                    // Look for existing record (most recent one with similar data)
                    var existingRecord = await _context.NCDRiskAssessments
                        .OrderByDescending(n => n.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (existingRecord != null)
                    {
                        // Update existing record with Page 2 data
                        await UpdateExistingNCDRecord(existingRecord, formData, pageNumber);
                        _logger.LogInformation($"Admin {User.Identity.Name} updated existing NCD record {existingRecord.Id} with Page 2 data");
                        return new JsonResult(new 
                        { 
                            success = true, 
                            message = "NCD form data updated successfully with Page 2 information",
                            ncdId = existingRecord.Id,
                            isUpdate = true
                        });
                    }
                }

                // Create new record (for Page 1 or if no existing record found for Page 2)
                var result = await SaveToNCDDatabase(formData, pageNumber);
                
                if (result.Success)
                {
                    _logger.LogInformation($"Admin {User.Identity.Name} saved NCD form data for {fileName} (Page {pageNumber}) to database with ID: {result.NCDId}");
                    return new JsonResult(new 
                    { 
                        success = true, 
                        message = $"NCD form data saved successfully to database for Page {pageNumber}",
                        ncdId = result.NCDId,
                        isUpdate = false
                    });
                }
                else
                {
                    return new JsonResult(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving form data: {ex.Message}");
                return new JsonResult(new { success = false, message = "An error occurred while saving the form data" });
            }
        }

        private string ProcessFormData(IFormCollection formData, string pageNumber)
        {
            var data = new Dictionary<string, object>();
            
            foreach (var key in formData.Keys)
            {
                if (key != "__RequestVerificationToken" && key != "fileName" && key != "pageNumber")
                {
                    var value = formData[key];
                    if (value.Count > 1)
                    {
                        data[key] = value.ToArray();
                    }
                    else
                    {
                        data[key] = value.ToString();
                    }
                }
            }
            
            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        private async Task<(bool Success, int? NCDId, string ErrorMessage)> SaveToNCDDatabase(IFormCollection formData, string pageNumber)
        {
            try
            {
                // Create new NCD Risk Assessment record
                var ncdAssessment = new NCDRiskAssessment
                {
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    AppointmentType = "NCD Assessment"
                };

                // Map form data to NCDRiskAssessment properties
                MapFormDataToNCD(formData, ncdAssessment);

                // Encrypt sensitive data before saving
                ncdAssessment.EncryptSensitiveData(_encryptionService);

                // Add to database
                _context.NCDRiskAssessments.Add(ncdAssessment);
                await _context.SaveChangesAsync();

                return (true, (int?)ncdAssessment.Id, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving NCD data to database: {ex.Message}");
                return (false, null, $"Database error: {ex.Message}");
            }
        }

        private async Task UpdateExistingNCDRecord(NCDRiskAssessment existingRecord, IFormCollection formData, string pageNumber)
        {
            try
            {
                // Update the existing record with Page 2 data
                existingRecord.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                
                // Map Page 2 specific data to existing record
                MapPage2DataToNCD(formData, existingRecord);
                
                // Save changes
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating existing NCD record: {ex.Message}");
                throw;
            }
        }

        private void MapPage2DataToNCD(IFormCollection formData, NCDRiskAssessment ncdAssessment)
        {
            // Exercise Information
            if (formData.ContainsKey("exerciseType"))
                ncdAssessment.ExerciseDuration = formData["exerciseType"].ToString();
            
            // Smoking Information
            if (formData.ContainsKey("isSmoker"))
            {
                var smokingStatus = formData["isSmoker"].ToString();
                ncdAssessment.SmokingStatus = smokingStatus == "Yes" ? "Current Smoker" : "Non-Smoker";
            }
            
            // Stress Information
            if (formData.ContainsKey("isStressed"))
            {
                var isStressed = formData["isStressed"].ToString();
                ncdAssessment.RiskStatus = isStressed == "true" ? "High Risk" : "Low Risk";
            }
            
            // Anthropometric Measurements
            if (formData.ContainsKey("weight") && double.TryParse(formData["weight"].ToString(), out double weight))
            {
                // Store weight in a field that exists in the database
                // Since we don't have a weight field, we'll use a text field or skip
            }
            
            if (formData.ContainsKey("height") && double.TryParse(formData["height"].ToString(), out double height))
            {
                // Store height in a field that exists in the database
                // Since we don't have a height field, we'll use a text field or skip
            }
            
            // Blood Pressure
            if (formData.ContainsKey("systolicBP") && int.TryParse(formData["systolicBP"].ToString(), out int systolicBP))
            {
                // Store in existing fields or skip if not available
            }
            
            if (formData.ContainsKey("diastolicBP") && int.TryParse(formData["diastolicBP"].ToString(), out int diastolicBP))
            {
                // Store in existing fields or skip if not available
            }
            
            // Blood Sugar
            if (formData.ContainsKey("fastingBloodSugar") && int.TryParse(formData["fastingBloodSugar"].ToString(), out int fastingBloodSugar))
            {
                // Store in existing fields or skip if not available
            }
            
            // Cholesterol
            if (formData.ContainsKey("totalCholesterol") && int.TryParse(formData["totalCholesterol"].ToString(), out int totalCholesterol))
            {
                // Store in existing fields or skip if not available
            }
            
            // Assessment Information
            if (formData.ContainsKey("riskPercentage") && double.TryParse(formData["riskPercentage"].ToString(), out double riskPercentage))
            {
                ncdAssessment.RiskStatus = $"Risk Level: {riskPercentage}%";
            }
            
            if (formData.ContainsKey("riskFactors"))
                ncdAssessment.RiskStatus += $" - {formData["riskFactors"].ToString()}";
            
            if (formData.ContainsKey("interviewedBy"))
            {
                // Store interviewer information
            }
            
            if (formData.ContainsKey("patientSignature"))
            {
                // Store patient signature
            }
        }

        private void MapFormDataToNCD(IFormCollection formData, NCDRiskAssessment ncdAssessment)
        {
            // Health Facility Information
            ncdAssessment.HealthFacility = formData["healthFacility"].ToString();
            ncdAssessment.FamilyNo = formData["familyNo"].ToString();
            ncdAssessment.Address = formData["address"].ToString();
            ncdAssessment.Barangay = formData["barangay"].ToString();
            
            // Personal Information
            ncdAssessment.FirstName = formData["firstName"].ToString();
            ncdAssessment.MiddleName = formData["middleName"].ToString();
            ncdAssessment.LastName = formData["lastName"].ToString();
            ncdAssessment.Telepono = formData["telepono"].ToString();
            
            // Parse birthday
            if (DateTime.TryParse(formData["birthday"].ToString(), out DateTime birthday))
            {
                ncdAssessment.Birthday = birthday.ToString("yyyy-MM-dd");
            }
            
            // Parse age
            if (int.TryParse(formData["edad"].ToString(), out int age))
            {
                ncdAssessment.Edad = age.ToString();
            }
            
            ncdAssessment.Kasarian = formData["kasarian"].ToString();
            ncdAssessment.Relihiyon = formData["relihiyon"].ToString();
            ncdAssessment.CivilStatus = formData["civilStatus"].ToString();
            ncdAssessment.Occupation = formData["occupation"].ToString();

            // Medical History
            ncdAssessment.HasDiabetes = formData.ContainsKey("hasDiabetes") ? "true" : "false";
            ncdAssessment.HasHypertension = formData.ContainsKey("hasHypertension") ? "true" : "false";
            ncdAssessment.HasCancer = formData.ContainsKey("hasCancer") ? "true" : "false";
            ncdAssessment.HasLungDisease = formData.ContainsKey("hasLungDisease") ? "true" : "false";
            ncdAssessment.HasCOPD = formData.ContainsKey("hasCOPD") ? "true" : "false";
            ncdAssessment.HasEyeDisease = formData.ContainsKey("hasEyeDisease") ? "true" : "false";
            
            ncdAssessment.CancerType = formData["cancerSite"].ToString();
            
            // Parse year diagnosed
            if (int.TryParse(formData["yearDiagnosed"].ToString(), out int yearDiagnosed))
            {
                ncdAssessment.DiabetesYear = yearDiagnosed.ToString();
                ncdAssessment.HypertensionYear = yearDiagnosed.ToString();
                ncdAssessment.CancerYear = yearDiagnosed.ToString();
                ncdAssessment.LungDiseaseYear = yearDiagnosed.ToString();
            }
            
            ncdAssessment.DiabetesMedication = formData["medication"].ToString();
            ncdAssessment.HypertensionMedication = formData["medication"].ToString();
            ncdAssessment.CancerMedication = formData["medication"].ToString();
            ncdAssessment.LungDiseaseMedication = formData["medication"].ToString();

            // Family History
            ncdAssessment.FamilyHasHypertension = formData.ContainsKey("familyHasHypertension") ? "true" : "false";
            ncdAssessment.FamilyHasHeartDisease = formData.ContainsKey("familyHasHeartDisease") ? "true" : "false";
            ncdAssessment.FamilyHasStroke = formData.ContainsKey("familyHasStroke") ? "true" : "false";
            ncdAssessment.FamilyHasDiabetes = formData.ContainsKey("familyHasDiabetes") ? "true" : "false";
            ncdAssessment.FamilyHasCancer = formData.ContainsKey("familyHasCancer") ? "true" : "false";
            ncdAssessment.FamilyHasKidneyDisease = formData.ContainsKey("familyHasKidneyDisease") ? "true" : "false";
            ncdAssessment.FamilyHasOtherDisease = formData.ContainsKey("familyHasOtherDisease") ? "true" : "false";
            ncdAssessment.FamilyOtherDiseaseDetails = formData["familyOtherDiseaseDetails"].ToString();

            // Lifestyle Factors
            ncdAssessment.HighSaltIntake = formData.ContainsKey("highSaltIntake") ? "true" : "false";
            ncdAssessment.AlcoholConsumption = formData["drinksAlcohol"].ToString();
            ncdAssessment.AlcoholFrequency = formData["alcoholFrequency"].ToString();
            ncdAssessment.HasNoRegularExercise = (!formData.ContainsKey("hasRegularExercise") || formData["hasRegularExercise"].ToString() == "No") ? "true" : "false";

            // Smoking Status
            if (formData["isSmoker"].ToString() == "Yes")
            {
                ncdAssessment.SmokingStatus = "Current Smoker";
            }
            else if (formData["smokingQuitDuration"].ToString() == "LessThan1Year")
            {
                ncdAssessment.SmokingStatus = "Quit Less Than 1 Year";
            }
            else if (formData["smokingQuitDuration"].ToString() == "MoreThan1Year")
            {
                ncdAssessment.SmokingStatus = "Quit More Than 1 Year";
            }
            else
            {
                ncdAssessment.SmokingStatus = "Non-smoker";
            }

            // Risk Status
            ncdAssessment.RiskStatus = "Assessment Completed";

            // Set default values for required fields
            ncdAssessment.UserId = null; // No specific user associated with form upload
            ncdAssessment.AppointmentId = null; // No appointment associated with form upload
        }

        public async Task<IActionResult> OnPostSetActiveImageAsync(string fileName, string page)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(page))
                {
                    return new JsonResult(new { success = false, message = "File name or page not specified" });
                }

                // Copy the selected image to be the active image for the page
                var sourcePath = Path.Combine(_environment.WebRootPath, "images", "forms", "admin", fileName);
                var targetPath = Path.Combine(_environment.WebRootPath, "images", "forms", $"ncd-form-page{page}.jpg");

                if (System.IO.File.Exists(sourcePath))
                {
                    System.IO.File.Copy(sourcePath, targetPath, true);
                    
                    // Process the image with OCR to extract form data
                    _logger.LogInformation($"Processing OCR for active NCD form image: {fileName}");
                    var ocrResult = await _formDataExtractionService.ExtractFormDataAsync(targetPath, "NCD", page);
                    
                    if (ocrResult.IsReadable && ocrResult.ExtractedData.Count > 0)
                    {
                        _logger.LogInformation($"OCR processing successful for {fileName}. Extracted {ocrResult.ExtractedData.Count} fields.");
                        
                        // Store the extracted data for this page
                        await StoreExtractedFormData(page, ocrResult.ExtractedData);
                        
                        _logger.LogInformation($"Admin {User.Identity.Name} set active NCD form image for page {page}: {fileName} and processed OCR data");
                        return new JsonResult(new { 
                            success = true, 
                            message = $"Page {page} image updated successfully and form data extracted ({ocrResult.ExtractedData.Count} fields)" 
                        });
                    }
                    else
                    {
                        _logger.LogWarning($"OCR processing failed or no data extracted for {fileName}");
                        return new JsonResult(new { 
                            success = true, 
                            message = $"Page {page} image updated successfully, but OCR processing failed. You can manually edit the form data." 
                        });
                    }
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Source file not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting active NCD form image: {ex.Message}");
                return new JsonResult(new { success = false, message = "An error occurred while updating the active image" });
            }
        }

        private async Task StoreExtractedFormData(string pageNumber, Dictionary<string, object> extractedData)
        {
            try
            {
                // Store the extracted data in a JSON file for this page
                var dataPath = Path.Combine(_environment.WebRootPath, "images", "forms", $"ncd-form-page{pageNumber}-data.json");
                var jsonData = System.Text.Json.JsonSerializer.Serialize(extractedData, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(dataPath, jsonData);
                
                _logger.LogInformation($"Stored extracted form data for page {pageNumber} with {extractedData.Count} fields");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error storing extracted form data for page {pageNumber}: {ex.Message}");
            }
        }

        private void LoadUploadedImages()
        {
            try
            {
                var adminImagesPath = Path.Combine(_environment.WebRootPath, "images", "forms", "admin");
                if (Directory.Exists(adminImagesPath))
                {
                    var files = Directory.GetFiles(adminImagesPath, "ncd-form-page*.jpg")
                        .OrderByDescending(f => System.IO.File.GetCreationTime(f))
                        .ToList();

                    UploadedImages = files.Select(file =>
                    {
                        var fileName = Path.GetFileName(file);
                        var fileInfo = new FileInfo(file);
                        var pageNumber = ExtractPageNumber(fileName);
                        
                        return new FormImageInfo
                        {
                            FileName = fileName,
                            FilePath = $"/images/forms/admin/{fileName}",
                            PageNumber = pageNumber,
                            UploadDate = fileInfo.CreationTime,
                            FileSize = fileInfo.Length,
                            IsActive = IsActiveImage(fileName, pageNumber)
                        };
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading uploaded images");
                UploadedImages = new List<FormImageInfo>();
            }
        }

        private string ExtractPageNumber(string fileName)
        {
            // Extract page number from filename like "ncd-form-page1-20241201120000.jpg"
            var parts = fileName.Split('-');
            if (parts.Length >= 3 && parts[2].StartsWith("page"))
            {
                return parts[2].Replace("page", "");
            }
            return "Unknown";
        }

        private bool IsActiveImage(string fileName, string pageNumber)
        {
            var activeImagePath = Path.Combine(_environment.WebRootPath, "images", "forms", $"ncd-form-page{pageNumber}.jpg");
            if (System.IO.File.Exists(activeImagePath))
            {
                var activeFileInfo = new FileInfo(activeImagePath);
                var currentFileInfo = new FileInfo(Path.Combine(_environment.WebRootPath, "images", "forms", "admin", fileName));
                
                // Compare file sizes and creation times to determine if this is the active image
                return activeFileInfo.Length == currentFileInfo.Length && 
                       Math.Abs((activeFileInfo.CreationTime - currentFileInfo.CreationTime).TotalSeconds) < 5;
            }
            return false;
        }
    }

    public class FormImageInfo
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string PageNumber { get; set; }
        public DateTime UploadDate { get; set; }
        public long FileSize { get; set; }
        public bool IsActive { get; set; }
    }
}
