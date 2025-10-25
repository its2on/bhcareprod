using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Barangay.Services;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UploadHEEADSSSFormImageModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadHEEADSSSFormImageModel> _logger;
        private readonly IFormDataExtractionService _formDataExtractionService;

        public UploadHEEADSSSFormImageModel(
            IWebHostEnvironment environment,
            ILogger<UploadHEEADSSSFormImageModel> logger,
            IFormDataExtractionService formDataExtractionService)
        {
            _environment = environment;
            _logger = logger;
            _formDataExtractionService = formDataExtractionService;
        }

        public void OnGet()
        {
            // Display the upload form
        }

        public async Task<IActionResult> OnPostAsync(IFormFile file, string page)
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
            if (!new[] { "1", "2", "3", "4" }.Contains(page))
            {
                return new JsonResult(new { success = false, message = "Invalid page number. Only 1, 2, 3, or 4 are allowed" });
            }

            try
            {
                // Create target directory if it doesn't exist
                var uploadPath = Path.Combine(_environment.WebRootPath, "images", "forms");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Determine target filename
                var fileName = $"heeadsss-form-page{page}.jpg";
                var filePath = Path.Combine(uploadPath, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Log the successful upload
                _logger.LogInformation($"Admin {User.Identity.Name} uploaded HEEADSSS form image for page {page}");

                // Return success with the file URL
                return new JsonResult(new
                {
                    success = true,
                    message = $"Successfully uploaded HEEADSSS form image for page {page}",
                    fileUrl = $"/images/forms/{fileName}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading HEEADSSS form image: {ex.Message}");
                return new JsonResult(new { success = false, message = "An error occurred while saving the file" });
            }
        }

        public async Task<IActionResult> OnPostSaveManualFormAsync()
        {
            try
            {
                // Read the JSON data from the request body
                using var reader = new StreamReader(Request.Body);
                var jsonString = await reader.ReadToEndAsync();
                
                if (string.IsNullOrEmpty(jsonString))
                {
                    return new JsonResult(new { success = false, message = "No form data received." });
                }

                // Parse the JSON data
                var formData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
                
                if (formData == null)
                {
                    return new JsonResult(new { success = false, message = "Invalid form data format." });
                }

                // Log the received data for debugging
                _logger.LogInformation($"Received HEEADSSS manual form data: {jsonString}");

                // Here you can process the form data as needed
                // For example, save to database, validate, etc.
                
                // For now, just return success
                return new JsonResult(new 
                { 
                    success = true, 
                    message = "HEEADSSS manual form data received and processed successfully.",
                    data = formData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing HEEADSSS manual form data: {ex.Message}");
                return new JsonResult(new { success = false, message = "An error occurred while processing the form data." });
            }
        }
    }
}

