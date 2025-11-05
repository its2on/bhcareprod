using Barangay.Data;
using Barangay.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Barangay.Services
{
    /// <summary>
    /// Service for atomic, thread-safe family number generation
    /// </summary>
    public interface IFamilyNumberService
    {
        Task<GenerateFamilyNumberResponse> GenerateFamilyNumberAsync(string lastName, string? healthFacility = null, string? patientCategory = null);
        Task<string> GetNextFamilyNumberAsync(string prefix);
        Task<bool> ValidateFamilyNumberAsync(string familyNumber);
        Task<string?> GetExistingFamilyNumberAsync(string userId);
        Task<string?> GetFamilyNumberByLastNameAsync(string lastName);
        Task<GenerateFamilyNumberResponse> GenerateOrReuseFamilyNumberAsync(string lastName, string userId, bool sameFamily);
        Task<string> GenerateNewFamilyNumberAsync(string lastName);
    }

    public class FamilyNumberService : IFamilyNumberService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FamilyNumberService> _logger;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public FamilyNumberService(ApplicationDbContext context, ILogger<FamilyNumberService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Generates a family number based on last name, health facility, and patient category
        /// </summary>
        public async Task<GenerateFamilyNumberResponse> GenerateFamilyNumberAsync(
            string lastName, 
            string? healthFacility = null, 
            string? patientCategory = null)
        {
            try
            {
                _logger.LogInformation("=== FAMILY NUMBER GENERATION STARTED ===");
                _logger.LogInformation("LastName: {LastName}, HealthFacility: {HealthFacility}, PatientCategory: {PatientCategory}", 
                    lastName, healthFacility, patientCategory);

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    return new GenerateFamilyNumberResponse
                    {
                        Success = false,
                        Error = "Last name is required"
                    };
                }

                // Determine prefix based on priority: PatientCategory > HealthFacility > LastName
                string prefix = DeterminePrefix(lastName, healthFacility, patientCategory);
                _logger.LogInformation("Determined prefix: {Prefix}", prefix);

                // Generate the family number atomically
                string familyNumber = await GetNextFamilyNumberAsync(prefix);
                
                _logger.LogInformation("Generated family number: {FamilyNumber}", familyNumber);
                _logger.LogInformation("=== FAMILY NUMBER GENERATION COMPLETED SUCCESSFULLY ===");

                return new GenerateFamilyNumberResponse
                {
                    Success = true,
                    FamilyNumber = familyNumber,
                    IsPreexisting = false,
                    Prefix = prefix,
                    SequenceNumber = ExtractSequenceNumber(familyNumber)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating family number for LastName: {LastName}", lastName);
                return new GenerateFamilyNumberResponse
                {
                    Success = false,
                    Error = "Error generating family number. Please try again."
                };
            }
        }

        /// <summary>
        /// Gets the next family number for a given prefix atomically
        /// </summary>
        public async Task<string> GetNextFamilyNumberAsync(string prefix)
        {
            await _semaphore.WaitAsync();
            try
            {
                return await GetNextFamilyNumberInternalAsync(prefix);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Internal method for atomic family number generation using database transactions
        /// </summary>
        private async Task<string> GetNextFamilyNumberInternalAsync(string prefix)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                _logger.LogInformation("Starting atomic family number generation for prefix: {Prefix}", prefix);

                // Get or create the counter for this prefix
                var counter = await _context.FamilyNumberCounters
                    .FirstOrDefaultAsync(c => c.Prefix == prefix);

                if (counter == null)
                {
                    // Create new counter
                    counter = new FamilyNumberCounter
                    {
                        Prefix = prefix,
                        LastNumber = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.FamilyNumberCounters.Add(counter);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created new counter for prefix: {Prefix}", prefix);
                }

                // Increment the counter atomically
                counter.LastNumber++;
                counter.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string familyNumber = $"{prefix}-{counter.LastNumber:D3}";
                _logger.LogInformation("Successfully generated family number: {FamilyNumber}", familyNumber);

                return familyNumber;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to generate family number for prefix: {Prefix}", prefix);
                throw;
            }
        }

        /// <summary>
        /// Determines the prefix based on priority rules - Last Name takes priority for first-come-first-serve
        /// </summary>
        private string DeterminePrefix(string lastName, string? healthFacility, string? patientCategory)
        {
            // Priority 1: Last Name (first-come-first-serve based on patient's last name)
            if (!string.IsNullOrWhiteSpace(lastName))
            {
                return lastName.Substring(0, 1).ToUpper();
            }

            // Priority 2: Health Facility (if last name is not available)
            if (!string.IsNullOrWhiteSpace(healthFacility))
            {
                return healthFacility.Substring(0, 1).ToUpper();
            }

            // Priority 3: Patient Category (fallback)
            if (!string.IsNullOrWhiteSpace(patientCategory))
            {
                return patientCategory.Substring(0, 1).ToUpper();
            }

            // Default fallback
            return "X";
        }

        /// <summary>
        /// Extracts sequence number from family number
        /// </summary>
        private int ExtractSequenceNumber(string familyNumber)
        {
            if (string.IsNullOrEmpty(familyNumber) || !familyNumber.Contains('-'))
                return 0;

            var parts = familyNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int sequence))
            {
                return sequence;
            }

            return 0;
        }

        /// <summary>
        /// Validates if a family number format is correct
        /// </summary>
        public async Task<bool> ValidateFamilyNumberAsync(string familyNumber)
        {
            if (string.IsNullOrWhiteSpace(familyNumber))
                return false;

            // Check format: X-XXX (prefix-number)
            if (!System.Text.RegularExpressions.Regex.IsMatch(familyNumber, @"^[A-Z]-\d{3}$"))
                return false;

            // Check if it exists in the database
            var parts = familyNumber.Split('-');
            if (parts.Length != 2)
                return false;

            string prefix = parts[0];
            if (int.TryParse(parts[1], out int sequence))
            {
                var counter = await _context.FamilyNumberCounters
                    .FirstOrDefaultAsync(c => c.Prefix == prefix);
                
                return counter != null && sequence <= counter.LastNumber;
            }

            return false;
        }

        /// <summary>
        /// Gets existing family number for a user from Patient table
        /// </summary>
        public async Task<string?> GetExistingFamilyNumberAsync(string userId)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);
            
            return patient?.FamilyNumber;
        }

        /// <summary>
        /// Gets an existing family number by last name (for same family reuse)
        /// </summary>
        public async Task<string?> GetFamilyNumberByLastNameAsync(string lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                return null;

            var prefix = lastName.Substring(0, 1).ToUpper();
            
            _logger.LogInformation("Searching for existing family number with LastName: {LastName}, Prefix: {Prefix}", lastName, prefix);
            
            // Find the most recent family number for patients with matching last name
            // Extract last name from FullName field (format: "FirstName LastName")
            var patient = await _context.Patients
                .Where(p => p.FamilyNumber != null && 
                           p.FamilyNumber.StartsWith(prefix) &&
                           p.FullName.EndsWith(lastName))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
            
            if (patient != null)
            {
                _logger.LogInformation("Found existing family number: {FamilyNumber} for patient: {PatientName}", 
                    patient.FamilyNumber, patient.FullName);
            }
            else
            {
                _logger.LogInformation("No existing family number found for last name: {LastName}", lastName);
            }
            
            return patient?.FamilyNumber;
        }

        /// <summary>
        /// Generates a new family number or reuses existing one based on sameFamily flag
        /// </summary>
        public async Task<GenerateFamilyNumberResponse> GenerateOrReuseFamilyNumberAsync(
            string lastName, 
            string userId, 
            bool sameFamily)
        {
            try
            {
                _logger.LogInformation("GenerateOrReuseFamilyNumber - LastName: {LastName}, UserId: {UserId}, SameFamily: {SameFamily}", 
                    lastName, userId, sameFamily);

                // If booking for same family, try to reuse existing family number based on last name
                if (sameFamily)
                {
                    // First check if user already has a family number
                    var existingUserFamilyNumber = await GetExistingFamilyNumberAsync(userId);
                    if (!string.IsNullOrEmpty(existingUserFamilyNumber))
                    {
                        _logger.LogInformation("Reusing logged-in user's family number: {FamilyNumber}", existingUserFamilyNumber);
                        return new GenerateFamilyNumberResponse
                        {
                            Success = true,
                            FamilyNumber = existingUserFamilyNumber,
                            IsPreexisting = true,
                            Message = "Using existing family number"
                        };
                    }
                    
                    // Otherwise, search for family number by last name
                    var familyNumber = await GetFamilyNumberByLastNameAsync(lastName);
                    if (!string.IsNullOrEmpty(familyNumber))
                    {
                        _logger.LogInformation("Reusing family number from last name search: {FamilyNumber}", familyNumber);
                        return new GenerateFamilyNumberResponse
                        {
                            Success = true,
                            FamilyNumber = familyNumber,
                            IsPreexisting = true,
                            Message = "Reusing family number for same family"
                        };
                    }
                }

                // Generate new family number (when sameFamily = false OR no existing family found)
                _logger.LogInformation("Generating new family number for {LastName}", lastName);
                return await GenerateFamilyNumberAsync(lastName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateOrReuseFamilyNumberAsync");
                return new GenerateFamilyNumberResponse
                {
                    Success = false,
                    Error = "Error processing family number request"
                };
            }
        }

        /// <summary>
        /// Generate a brand new family number without checking for existing ones
        /// </summary>
        public async Task<string> GenerateNewFamilyNumberAsync(string lastName)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Generating brand new family number for {LastName}", lastName);

                // Get the first letter of last name for prefix
                var prefix = lastName.Substring(0, 1).ToUpper();

                // Get the next available number for this prefix
                var nextNumber = await GetNextFamilyNumberAsync(prefix);

                var newFamilyNumber = $"{prefix}-{nextNumber:D3}";
                _logger.LogInformation("Generated new family number: {FamilyNumber}", newFamilyNumber);

                return newFamilyNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating new family number for {LastName}", lastName);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
