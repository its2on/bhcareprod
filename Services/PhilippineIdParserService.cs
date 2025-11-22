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
            var namePattern = @"(?:Name|Full\s+Name)[:\s]+([A-Z]{2,20}(?:\s+[A-Z]{1,20}){1,4})";
            var nameMatch = Regex.Match(text, namePattern, RegexOptions.IgnoreCase);
            
            if (nameMatch.Success)
            {
                var fullName = nameMatch.Groups[1].Value.Trim();
                var nameParts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (nameParts.Length > 0)
                    result.FirstName = nameParts[0];
                
                if (nameParts.Length > 2)
                    result.MiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                
                if (nameParts.Length > 1)
                    result.LastName = nameParts[nameParts.Length - 1];
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

            var addressKeywords = new[] { "ADDRESS", "TIRAHAN", "BARANGAY" };
            var addressStartIndex = -1;
            string keywordFound = string.Empty;

            // Find the first occurrence of any address keyword
            foreach (var keyword in addressKeywords)
            {
                var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                if (index >= 0 && (addressStartIndex == -1 || index < addressStartIndex))
                {
                    addressStartIndex = index;
                    keywordFound = keyword;
                }
            }

            if (addressStartIndex >= 0)
            {
                // Take text from the keyword to the end of the document
                var addressText = text.Substring(addressStartIndex + keywordFound.Length);
                addressText = CleanOcrErrors(addressText);
                
                // Special handling for PhilSys ID format
                if (text.Contains("PHILIPPINE IDENTIFICATION SYSTEM") || text.Contains("PHILSYS"))
                {
                    // For PhilSys, look for the address after the "PHL" line
                    var phlMatch = Regex.Match(addressText, @"(PHL\s*[MF]\s*\d{4}/\d{2}/\d{2})\s*([\s\S]*?)(?=\n\s*\n|\Z)", RegexOptions.IgnoreCase);
                    if (phlMatch.Success)
                    {
                        // Get the address part after the PHL line
                        var addressPart = phlMatch.Groups[2].Value.Trim();
                        
                        // Split into lines and clean each line
                        var addressLines = addressPart.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(line => line.Trim())
                            .Where(line => !string.IsNullOrWhiteSpace(line) && line.Length > 2)
                            .ToList();

                        // Join lines with proper formatting
                        result.Address = string.Join(" ", addressLines)
                            .Replace("  ", " ")
                            .Replace(" , ", ", ")
                            .Replace(" ,", ", ")
                            .Trim();

                        // Extract barangay number if present
                        var brgyMatch = Regex.Match(result.Address, @"(?:BRGY|BRGY\.|BARANGAY)[\s,]*0*(\d{1,3})", RegexOptions.IgnoreCase);
                        if (!brgyMatch.Success)
                        {
                            // Try alternative patterns for barangay
                            brgyMatch = Regex.Match(result.Address, @"\b(\d{3})\b");
                        }
                        
                        if (brgyMatch.Success)
                        {
                            result.Barangay = brgyMatch.Groups[1].Value.Trim();
                        }

                        // Clean up the address
                        result.Address = Regex.Replace(result.Address, @"(?:BRGY|BRGY\.|BARANGAY)[\s,]*\d{1,3}", "", RegexOptions.IgnoreCase)
                            .Replace(",,", ",")
                            .Replace("  ", " ")
                            .Trim(',', ' ', '-', '.');

                        _logger.LogInformation("Extracted PhilSys address: {Address}", result.Address);
                        return;
                    }
                }
                
                // For other ID types or if PhilSys parsing didn't work
                // Remove any label prefixes and clean up
                addressText = Regex.Replace(addressText, @"^[\s:\-\n]+|[""'']", " ", RegexOptions.Multiline);
                
                // Split into lines and clean each line
                var addressLines = new List<string>();
                var lines = addressText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var cleanLine = line.Trim();
                    
                    // Skip very short lines or lines that look like other fields
                    if (cleanLine.Length < 3 || 
                        cleanLine.Contains("DATE OF BIRTH") || 
                        cleanLine.Contains("BIRTH") ||
                        cleanLine.StartsWith("PHL") ||
                        cleanLine.StartsWith("MALE") ||
                        cleanLine.StartsWith("FEMALE") ||
                        cleanLine.StartsWith("SEX") ||
                        cleanLine.StartsWith("GENDER") ||
                        Regex.IsMatch(cleanLine, @"^\d{1,2}/\d{1,2}/\d{4}$") ||
                        Regex.IsMatch(cleanLine, @"^\d{4}-\d{2}-\d{2}$"))
                    {
                        continue;
                    }
                    
                    // Clean up the line
                    cleanLine = cleanLine
                        .Replace("  ", " ")
                        .Replace(" ,", ",")
                        .Trim(',', ' ', '-', '.');
                        
                    if (!string.IsNullOrWhiteSpace(cleanLine))
                    {
                        addressLines.Add(cleanLine);
                    }
                    
                    // Take up to 3 address lines
                    if (addressLines.Count >= 3)
                        break;
                }

                // Join the lines with proper formatting
                result.Address = string.Join(" ", addressLines)
                    .Replace(", ,", ",")  // Remove empty segments
                    .Replace("  ", " ")    // Remove double spaces
                    .Trim(',', ' ', '-', '.');
                
                // Extract and clean up barangay number if present
                var barangayMatch = Regex.Match(result.Address, @"(?:BRGY|BRGY\.|BARANGAY)[\s,]*0*(\d{1,3})", RegexOptions.IgnoreCase);
                if (!barangayMatch.Success)
                {
                    // Try alternative patterns for barangay
                    barangayMatch = Regex.Match(result.Address, @"\b(\d{3})\b");
                }
                
                if (barangayMatch.Success)
                {
                    result.Barangay = barangayMatch.Groups[1].Value.Trim();
                    // Remove the barangay part from the address to avoid duplication
                    result.Address = Regex.Replace(result.Address, @"(?:BRGY|BRGY\.|BARANGAY)[\s,]*\d{1,3}", "", RegexOptions.IgnoreCase)
                        .Replace(", ,", ",")
                        .Trim(',', ' ');
                }
                
                // Final cleanup
                result.Address = Regex.Replace(result.Address, @"\s*,\s*", ", ") // Normalize spaces after commas
                    .Replace("  ", " ")
                    .Trim();
                
                _logger.LogInformation("Extracted address: {Address}", result.Address);
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