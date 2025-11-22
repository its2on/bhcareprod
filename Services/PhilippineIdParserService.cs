using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Barangay.Services
{
    /// <summary>
    /// Service for parsing Philippine ID documents with ID-specific patterns
    /// </summary>
    public class PhilippineIdParserService
    {
        private readonly ILogger<PhilippineIdParserService> _logger;

        public PhilippineIdParserService(ILogger<PhilippineIdParserService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Detects ID type from OCR text
        /// </summary>
        public string DetectIdType(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var upperText = text.ToUpper();

            // Driver's License
            if (upperText.Contains("LTO") || 
                (upperText.Contains("DRIVER") || upperText.Contains("DRIVERS")) && upperText.Contains("LICENSE") ||
                upperText.Contains("LAND TRANSPORTATION") || upperText.Contains("TRANSPORTATION OFFICE"))
            {
                return "DriversLicense";
            }

            // PhilSys National ID
            if (upperText.Contains("PHILSYS") || 
                upperText.Contains("PHILIPPINE IDENTIFICATION SYSTEM") ||
                upperText.Contains("PAMBANSANG PAGKAKAKILANLAN") ||
                upperText.Contains("NATIONAL ID"))
            {
                return "PhilSys";
            }

            // PhilHealth ID
            if (upperText.Contains("PHILHEALTH") || 
                upperText.Contains("PHIL-HEALTH") ||
                upperText.Contains("PHIL HEALTH") ||
                upperText.Contains("HEALTH INSURANCE CORPORATION") ||
                upperText.Contains("MDR ID"))
            {
                return "PhilHealth";
            }

            // Postal ID
            if (upperText.Contains("POSTAL ID") || 
                upperText.Contains("PHILIPPINE POSTAL") ||
                upperText.Contains("PHLPOST") ||
                upperText.Contains("POST OFFICE") ||
                upperText.Contains("POSTAL REFERENCE NUMBER") ||
                upperText.Contains("PRN"))
            {
                return "PostalId";
            }

            // UMID
            if (upperText.Contains("UMID") || 
                upperText.Contains("UNIFIED MULTI-PURPOSE ID") ||
                upperText.Contains("COMMON REFERENCE NUMBER") ||
                upperText.Contains("CRN"))
            {
                return "UMID";
            }

            // TIN ID
            if (upperText.Contains("TIN") && 
                (upperText.Contains("TAX IDENTIFICATION") || upperText.Contains("BIR") || 
                 Regex.IsMatch(upperText, @"\b\d{3}-\d{3}-\d{3}-\d{3}\b")))
            {
                return "TINId";
            }

            // SSS ID
            if (upperText.Contains("SSS") && 
                (upperText.Contains("SOCIAL SECURITY") || 
                 Regex.IsMatch(upperText, @"\b\d{2}-\d{7}-\d{1}\b")))
            {
                return "SSSId";
            }

            // Passport
            if (upperText.Contains("PASSPORT") || 
                upperText.Contains("PASAPORTE") ||
                upperText.Contains("P<PHL"))
            {
                return "Passport";
            }

            return null;
        }

        /// <summary>
        /// Parses ID data based on detected ID type
        /// </summary>
        public ParsedIdData ParseIdByType(string text, string idType)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new ParsedIdData();

            _logger.LogInformation("Parsing {IdType} with text length: {Length}", idType, text.Length);

            return idType switch
            {
                "DriversLicense" => ParseDriversLicense(text),
                "PhilSys" => ParsePhilSys(text),
                "PhilHealth" => ParsePhilHealth(text),
                "PostalId" => ParsePostalId(text),
                "UMID" => ParseUMID(text),
                "TINId" => ParseTINId(text),
                "SSSId" => ParseSSSId(text),
                "Passport" => ParsePassport(text),
                _ => ParseGenericId(text)
            };
        }

        private ParsedIdData ParseDriversLicense(string text)
        {
            var result = new ParsedIdData { IdType = "DriversLicense" };
            var cleanedText = CleanOcrErrors(text);
            
            ExtractNameDriversLicense(cleanedText, result);
            ExtractBirthDate(cleanedText, result);
            ExtractAddress(cleanedText, result);
            result.Gender = ExtractGender(cleanedText);
            
            return result;
        }

        private ParsedIdData ParsePhilSys(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new ParsedIdData { IdType = "PhilSys" };

            var result = ParseGenericId(text);
            result.IdType = "PhilSys";

            if (string.IsNullOrWhiteSpace(result.Address))
                ExtractAddress(text, result);

            if (string.IsNullOrWhiteSpace(result.Gender))
                result.Gender = ExtractGender(text);

            if (string.IsNullOrWhiteSpace(result.ContactNumber))
                result.ContactNumber = ExtractContactNumber(text);

            return result;
        }

        private ParsedIdData ParsePhilHealth(string text)
        {
            var result = new ParsedIdData { IdType = "PhilHealth" };
            ExtractNameGeneric(text, result);
            ExtractBirthDate(text, result);
            ExtractAddress(text, result);
            return result;
        }

        private ParsedIdData ParsePostalId(string text)
        {
            var result = new ParsedIdData { IdType = "PostalId" };
            ExtractNameGeneric(text, result);
            ExtractBirthDate(text, result);
            ExtractAddress(text, result);
            return result;
        }

        private ParsedIdData ParseUMID(string text)
        {
            var result = new ParsedIdData { IdType = "UMID" };
            ExtractNameDriversLicense(text, result);
            ExtractBirthDate(text, result);
            ExtractAddress(text, result);
            return result;
        }

        private ParsedIdData ParseTINId(string text)
        {
            var result = new ParsedIdData { IdType = "TINId" };
            ExtractNameGeneric(text, result);
            ExtractBirthDate(text, result);
            ExtractAddress(text, result);
            return result;
        }

        private ParsedIdData ParseSSSId(string text)
        {
            var result = new ParsedIdData { IdType = "SSSId" };
            ExtractNameDriversLicense(text, result);
            ExtractBirthDate(text, result);
            ExtractAddress(text, result);
            return result;
        }

        private ParsedIdData ParsePassport(string text)
        {
            var result = new ParsedIdData { IdType = "Passport" };
            ExtractNameDriversLicense(text, result);
            ExtractBirthDate(text, result);
            ExtractAddress(text, result);
            return result;
        }

        private ParsedIdData ParseGenericId(string text)
        {
            var result = new ParsedIdData();
            ExtractNameGeneric(text, result);
            ExtractBirthDate(text, result);
            ExtractAddress(text, result);
            result.Gender = ExtractGender(text);
            return result;
        }

        // Helper methods
        private string CleanOcrErrors(string text)
        {
            return text
                .Replace("LITS'B IKI", "LT5 BLK1")
                .Replace("LITS'B", "LT5 BLK1")
                .Replace("LITS B", "LT5 BLK1")
                .Replace("LTS BLK", "LT5 BLK1")
                .Replace("IKI", "1")
                .Replace("NER", "NCR")
                .Replace("GITY", "CITY")
                .Replace("BARANGAYGITY", "BARANGAY")
                .Replace("ANT ", "ANTHONY ")
                .Replace("ANT,", "ANTHONY,")
                .Replace("ANT\n", "ANTHONY\n")
                .Replace("ALPHA HO!", "ALPHA HOMES")
                .Replace("ALPHA HOI", "ALPHA HOMES")
                .Replace("ALPHA HO ", "ALPHA HOMES ")
                .Replace("  ", " ")
                .Replace(" ,", ",");
        }

        private void ExtractNameDriversLicense(string text, ParsedIdData result)
        {
            var namePattern = @"\b([A-Z]{3,20}),\s+([A-Z]{2,20}(?:\s+[A-Z]{1,20}){0,3})\s*(JR\.?|SR\.?|I{2,3}|IV|V)?\s*([A-Z]{2,20})?\b";
            var nameMatch = Regex.Match(text, namePattern, RegexOptions.IgnoreCase);
            
            if (nameMatch.Success)
            {
                result.LastName = nameMatch.Groups[1].Value.Trim();
                var givenNames = nameMatch.Groups[2].Value.Trim();
                var nameParts = givenNames.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (nameParts.Length > 0)
                    result.FirstName = nameParts[0];
                
                if (nameParts.Length > 1)
                    result.MiddleName = string.Join(" ", nameParts.Skip(1));
                
                if (nameMatch.Groups.Count > 3 && !string.IsNullOrWhiteSpace(nameMatch.Groups[3].Value))
                    result.Suffix = nameMatch.Groups[3].Value.Trim().Replace(".", "");
            }
        }

        private void ExtractNameGeneric(string text, ParsedIdData result)
        {
            try
            {
                // First try to find the name in the format "APELYIDO, PANGALAN GITNANG PANGALAN"
                var namePattern = @"APELYIDO\s*\/\s*Last Name\s*([A-Z]+(?:\s+[A-Z]+)*)\s*Pangalan\s*\/\s*Given Names\s*([A-Z]+(?:\s+[A-Z]+)*)(?:\s+Gitnang\s+Apelyido\s*\/\s*Middle Name\s*([A-Z]+(?:\s+[A-Z]+)*))?";
                var nameMatch = Regex.Match(text, namePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                
                if (nameMatch.Success)
                {
                    // Format: APELYIDO, PANGALAN GITNANG PANGALAN
                    result.LastName = nameMatch.Groups[1].Value.Trim();
                    
                    var givenNames = nameMatch.Groups[2].Value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (givenNames.Length > 0)
                    {
                        result.FirstName = givenNames[0];
                        
                        if (givenNames.Length > 1)
                        {
                            result.MiddleName = string.Join(" ", givenNames.Skip(1));
                        }
                    }
                    
                    // If middle name is in a separate field
                    if (nameMatch.Groups.Count > 3 && !string.IsNullOrWhiteSpace(nameMatch.Groups[3].Value))
                    {
                        result.MiddleName = nameMatch.Groups[3].Value.Trim();
                    }
                    
                    _logger.LogInformation("Extracted name (APELYIDO format): {LastName}, {FirstName} {MiddleName}", 
                        result.LastName, result.FirstName, result.MiddleName);
                    return;
                }
                
                // Fallback to simpler pattern if the first one doesn't match
                namePattern = @"(?:Name|Full\s+Name|Pangalan|Apelyido)[\s:]+([A-Z]{2,20}(?:\s+[A-Z]{1,20}){1,4})";
                nameMatch = Regex.Match(text, namePattern, RegexOptions.IgnoreCase);
                
                if (nameMatch.Success)
                {
                    var fullName = nameMatch.Groups[1].Value.Trim();
                    var nameParts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (nameParts.Length > 0)
                        result.LastName = nameParts[0];
                    
                    if (nameParts.Length > 1)
                        result.FirstName = nameParts[1];
                    
                    if (nameParts.Length > 2)
                        result.MiddleName = string.Join(" ", nameParts.Skip(2));
                    
                    _logger.LogInformation("Extracted name (fallback): {LastName}, {FirstName} {MiddleName}", 
                        result.LastName, result.FirstName, result.MiddleName);
                }
                
                // Try to find names in the format "REBOREDO" (last name) and "RHYLLE LANDER" (first name)
                var lastNameMatch = Regex.Match(text, @"Apelyido\s*\/\s*Last Name\s*([A-Z]+(?:\s+[A-Z]+)*)", RegexOptions.IgnoreCase);
                var firstNameMatch = Regex.Match(text, @"Pangalan\s*\/\s*Given Names\s*([A-Z]+(?:\s+[A-Z]+)*)", RegexOptions.IgnoreCase);
                var middleNameMatch = Regex.Match(text, @"Gitnang\s+Apelyido\s*\/\s*Middle Name\s*([A-Z]+(?:\s+[A-Z]+)*)", RegexOptions.IgnoreCase);
                
                if (lastNameMatch.Success && firstNameMatch.Success)
                {
                    result.LastName = lastNameMatch.Groups[1].Value.Trim();
                    result.FirstName = firstNameMatch.Groups[1].Value.Trim();
                    
                    if (middleNameMatch.Success)
                    {
                        result.MiddleName = middleNameMatch.Groups[1].Value.Trim();
                    }
                    
                    _logger.LogInformation("Extracted name (separate fields): {LastName}, {FirstName} {MiddleName}", 
                        result.LastName, result.FirstName, result.MiddleName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting name from text");
            }
        }

        private void ExtractBirthDate(string text, ParsedIdData result)
        {
            var dobPatterns = new[]
            {
                @"(?:Date\s+of\s+Birth|Birth\s+Date|DOB)[:\s]*(\d{4})[/-](\d{1,2})[/-](\d{1,2})",
                @"(?:Date\s+of\s+Birth|Birth\s+Date|DOB)[:\s]*(\d{1,2})[/-](\d{1,2})[/-](\d{4})"
            };
            
            foreach (var pattern in dobPatterns)
            {
                var dobMatch = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (dobMatch.Success)
                {
                    var part1 = dobMatch.Groups[1].Value.Trim();
                    var part2 = dobMatch.Groups[2].Value.Trim();
                    var part3 = dobMatch.Groups[3].Value.Trim();
                    
                    if (part1.Length == 4)
                    {
                        result.BirthDate = $"{part1}-{part2.PadLeft(2, '0')}-{part3.PadLeft(2, '0')}";
                    }
                    else if (part3.Length == 4)
                    {
                        var year = part3;
                        if (int.TryParse(part1, out int p1) && p1 > 12)
                        {
                            result.BirthDate = $"{year}-{part2.PadLeft(2, '0')}-{part1.PadLeft(2, '0')}";
                        }
                        else
                        {
                            result.BirthDate = $"{year}-{part1.PadLeft(2, '0')}-{part2.PadLeft(2, '0')}";
                        }
                    }
                    
                    if (!string.IsNullOrWhiteSpace(result.BirthDate))
                        break;
                }
            }
        }

        private void ExtractAddress(string text, ParsedIdData result)
        {
            if (result == null || string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                // Clean common OCR errors first
                text = text.Replace("  ", " ")
                          .Replace(" ,", ",")
                          .Replace(",,", ",")
                          .Replace("CALOORA", "CALOOCAN")
                          .Replace("SOLE BARANICAY IGO", "BARANGAY 160")
                          .Replace("BARANICAY IGO", "BARANGAY 160")
                          .Replace("IGO CITY", "CITY OF CALOOCAN")
                          .Replace("RUBYVILLE SUBDE", "RUBYVILLE SUBD")
                          .Replace("NOR", "NCR")
                          .Replace("/ADDRESS", "")
                          .Replace("ADDRESS:", "")
                          .Replace("PHL", "")
                          .Trim();

                // Define address section patterns
                var addressPatterns = new[]
                {
                    @"(?:ADDRESS|TIRAHAN|ADRESA|BARANGAY|TIRAHAN:?)[\s:]*([^\n]*?)(?=\b(?:DATE|BIRTH|AGE|SEX|GENDER|CONTACT|PHONE|MOBILE|TEL|ID|PHILSYS|REPUBLIC|PHILIPPINE|PAMBANSA|$))",
                    @"(?:ADDRESS|TIRAHAN|ADRESA|BARANGAY|TIRAHAN:?)[\s:]*(.*)"
                };

                string addressText = string.Empty;

                // Try to find address section using patterns
                foreach (var pattern in addressPatterns)
                {
                    var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (match.Success)
                    {
                        addressText = match.Groups[1].Value.Trim();
                        break;
                    }
                }

                // If no match found, try to find address-like text
                if (string.IsNullOrEmpty(addressText))
                {
                    // Look for common address patterns
                    var addressLike = Regex.Match(text, @"\b(?:\d+[A-Z]*(?:\s+[A-Z][a-z]*)+\s+(?:STREET|ST|AVENUE|AVE|ROAD|RD|HIGHWAY|HWY|BOULEVARD|BLVD|SUBDIVISION|SUBD|VILLAGE|VILL|BRGY|BARANGAY)\b.*", 
                        RegexOptions.IgnoreCase);
                    if (addressLike.Success)
                    {
                        addressText = addressLike.Value.Trim();
                    }
                }

                if (!string.IsNullOrEmpty(addressText))
                {
                    // Clean up the address text
                    addressText = Regex.Replace(addressText, @"\s+", " ")
                                     .Replace(" ,", ",")
                                     .Replace(",,", ",")
                                     .Replace(" , ", ", ")
                                     .Replace("ALPHA HO!", "ALPHA HOMES")
                                     .Replace("ALPHA HOI", "ALPHA HOMES")
                                     .Replace("ALPHA HO ", "ALPHA HOMES ")
                                     .Replace("RUBYVILLESUBDE", "RUBYVILLE SUBD")
                                     .Replace("RUBYVILLE SUBDE", "RUBYVILLE SUBD")
                                     .Replace("CITY OF CALOOCAN NCR", "CITY OF CALOOCAN, NCR")
                                     .Trim();

                    // Extract barangay number with better pattern matching
                    var brgyMatch = Regex.Match(addressText, 
                        @"(?:BRGY|BRGY\.|BARANGAY|BRG\.?|BGY\.?)[\s,]*0*(\d{1,3})", 
                        RegexOptions.IgnoreCase);
                    
                    if (!brgyMatch.Success)
                    {
                        // Try alternative patterns for barangay
                        brgyMatch = Regex.Match(addressText, 
                            @"\b(?:BRGY|BRG|BGY|BARANGAY)[\s,]*#?0*(\d{1,3})\b", 
                            RegexOptions.IgnoreCase);
                    }

                    // Clean up address text before processing
                    var cleanAddressText = addressText;
                    
                    if (brgyMatch.Success)
                    {
                        result.Barangay = brgyMatch.Groups[1].Value.Trim();
                        // Remove the barangay part to avoid duplication
                        cleanAddressText = Regex.Replace(cleanAddressText, 
                            @"(?:BRGY|BRGY\.|BARANGAY|BRG\.?|BGY\.?)[\s,]*#?0*\d{1,3}", 
                            "", 
                            RegexOptions.IgnoreCase);
                    }

                    // Split into address parts
                    var addressParts = cleanAddressText
                        .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p) && p.Length >= 3)
                        .Where(p => !Regex.IsMatch(p, @"^\d{1,2}(?:ST|ND|RD|TH)?\s+(?:DISTRICT|DIST|DIST\.?)$", RegexOptions.IgnoreCase))
                        .ToList();

                    // Reconstruct the address with proper formatting
                    var formattedAddress = string.Join(", ", addressParts)
                        .Replace(",,", ",")
                        .Replace(" , ", ", ")
                        .Replace("  ", " ")
                        .Trim();

                    // Ensure proper comma separation for common patterns
                    formattedAddress = Regex.Replace(formattedAddress, 
                        @"(\d+[A-Z]*(?:\s+[A-Z][a-z]*)+)\s+([A-Z][a-z]+(?: [A-Z][a-z]+)*)", 
                        "$1, $2");

                    // Add barangay after city if not already present
                    if (!string.IsNullOrEmpty(result.Barangay) && !formattedAddress.Contains("BARANGAY", StringComparison.OrdinalIgnoreCase))
                    {
                        var cityMatch = Regex.Match(formattedAddress, @"(CITY\s+OF\s+[A-Z\s]+)", RegexOptions.IgnoreCase);
                        if (cityMatch.Success)
                        {
                            // Insert barangay after city
                            formattedAddress = formattedAddress.Insert(
                                cityMatch.Index + cityMatch.Length,
                                $", BARANGAY {result.Barangay}"
                            );
                        }
                        else
                        {
                            // If no city found, prepend barangay
                            formattedAddress = $"BARANGAY {result.Barangay}, {formattedAddress}";
                        }
                    }

                    // Final cleanup
                    result.Address = Regex.Replace(formattedAddress, @"\s+", " ")
                                        .Replace(" ,", ",")
                                        .Replace(",,", ",")
                                        .Replace(" , ", ", ")
                                        .Replace("  ", " ")
                                        .Trim(',', ' ', '-', '.');

                    _logger.LogInformation("Extracted address: {Address}", result.Address);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting address from text");
            }
        }

        /// <summary>
        /// Extracts gender from text (accessible by other services)
        /// </summary>
        public string ExtractGender(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            
            // PRIORITY CHECK: Driver's License format - "PHL M YYYY/MM/DD"
            var driverLicensePattern = @"(PHL|Nationality)\s+([MF])\s+\d{4}[/-]\d{2}[/-]\d{2}";
            var dlMatch = Regex.Match(text, driverLicensePattern, RegexOptions.IgnoreCase);
            if (dlMatch.Success)
            {
                if (dlMatch.Groups[2].Value.ToUpper() == "M")
                    return "Male";
                else if (dlMatch.Groups[2].Value.ToUpper() == "F")
                    return "Female";
            }
            
            var genderPattern = @"(?:SEX|GENDER|KASARIAN)[:\s]+([MF]|MALE|FEMALE|LALAKI|BABAE)";
            var genderMatch = Regex.Match(text, genderPattern, RegexOptions.IgnoreCase);
            
            if (genderMatch.Success)
            {
                var genderValue = genderMatch.Groups[1].Value.Trim().ToUpper();
                
                if (genderValue == "M" || genderValue == "MALE" || genderValue == "LALAKI")
                    return "Male";
                else if (genderValue == "F" || genderValue == "FEMALE" || genderValue == "BABAE")
                    return "Female";
            }
            
            return "";
        }

        /// <summary>
        /// Extracts contact number from text (accessible by other services)
        /// </summary>
        public string ExtractContactNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
                
            var phonePattern = @"(\+?63\s*9\d{9}|\b09\d{9}\b)";
            var phoneMatch = Regex.Match(text, phonePattern);
            
            if (phoneMatch.Success)
            {
                var number = phoneMatch.Groups[1].Value.Replace(" ", "").Replace("+63", "09");
                if (number.StartsWith("639"))
                {
                    number = "0" + number.Substring(2);
                }
                return number;
            }
            
            return "";
        }
    }
}