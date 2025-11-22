using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace ReproApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string text = @"Republic of the Philippines
PHILIPPINE IDENTIFICATION CARD
Pambansang Pagkakakilanlan
Last Name
REBOREDO
Given Names
RHYLLE LANDER
Middle Name
LLONA
Sex
Male
Date of Birth
2008/05/23
Address
391 ALPHA HOMES RUBYVILLE SUBDE, BARANGAY 160, CITY
OF CALOOCAN, NOR, TTHIRD DISTRICT
Date of Issue
2021/02/26";

            Console.WriteLine($"Analyzing text:\n{text}\n");
            
            var barangay = ExtractBarangayNumber(text);
            Console.WriteLine($"Extracted Barangay: '{barangay}'");
        }

        static string ExtractBarangayNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
                
            // Clean up common OCR errors first
            var cleanedText = text.Replace("16I", "161").Replace("16l", "161").Replace("16|", "161").Replace("16O", "160")
                .Replace("181", "161") // Common OCR error: 8 misread as 6
                .Replace("BARANGAY 18", "BARANGAY 16") // Fix partial matches
                .Replace("IGO", "160") // OCR error: "IGO" instead of "160"
                .Replace("BARANICAY IGO", "BARANGAY 160")
                .Replace("SOLE BARANICAY IGO", "BARANGAY 160");
            
            var validBarangays = new[] { "158", "159", "160", "161" };
            
            // Pattern 1: "BARANGAY 161" or "BARANGAY 161," or "BRGY 161" (most specific)
            var pattern1 = @"\b(?:BARANGAY|BRGY|BARANG|BARANICAY)\s*(\d{3})\b";
            var match1 = Regex.Match(cleanedText, pattern1, RegexOptions.IgnoreCase);
            if (match1.Success)
            {
                var barangay = match1.Groups[1].Value.Trim();
                Console.WriteLine($"Matched Pattern 1: {barangay}");
                if (validBarangays.Contains(barangay))
                {
                    return barangay;
                }
            }

            // Pattern 2: "BARANGAY 161" in address context (more lenient, handles 2-3 digits)
            var pattern2 = @"(?:BARANGAY|BRGY|BARANG|BARANICAY)\s*(\d{2,3})";
            var match2 = Regex.Match(cleanedText, pattern2, RegexOptions.IgnoreCase);
            if (match2.Success)
            {
                var barangay = match2.Groups[1].Value.Trim();
                Console.WriteLine($"Matched Pattern 2: {barangay}");
                
                // Handle OCR errors: 16I, 16l, 16| -> 161
                if (barangay == "16" || barangay.StartsWith("16"))
                {
                    // Check if followed by I, l, or | (common OCR errors for 1)
                    var nextCharIndex = match2.Index + match2.Length;
                    if (nextCharIndex < cleanedText.Length)
                    {
                        var nextChar = cleanedText[nextCharIndex];
                        if (nextChar == 'I' || nextChar == 'l' || nextChar == '|')
                        {
                            return "161";
                        }
                        if (nextChar == 'O' || nextChar == '0')
                        {
                            return "160";
                        }
                    }
                    // If just "16" without following character, check context
                    // Look for "160" or "161" patterns nearby
                    var contextStart = Math.Max(0, match2.Index - 20);
                    var contextEnd = Math.Min(cleanedText.Length, match2.Index + match2.Length + 20);
                    var context = cleanedText.Substring(contextStart, contextEnd - contextStart);
                    if (context.Contains("160") || context.Contains("16O"))
                    {
                        return "160";
                    }
                    if (context.Contains("161") || context.Contains("16I") || context.Contains("16l"))
                    {
                        return "161";
                    }
                    // Default to 160 if ambiguous
                    return "160";
                }
                
                // Handle "18" -> "16" (OCR error: 8 misread as 6)
                if (barangay == "18" || barangay.StartsWith("18"))
                {
                    // Check if it's actually "181" (should be "161")
                    if (barangay == "181" || cleanedText.Substring(match2.Index, Math.Min(5, cleanedText.Length - match2.Index)).Contains("181"))
                    {
                        return "161";
                    }
                    // Otherwise might be "180" -> "160"
                    return "160";
                }
                
                // Direct match
                if (validBarangays.Contains(barangay))
                {
                    return barangay;
                }
            }

            // Pattern 3: Look for numbers 158-161 near address keywords (in address lines)
            var pattern3 = @"(?:LT|BLK|BLK1|LT5|ADDRESS|TIRAHAN|BARANG|BRGY|BARANICAY|CITY|REPARO|LIBIS|KALOOKAN|CALOOCAN).*?(158|159|160|161)\b";
            var match3 = Regex.Match(cleanedText, pattern3, RegexOptions.IgnoreCase);
            if (match3.Success)
            {
                var barangay = match3.Groups[1].Value.Trim();
                Console.WriteLine($"Matched Pattern 3: {barangay}");
                if (validBarangays.Contains(barangay))
                {
                    return barangay;
                }
            }
            
            // Pattern 4: Look for "161" or "16I" near "BARANGAY" (handle OCR errors)
            var pattern4 = @"BARANGAY\s*16[1Iil|O]";
            var match4 = Regex.Match(cleanedText, pattern4, RegexOptions.IgnoreCase);
            if (match4.Success)
            {
                Console.WriteLine($"Matched Pattern 4: {match4.Value}");
                // Check the actual character after "16"
                var after16Index = match4.Index + match4.Value.IndexOf("16") + 2;
                if (after16Index < cleanedText.Length)
                {
                    var charAfter16 = cleanedText[after16Index];
                    if (charAfter16 == '1' || charAfter16 == 'I' || charAfter16 == 'l' || charAfter16 == '|')
                    {
                        return "161";
                    }
                    if (charAfter16 == '0' || charAfter16 == 'O')
                    {
                        return "160";
                    }
                }
                return "161"; // Default
            }
            
            // Pattern 5: Look for "IGO" which is OCR error for "160"
            var pattern5 = @"BARANGAY\s*IGO";
            var match5 = Regex.Match(cleanedText, pattern5, RegexOptions.IgnoreCase);
            if (match5.Success)
            {
                Console.WriteLine($"Matched Pattern 5");
                return "160";
            }
            
            // Pattern 6: Look for standalone valid barangay numbers in address context
            // This handles cases where "BARANGAY" keyword might be missing or garbled
            var pattern6 = @"(?:^|\s|,)(158|159|160|161)(?:\s|,|$|\.)";
            var matches6 = Regex.Matches(cleanedText, pattern6, RegexOptions.IgnoreCase);
            foreach (Match match in matches6)
            {
                var barangay = match.Groups[1].Value.Trim();
                Console.WriteLine($"Matched Pattern 6: {barangay}");
                // Check context - should be near address-related words
                var contextStart = Math.Max(0, match.Index - 100);
                var contextEnd = Math.Min(cleanedText.Length, match.Index + match.Length + 100);
                var context = cleanedText.Substring(contextStart, contextEnd - contextStart).ToUpper();
                
                // Should be near address keywords, not near name/date fields
                if ((context.Contains("BARANGAY") || context.Contains("CITY") || context.Contains("ADDRESS") ||
                     context.Contains("LT") || context.Contains("BLK") || context.Contains("REPARO") ||
                     context.Contains("LIBIS") || context.Contains("KALOOKAN") || context.Contains("CALOOCAN")) &&
                    !context.Contains("NAME") && !context.Contains("DATE OF BIRTH") && !context.Contains("BIRTH DATE"))
                {
                    return barangay;
                }
            }

            return "";
        }
    }
}
