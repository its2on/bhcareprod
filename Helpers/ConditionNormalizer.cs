using System.Globalization;

namespace Barangay.Helpers
{
    /// <summary>
    /// Helper class for normalizing medical condition text to combine similar conditions
    /// regardless of capitalization and spelling variations
    /// </summary>
    public static class ConditionNormalizer
    {
        /// <summary>
        /// Normalizes condition text to combine similar conditions regardless of capitalization
        /// </summary>
        /// <param name="condition">The condition text to normalize</param>
        /// <returns>Normalized condition text</returns>
        public static string NormalizeCondition(string? condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return "Unknown";

            // Trim whitespace and normalize to title case
            var normalized = condition.Trim();
            
            // Handle common variations and normalize to title case
            normalized = normalized.ToLowerInvariant();
            
            // Apply title case (first letter of each word capitalized)
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            normalized = textInfo.ToTitleCase(normalized);
            
            // Handle specific medical term normalizations
            normalized = normalized.Replace("N/A", "Not Specified")
                                 .Replace("Na", "Not Specified")
                                 .Replace("N/a", "Not Specified")
                                 .Replace("N A", "Not Specified");
            
            return normalized;
        }
    }
}




