using System;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Barangay.Models;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;

namespace Barangay.Services
{
    public interface IDataEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        bool CanUserDecrypt(ClaimsPrincipal user);
        string DecryptForUser(string cipherText, ClaimsPrincipal user);
        bool IsEncrypted(string text);
    }

    public class DataEncryptionService : IDataEncryptionService
    {
        private readonly string _encryptionKey;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DataEncryptionService(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _encryptionKey = configuration["DataEncryption:Key"] ?? 
                           Environment.GetEnvironmentVariable("BHCARE_ENCRYPTION_KEY");
            
            if (string.IsNullOrEmpty(_encryptionKey))
            {
                throw new InvalidOperationException("Encryption key not found. Please set DataEncryption:Key in appsettings.json or BHCARE_ENCRYPTION_KEY environment variable.");
            }
            
            if (_encryptionKey.Length < 32)
            {
                _encryptionKey = _encryptionKey.PadRight(32, '0');
            }
            else if (_encryptionKey.Length > 32)
            {
                _encryptionKey = _encryptionKey.Substring(0, 32);
            }

            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _context = context;
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
                    aes.GenerateIV();

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var plainBytes = Encoding.UTF8.GetBytes(plainText);
                        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                        // Combine IV and encrypted data
                        var result = new byte[aes.IV.Length + encryptedBytes.Length];
                        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

                        return Convert.ToBase64String(result);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't expose sensitive information
                Console.WriteLine($"Encryption error: {ex.Message}");
                return plainText; // Return original text if encryption fails
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                Console.WriteLine($"Decrypt called with: {cipherText?.Substring(0, Math.Min(20, cipherText?.Length ?? 0))}...");
                Console.WriteLine($"IsEncrypted: {IsEncrypted(cipherText)}");
                
                // Try to decrypt - if it fails, the catch block will handle it
                var encryptedBytes = Convert.FromBase64String(cipherText);

                // Try with the new DataEncryption key first
                try
                {
                    using (var aes = Aes.Create())
                    {
                        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);

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
                            var result = Encoding.UTF8.GetString(decryptedBytes);
                            Console.WriteLine($"Decrypt successful: {result?.Substring(0, Math.Min(20, result?.Length ?? 0))}...");
                            return result;
                        }
                    }
                }
                catch (CryptographicException)
                {
                    Console.WriteLine("Decrypt: DataEncryption key failed, trying legacy encryption key");
                    
                    // Fallback: Try with the legacy EncryptionKey from appsettings
                    var legacyKey = Environment.GetEnvironmentVariable("LEGACY_ENCRYPTION_KEY") ?? 
                                   "YourStrongEncryptionKeyHere1234567890123456";
                    
                    // Normalize legacy key length
                    if (legacyKey.Length < 32)
                        legacyKey = legacyKey.PadRight(32, '0');
                    else if (legacyKey.Length > 32)
                        legacyKey = legacyKey.Substring(0, 32);
                    
                    using (var aes = Aes.Create())
                    {
                        aes.Key = Encoding.UTF8.GetBytes(legacyKey);

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
                            var result = Encoding.UTF8.GetString(decryptedBytes);
                            Console.WriteLine($"Legacy decrypt successful: {result?.Substring(0, Math.Min(20, result?.Length ?? 0))}...");
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't expose sensitive information
                Console.WriteLine($"Decryption error: {ex.Message}");
                
                // Additional fallback: Try with alternative key formats
                try
                {
                    Console.WriteLine("Decrypt: Trying alternative key formats...");
                    return TryAlternativeDecryption(cipherText);
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"Alternative decryption also failed: {fallbackEx.Message}");
                    return cipherText; // Return original text if all decryption attempts fail
                }
            }
        }
        
        private string TryAlternativeDecryption(string cipherText)
        {
            var encryptedBytes = Convert.FromBase64String(cipherText);
            
            // Try with different key variations and methods
            var alternativeKeys = new[]
            {
                "BHCARE_DataEncryption_Key_2024_Secure_32Chars".PadRight(32, '0'),
                "YourStrongEncryptionKeyHere1234567890123456".PadRight(32, '0'),
                "BHCARE_DataEncryption_Key_2024_Secure_32Chars".Substring(0, Math.Min(32, "BHCARE_DataEncryption_Key_2024_Secure_32Chars".Length)).PadRight(32, '0'),
                "YourStrongEncryptionKeyHere1234567890123456".Substring(0, Math.Min(32, "YourStrongEncryptionKeyHere1234567890123456".Length)).PadRight(32, '0')
            };
            
            // Try standard AES-CBC decryption with different keys
            foreach (var key in alternativeKeys)
            {
                try
                {
                    using (var aes = Aes.Create())
                    {
                        aes.Key = Encoding.UTF8.GetBytes(key);
                        
                        var iv = new byte[16];
                        Buffer.BlockCopy(encryptedBytes, 0, iv, 0, iv.Length);
                        aes.IV = iv;
                        
                        var encryptedData = new byte[encryptedBytes.Length - iv.Length];
                        Buffer.BlockCopy(encryptedBytes, iv.Length, encryptedData, 0, encryptedData.Length);
                        
                        using (var decryptor = aes.CreateDecryptor())
                        {
                            var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                            var result = Encoding.UTF8.GetString(decryptedBytes);
                            Console.WriteLine($"Alternative key decrypt successful: {result?.Substring(0, Math.Min(20, result?.Length ?? 0))}...");
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Alternative key failed: {ex.Message}");
                    continue;
                }
            }
            
            // Try the older EncryptionService method (different key handling)
            try
            {
                Console.WriteLine("Trying older EncryptionService method...");
                return TryLegacyEncryptionServiceMethod(cipherText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Legacy EncryptionService method failed: {ex.Message}");
            }
            
            throw new Exception("All alternative decryption methods failed");
        }
        
        private string TryLegacyEncryptionServiceMethod(string cipherText)
        {
            var encryptedBytes = Convert.FromBase64String(cipherText);
            
            if (encryptedBytes.Length < 16)
            {
                throw new Exception("Cipher too short");
            }
            
            // Try with the legacy EncryptionKey from appsettings
            var legacyKey = Environment.GetEnvironmentVariable("LEGACY_ENCRYPTION_KEY") ?? 
                           "YourStrongEncryptionKeyHere1234567890123456";
            
            // Use the same key handling as the old EncryptionService
            byte[] keyBytes = new byte[32]; // Using 256 bits (32 bytes)
            byte[] existingKeyBytes = Encoding.UTF8.GetBytes(legacyKey);
            
            // Copy the existing key bytes, padding or truncating as needed
            Array.Copy(existingKeyBytes, keyBytes, Math.Min(existingKeyBytes.Length, keyBytes.Length));
            
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                
                // Extract IV and encrypted data
                byte[] iv = new byte[16];
                Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                byte[] cipherBytes = new byte[encryptedBytes.Length - iv.Length];
                Array.Copy(encryptedBytes, iv.Length, cipherBytes, 0, cipherBytes.Length);
                
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
                        cryptoStream.FlushFinalBlock();
                        var result = Encoding.UTF8.GetString(memoryStream.ToArray());
                        Console.WriteLine($"Legacy method decrypt successful: {result?.Substring(0, Math.Min(20, result?.Length ?? 0))}...");
                        return result;
                    }
                }
            }
        }

        public bool CanUserDecrypt(ClaimsPrincipal user)
        {
            if (user == null || !user.Identity.IsAuthenticated)
                return false;

            // Check if user has authorized roles - include all roles that should be able to decrypt their own data
            var roles = new[] { "Admin", "Doctor", "Nurse", "System Administrator", "User", "Patient", "Head Doctor", "Head Nurse" };
            return roles.Any(role => user.IsInRole(role));
        }

        public string DecryptForUser(string cipherText, ClaimsPrincipal user)
        {
            Console.WriteLine($"DecryptForUser called with cipherText: {cipherText?.Substring(0, Math.Min(20, cipherText?.Length ?? 0))}...");
            Console.WriteLine($"CanUserDecrypt: {CanUserDecrypt(user)}");
            
            if (!CanUserDecrypt(user))
            {
                Console.WriteLine("DecryptForUser: Access denied");
                return "[ACCESS DENIED]";
            }

            var result = Decrypt(cipherText);
            Console.WriteLine($"DecryptForUser result: {result?.Substring(0, Math.Min(20, result?.Length ?? 0))}...");
            return result;
        }

        public bool IsEncrypted(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            try
            {
                // Check if the text is a valid Base64 string and has minimum length for encrypted data
                var bytes = Convert.FromBase64String(text);
                return bytes.Length >= 16; // Minimum length for IV + some encrypted data
            }
            catch
            {
                return false;
            }
        }
    }
}
