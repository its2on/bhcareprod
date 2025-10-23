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
                return cipherText; // Return original text if decryption fails
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
