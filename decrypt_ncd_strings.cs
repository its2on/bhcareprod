using System;
using System.Security.Cryptography;
using System.Text;

namespace Barangay.Tools
{
    public class NCDStringDecryptor
    {
        public static void DecryptStrings()
        {
            Console.WriteLine("=== NCD Assessment String Decryptor ===");
            Console.WriteLine();

            // Encrypted strings from the user's images
            string[] encryptedStrings = {
                 // ID No from the first image
                "LBDLmQqACSX3t+X/5n54NEI6fWXJM28mLYVEvUnIXAkax0w8D5CWOyi",
                
                // ID No from the second image  
                "LBDLmQqACSX3t+X/5n54NEI6WXJM28mLYVEvUnIXAkax0w6D5CWOy7M7prER59abCvdfRW0y1CmFfV+telS9w==",
                
                // Assessment Date from the second image
                "fimzPfiFKUXg7io1zZQno8dctWgaS3n9TB5TCs6zqRnVbEs2cu6yxJnfxXW0+CpNIOZOVynsSbTErtfHV0IQRTDtiX28HAOuj7lq4PISsBvf4Bn8B7A2Ps6ewFlowKUg"
            };

            string[] stringLabels = {
                "ID No (Image 1 - Creation Form)",
                "ID No (Image 2 - Assessment View)", 
                "Assessment Date (Image 2 - Assessment View)"
            };

            for (int i = 0; i < encryptedStrings.Length; i++)
            {
                Console.WriteLine($"{stringLabels[i]}:");
                Console.WriteLine($"Encrypted: {encryptedStrings[i]}");
                Console.WriteLine($"Decrypted: {DecryptNCDValue(encryptedStrings[i])}");
                Console.WriteLine();
            }
        }

        public static string DecryptNCDValue(string encryptedValue)
        {
            if (string.IsNullOrEmpty(encryptedValue))
                return encryptedValue;

            try
            {
                // All possible encryption keys found in the codebase
                string[] possibleKeys = {
                    "YourStrongEncryptionKeyHere1234567890123456", // Legacy key
                    "BHCARE_ENCRYPTION_KEY_FOR_PRODUCTION_SYSTEM", // Production
                    "BarangayHealthCareEncryptionSystem2024", // System-specific
                    "NCDAssessmentEncryptionKey2024ForPH", // NCD-specific
                    "BHCARE_2024_SECRET_KEY_32BYTES_LONG", // Found in NCDRiskAssessment.cshtml.cs
                    Environment.GetEnvironmentVariable("BHCARE_ENCRYPTION_KEY") ?? "",
                    Environment.GetEnvironmentVariable("DataEncryption:Key") ?? "",
                    Environment.GetEnvironmentVariable("LEGACY_ENCRYPTION_KEY") ?? ""
                };

                foreach (var key in possibleKeys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        var result = TryDecryptWithKey(encryptedValue, key);
                        if (!string.IsNullOrEmpty(result) && result != encryptedValue && result != "[ACCESS DENIED]" && IsValidDecryption(result))
                        {
                            Console.WriteLine($" Successfully decrypted with key: {key.Substring(0, Math.Min(30, key.Length))}...");
                            return result;
                        }
                    }
                }

                return "[DECRYPTION FAILED - No valid key found]";
            }
            catch (Exception ex)
            {
                return $"[ERROR: {ex.Message}]";
            }
        }

        private static string TryDecryptWithKey(string cipherText, string key)
        {
            try
            {
                // Check if it's valid Base64 and long enough for AES encryption
                var encryptedBytes = Convert.FromBase64String(cipherText);
                if (encryptedBytes.Length < 16) // Too short for AES IV + data
                    return null;

                // Normalize key length to 32 bytes for AES-256
                string normalizedKey = NormalizeKey(key);

                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(normalizedKey);

                    // Extract IV from the beginning of the encrypted data
                    var iv = new byte[16];
                    Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    // Extract encrypted data
                    var encryptedData = new byte[encryptedBytes.Length - iv.Length];
                    Buffer.BlockCopy(encryptedBytes, iv.Length, encryptedData, 0, encryptedData.Length);

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch
            {
                return null; // This key failed
            }
        }

        private static string NormalizeKey(string key)
        {
            if (key.Length < 32)
                return key.PadRight(32, '0');
            else if (key.Length > 32)
                return key.Substring(0, 32);
            return key;
        }

        private static bool IsValidDecryption(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // Check if it contains printable characters and looks like readable data
            foreach (char c in text)
            {
                if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                    return false;
            }

            // Additional validation - should not be binary-like data
            int printableCount = 0;
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSeparator(c) || char.IsWhiteSpace(c))
                    printableCount++;
            }

            return printableCount > text.Length * 0.7; // At least 70% printable characters
        }
    }
}

