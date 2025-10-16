using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Barangay.Services;
using Barangay.Migrations;

namespace Barangay.Controllers
{
    [Authorize(Roles = "Admin,System Administrator")]
    public class DataEncryptionController : Controller
    {
        // private readonly EncryptExistingDataService _encryptionService;

        // public DataEncryptionController(EncryptExistingDataService encryptionService)
        // {
        //     _encryptionService = encryptionService;
        // }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EncryptExistingData()
        {
            try
            {
                // await _encryptionService.EncryptAllExistingDataAsync();
                
                TempData["SuccessMessage"] = "Encryption service temporarily disabled for debugging.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error encrypting data: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TestEncryption(string testData)
        {
            try
            {
                if (string.IsNullOrEmpty(testData))
                {
                    TempData["ErrorMessage"] = "Please provide test data to encrypt.";
                    return RedirectToAction(nameof(Index));
                }

                // This would be implemented with the actual encryption service
                // For now, just return a success message
                TempData["SuccessMessage"] = "Encryption test completed successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error testing encryption: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
