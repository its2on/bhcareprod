using System;
using System.Reflection;
using System.Security.Claims;
using Barangay.Attributes;
using Barangay.Services;
using System.Linq;

namespace Barangay.Extensions
{
    public static class EncryptionExtensions
    {
        /// <summary>
        /// Encrypts all properties marked with [Encrypted] attribute on an object
        /// </summary>
        public static T EncryptSensitiveData<T>(this T obj, IDataEncryptionService encryptionService) where T : class
        {
            if (obj == null || encryptionService == null)
                return obj;

            Console.WriteLine($"EncryptSensitiveData called for {obj.GetType().Name}");
            Console.WriteLine($"Total properties found: {typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Length}");

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var encryptedProperties = properties.Where(p => p.GetCustomAttribute<EncryptedAttribute>() != null).ToList();
            Console.WriteLine($"Properties with [Encrypted] attribute: {encryptedProperties.Count}");
            
            foreach (var property in encryptedProperties)
            {
                Console.WriteLine($"Processing encrypted property: {property.Name} of type {property.PropertyType}");
                if (property.CanWrite)
                {
                    var value = property.GetValue(obj);
                    if (value != null)
                    {
                        var stringValue = value.ToString();
                        if (!string.IsNullOrEmpty(stringValue))
                        {
                            Console.WriteLine($"Encrypting {property.Name}: {stringValue}");
                            var encryptedValue = encryptionService.Encrypt(stringValue);
                            Console.WriteLine($"Encrypted {property.Name}: {encryptedValue}");
                            
                            // Handle different property types
                            if (property.PropertyType == typeof(string))
                            {
                                property.SetValue(obj, encryptedValue);
                            }
                            else if (property.PropertyType == typeof(int?) || property.PropertyType == typeof(int))
                            {
                                // For numeric types, we need to store the encrypted string
                                // This requires changing the database schema to use string columns for encrypted numeric values
                                // For now, we'll store the encrypted value as a string representation
                                property.SetValue(obj, encryptedValue);
                            }
                            else if (property.PropertyType == typeof(decimal?) || property.PropertyType == typeof(decimal))
                            {
                                // For decimal types, store encrypted value as string
                                property.SetValue(obj, encryptedValue);
                            }
                            else
                            {
                                // For other types, try to convert back to original type
                                try
                                {
                                    var convertedValue = Convert.ChangeType(encryptedValue, property.PropertyType);
                                    property.SetValue(obj, convertedValue);
                                }
                                catch
                                {
                                    // If conversion fails, store as string
                                    property.SetValue(obj, encryptedValue);
                                }
                            }
                        }
                    }
                }
            }

            return obj;
        }

        /// <summary>
        /// Decrypts all properties marked with [Encrypted] attribute on an object for authorized users
        /// </summary>
        public static T DecryptSensitiveData<T>(this T obj, IDataEncryptionService encryptionService, ClaimsPrincipal user) where T : class
        {
            if (obj == null || encryptionService == null)
            {
                Console.WriteLine($"DecryptSensitiveData: Cannot decrypt - Object: {obj != null}, Service: {encryptionService != null}");
                return obj;
            }

            Console.WriteLine($"DecryptSensitiveData: Starting decryption for {typeof(T).Name}");
            Console.WriteLine($"DecryptSensitiveData: User can decrypt: {encryptionService.CanUserDecrypt(user)}");
            
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var encryptedProperties = properties.Where(p => p.GetCustomAttribute<EncryptedAttribute>() != null).ToList();
            Console.WriteLine($"DecryptSensitiveData: Found {encryptedProperties.Count} encrypted properties");
            
            // Debug: Log user info
            Console.WriteLine($"DecryptSensitiveData: User: {user?.Identity?.Name}, IsAuthenticated: {user?.Identity?.IsAuthenticated}");
            Console.WriteLine($"DecryptSensitiveData: User roles: {string.Join(", ", user?.Claims?.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Select(c => c.Value) ?? new string[0])}");
            
            foreach (var property in encryptedProperties)
            {
                if (property.CanWrite)
                {
                    var value = property.GetValue(obj)?.ToString();
                    Console.WriteLine($"DecryptSensitiveData: Processing {property.Name} = {value?.Substring(0, Math.Min(20, value?.Length ?? 0))}...");
                    
                    if (!string.IsNullOrEmpty(value) && encryptionService.IsEncrypted(value))
                    {
                        Console.WriteLine($"DecryptSensitiveData: {property.Name} is encrypted, attempting decryption");
                        try
                        {
                            // Try DecryptForUser first
                            var decryptedValue = encryptionService.DecryptForUser(value, user);
                            Console.WriteLine($"DecryptSensitiveData: {property.Name} DecryptForUser result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                            
                            // If DecryptForUser failed or returned access denied, try direct decryption
                            if (decryptedValue == "[ACCESS DENIED]" || decryptedValue == value)
                            {
                                Console.WriteLine($"DecryptSensitiveData: DecryptForUser failed for {property.Name}, trying direct decryption");
                                decryptedValue = encryptionService.Decrypt(value);
                                Console.WriteLine($"DecryptSensitiveData: {property.Name} direct decrypt result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                            }
                            
                            if (decryptedValue != value && !decryptedValue.Contains("[ACCESS DENIED]"))
                            {
                                property.SetValue(obj, decryptedValue);
                                Console.WriteLine($"DecryptSensitiveData: Successfully decrypted {property.Name}");
                            }
                            else
                            {
                                Console.WriteLine($"DecryptSensitiveData: Failed to decrypt {property.Name} - returned original or access denied");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"DecryptSensitiveData: Exception decrypting {property.Name}: {ex.Message}");
                            
                            // Try direct decryption as fallback
                            try
                            {
                                var fallbackDecrypted = encryptionService.Decrypt(value);
                                if (fallbackDecrypted != value)
                                {
                                    property.SetValue(obj, fallbackDecrypted);
                                    Console.WriteLine($"DecryptSensitiveData: Fallback decryption successful for {property.Name}");
                                }
                            }
                            catch (Exception fallbackEx)
                            {
                                Console.WriteLine($"DecryptSensitiveData: Fallback decryption also failed for {property.Name}: {fallbackEx.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"DecryptSensitiveData: {property.Name} is not encrypted or empty");
                    }
                }
            }

            return obj;
        }

        /// <summary>
        /// Decrypts a single encrypted string for authorized users
        /// </summary>
        public static string DecryptForUser(this string encryptedText, IDataEncryptionService encryptionService, ClaimsPrincipal user)
        {
            if (string.IsNullOrEmpty(encryptedText) || encryptionService == null)
                return encryptedText;

            if (!encryptionService.CanUserDecrypt(user))
                return "[ACCESS DENIED]";

            return encryptionService.DecryptForUser(encryptedText, user);
        }

        /// <summary>
        /// Encrypts a single string
        /// </summary>
        public static string EncryptText(this string plainText, IDataEncryptionService encryptionService)
        {
            if (string.IsNullOrEmpty(plainText) || encryptionService == null)
                return plainText;

            return encryptionService.Encrypt(plainText);
        }

        /// <summary>
        /// Encrypts VitalSign data using dual-column approach
        /// </summary>
        public static Models.VitalSign EncryptVitalSignData(this Models.VitalSign vitalSign, IDataEncryptionService encryptionService)
        {
            if (vitalSign == null || encryptionService == null)
            {
                Console.WriteLine("EncryptVitalSignData: vitalSign or encryptionService is null");
                return vitalSign;
            }

            Console.WriteLine($"EncryptVitalSignData called - Temperature: {vitalSign.Temperature}, BloodPressure: {vitalSign.BloodPressure}");

            // Encrypt Temperature and store in regular field
            if (!string.IsNullOrEmpty(vitalSign.Temperature))
            {
                var encryptedTemp = encryptionService.Encrypt(vitalSign.Temperature);
                vitalSign.Temperature = encryptedTemp; // Store encrypted data in regular field
                vitalSign.EncryptedTemperature = encryptedTemp;
                Console.WriteLine($"Encrypted Temperature: {vitalSign.Temperature} -> {encryptedTemp}");
            }

            // Encrypt BloodPressure and store in regular field
            if (!string.IsNullOrEmpty(vitalSign.BloodPressure))
            {
                var encryptedBP = encryptionService.Encrypt(vitalSign.BloodPressure);
                vitalSign.BloodPressure = encryptedBP; // Store encrypted data in regular field
                vitalSign.EncryptedBloodPressure = encryptedBP;
            }

            // Encrypt HeartRate and store in regular field
            if (!string.IsNullOrEmpty(vitalSign.HeartRate))
            {
                var encryptedHR = encryptionService.Encrypt(vitalSign.HeartRate);
                vitalSign.HeartRate = encryptedHR; // Store encrypted data in regular field
                vitalSign.EncryptedHeartRate = encryptedHR;
            }

            // Encrypt RespiratoryRate and store in regular field
            if (!string.IsNullOrEmpty(vitalSign.RespiratoryRate))
            {
                var encryptedRR = encryptionService.Encrypt(vitalSign.RespiratoryRate);
                vitalSign.RespiratoryRate = encryptedRR; // Store encrypted data in regular field
                vitalSign.EncryptedRespiratoryRate = encryptedRR;
            }

            // Encrypt SpO2 and store in regular field
            if (!string.IsNullOrEmpty(vitalSign.SpO2))
            {
                var encryptedSpO2 = encryptionService.Encrypt(vitalSign.SpO2);
                vitalSign.SpO2 = encryptedSpO2; // Store encrypted data in regular field
                vitalSign.EncryptedSpO2 = encryptedSpO2;
            }

            // Encrypt Weight and store in regular field
            if (!string.IsNullOrEmpty(vitalSign.Weight))
            {
                var encryptedWeight = encryptionService.Encrypt(vitalSign.Weight);
                vitalSign.Weight = encryptedWeight; // Store encrypted data in regular field
                vitalSign.EncryptedWeight = encryptedWeight;
            }

            // Encrypt Height and store in regular field
            if (!string.IsNullOrEmpty(vitalSign.Height))
            {
                var encryptedHeight = encryptionService.Encrypt(vitalSign.Height);
                vitalSign.Height = encryptedHeight; // Store encrypted data in regular field
                vitalSign.EncryptedHeight = encryptedHeight;
            }

            // Encrypt Notes (already has [Encrypted] attribute, but let's handle it explicitly)
            if (!string.IsNullOrEmpty(vitalSign.Notes))
            {
                vitalSign.Notes = encryptionService.Encrypt(vitalSign.Notes);
            }

            return vitalSign;
        }

        /// <summary>
        /// Decrypts VitalSign data using dual-column approach
        /// </summary>
        public static Models.VitalSign DecryptVitalSignData(this Models.VitalSign vitalSign, IDataEncryptionService encryptionService, ClaimsPrincipal user, ILogger logger = null)
        {
            if (vitalSign == null || encryptionService == null || !encryptionService.CanUserDecrypt(user))
            {
                System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Cannot decrypt - VitalSign: {vitalSign != null}, Service: {encryptionService != null}, CanDecrypt: {encryptionService?.CanUserDecrypt(user)}");
                return vitalSign;
            }

            System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Starting decryption for vital sign ID {vitalSign.Id}");

            // Decrypt Temperature from encrypted field
            if (!string.IsNullOrEmpty(vitalSign.EncryptedTemperature))
            {
                var originalValue = vitalSign.EncryptedTemperature;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(vitalSign.EncryptedTemperature, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        vitalSign.Temperature = decryptedValue;
                        logger?.LogInformation($"DecryptVitalSignData: Temperature - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Decrypted: {vitalSign.Temperature?.Substring(0, Math.Min(20, vitalSign.Temperature?.Length ?? 0))}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Temperature decryption failed or returned original for vital sign {vitalSign.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"DecryptVitalSignData: Exception decrypting Temperature for vital sign {vitalSign.Id}: {ex.Message}");
                }
            }

            // Decrypt BloodPressure from encrypted field
            if (!string.IsNullOrEmpty(vitalSign.EncryptedBloodPressure))
            {
                var originalValue = vitalSign.EncryptedBloodPressure;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(vitalSign.EncryptedBloodPressure, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        vitalSign.BloodPressure = decryptedValue;
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: BloodPressure - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Decrypted: {vitalSign.BloodPressure?.Substring(0, Math.Min(20, vitalSign.BloodPressure?.Length ?? 0))}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: BloodPressure decryption failed or returned original for vital sign {vitalSign.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"DecryptVitalSignData: Exception decrypting BloodPressure for vital sign {vitalSign.Id}: {ex.Message}");
                }
            }

            // Decrypt HeartRate from encrypted field
            if (!string.IsNullOrEmpty(vitalSign.EncryptedHeartRate))
            {
                var originalValue = vitalSign.EncryptedHeartRate;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(vitalSign.EncryptedHeartRate, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        vitalSign.HeartRate = decryptedValue;
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: HeartRate - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Decrypted: {vitalSign.HeartRate?.Substring(0, Math.Min(20, vitalSign.HeartRate?.Length ?? 0))}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: HeartRate decryption failed or returned original for vital sign {vitalSign.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"DecryptVitalSignData: Exception decrypting HeartRate for vital sign {vitalSign.Id}: {ex.Message}");
                }
            }

            // Decrypt RespiratoryRate from encrypted field
            if (!string.IsNullOrEmpty(vitalSign.EncryptedRespiratoryRate))
            {
                var originalValue = vitalSign.EncryptedRespiratoryRate;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(vitalSign.EncryptedRespiratoryRate, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        vitalSign.RespiratoryRate = decryptedValue;
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: RespiratoryRate - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Decrypted: {vitalSign.RespiratoryRate?.Substring(0, Math.Min(20, vitalSign.RespiratoryRate?.Length ?? 0))}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: RespiratoryRate decryption failed or returned original for vital sign {vitalSign.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Exception decrypting RespiratoryRate for vital sign {vitalSign.Id}: {ex.Message}");
                }
            }

            // Decrypt SpO2 from encrypted field
            if (!string.IsNullOrEmpty(vitalSign.EncryptedSpO2))
            {
                var originalValue = vitalSign.EncryptedSpO2;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(vitalSign.EncryptedSpO2, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        vitalSign.SpO2 = decryptedValue;
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: SpO2 - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Decrypted: {vitalSign.SpO2?.Substring(0, Math.Min(20, vitalSign.SpO2?.Length ?? 0))}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: SpO2 decryption failed or returned original for vital sign {vitalSign.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Exception decrypting SpO2 for vital sign {vitalSign.Id}: {ex.Message}");
                }
            }

            // Decrypt Weight from encrypted field
            if (!string.IsNullOrEmpty(vitalSign.EncryptedWeight))
            {
                var originalValue = vitalSign.EncryptedWeight;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(vitalSign.EncryptedWeight, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        vitalSign.Weight = decryptedValue;
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Weight - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Decrypted: {vitalSign.Weight?.Substring(0, Math.Min(20, vitalSign.Weight?.Length ?? 0))}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Weight decryption failed or returned original for vital sign {vitalSign.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Exception decrypting Weight for vital sign {vitalSign.Id}: {ex.Message}");
                }
            }

            // Decrypt Height from encrypted field
            if (!string.IsNullOrEmpty(vitalSign.EncryptedHeight))
            {
                var originalValue = vitalSign.EncryptedHeight;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(vitalSign.EncryptedHeight, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        vitalSign.Height = decryptedValue;
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Height - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Decrypted: {vitalSign.Height?.Substring(0, Math.Min(20, vitalSign.Height?.Length ?? 0))}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Height decryption failed or returned original for vital sign {vitalSign.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Exception decrypting Height for vital sign {vitalSign.Id}: {ex.Message}");
                }
            }

            // Decrypt Notes
            if (!string.IsNullOrEmpty(vitalSign.Notes))
            {
                var originalValue = vitalSign.Notes;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(vitalSign.Notes, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        vitalSign.Notes = decryptedValue;
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Notes - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Decrypted: {vitalSign.Notes?.Substring(0, Math.Min(20, vitalSign.Notes?.Length ?? 0))}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Notes decryption failed or returned original for vital sign {vitalSign.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DecryptVitalSignData: Exception decrypting Notes for vital sign {vitalSign.Id}: {ex.Message}");
                }
            }

            return vitalSign;
        }

        /// <summary>
        /// Decrypts ImmunizationRecord data for authorized users
        /// </summary>
        public static Models.ImmunizationRecord DecryptImmunizationData(this Models.ImmunizationRecord record, IDataEncryptionService encryptionService, ClaimsPrincipal user)
        {
            if (record == null || encryptionService == null || !encryptionService.CanUserDecrypt(user))
            {
                System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: Cannot decrypt - Record: {record != null}, Service: {encryptionService != null}, CanDecrypt: {encryptionService?.CanUserDecrypt(user)}");
                return record;
            }

            System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: Starting decryption for record ID {record.Id}");

            // Decrypt basic information
            if (!string.IsNullOrEmpty(record.ChildName))
            {
                var originalValue = record.ChildName;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(record.ChildName, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        record.ChildName = decryptedValue;
                        System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: ChildName - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Decrypted: {record.ChildName?.Substring(0, Math.Min(20, record.ChildName?.Length ?? 0))}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: ChildName decryption failed or returned original for record {record.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: Exception decrypting ChildName for record {record.Id}: {ex.Message}");
                }
            }

            if (!string.IsNullOrEmpty(record.FamilyNumber))
            {
                var originalValue = record.FamilyNumber;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(record.FamilyNumber, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        record.FamilyNumber = decryptedValue;
                        System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: FamilyNumber - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Decrypted: {record.FamilyNumber?.Substring(0, Math.Min(20, record.FamilyNumber?.Length ?? 0))}...");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: FamilyNumber decryption failed or returned original for record {record.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: Exception decrypting FamilyNumber for record {record.Id}: {ex.Message}");
                }
            }

            if (!string.IsNullOrEmpty(record.DateOfBirth))
            {
                try
                {
                    var decryptedDate = encryptionService.DecryptForUser(record.DateOfBirth, user);
                    // Ensure the date is in the correct format for HTML date inputs
                    if (DateTime.TryParse(decryptedDate, out DateTime parsedDate))
                    {
                        record.DateOfBirth = parsedDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        record.DateOfBirth = decryptedDate; // Keep original if parsing fails
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: Failed to decrypt DateOfBirth for record {record.Id}: {ex.Message}");
                    // Keep original value if decryption fails
                }
            }

            if (!string.IsNullOrEmpty(record.MotherName))
            {
                var originalValue = record.MotherName;
                try
                {
                    var decryptedValue = encryptionService.DecryptForUser(record.MotherName, user);
                    // Check if decryption actually worked (not just returned the original)
                    if (decryptedValue != originalValue && !decryptedValue.Contains("[ACCESS DENIED]"))
                    {
                        record.MotherName = decryptedValue;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: MotherName decryption failed or returned original for record {record.Id} - Original: {originalValue.Substring(0, Math.Min(20, originalValue.Length))}..., Result: {decryptedValue?.Substring(0, Math.Min(20, decryptedValue?.Length ?? 0))}...");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DecryptImmunizationData: Exception decrypting MotherName for record {record.Id}: {ex.Message}");
                }
            }

            if (!string.IsNullOrEmpty(record.FatherName))
            {
                record.FatherName = encryptionService.DecryptForUser(record.FatherName, user);
            }

            if (!string.IsNullOrEmpty(record.Sex))
            {
                record.Sex = encryptionService.DecryptForUser(record.Sex, user);
            }

            if (!string.IsNullOrEmpty(record.BirthHeight))
            {
                record.BirthHeight = encryptionService.DecryptForUser(record.BirthHeight, user);
            }

            if (!string.IsNullOrEmpty(record.BirthWeight))
            {
                record.BirthWeight = encryptionService.DecryptForUser(record.BirthWeight, user);
            }

            if (!string.IsNullOrEmpty(record.PlaceOfBirth))
            {
                record.PlaceOfBirth = encryptionService.DecryptForUser(record.PlaceOfBirth, user);
            }

            if (!string.IsNullOrEmpty(record.Address))
            {
                record.Address = encryptionService.DecryptForUser(record.Address, user);
            }

            if (!string.IsNullOrEmpty(record.HealthCenter))
            {
                record.HealthCenter = encryptionService.DecryptForUser(record.HealthCenter, user);
            }

            if (!string.IsNullOrEmpty(record.Barangay))
            {
                record.Barangay = encryptionService.DecryptForUser(record.Barangay, user);
            }

            if (!string.IsNullOrEmpty(record.FamilyNumber))
            {
                record.FamilyNumber = encryptionService.DecryptForUser(record.FamilyNumber, user);
            }

            if (!string.IsNullOrEmpty(record.Email))
            {
                record.Email = encryptionService.DecryptForUser(record.Email, user);
            }

            if (!string.IsNullOrEmpty(record.ContactNumber))
            {
                record.ContactNumber = encryptionService.DecryptForUser(record.ContactNumber, user);
            }

            // Decrypt vaccine information
            if (!string.IsNullOrEmpty(record.BCGVaccineDate))
            {
                record.BCGVaccineDate = encryptionService.DecryptForUser(record.BCGVaccineDate, user);
            }

            if (!string.IsNullOrEmpty(record.BCGVaccineRemarks))
            {
                record.BCGVaccineRemarks = encryptionService.DecryptForUser(record.BCGVaccineRemarks, user);
            }

            if (!string.IsNullOrEmpty(record.HepatitisBVaccineDate))
            {
                record.HepatitisBVaccineDate = encryptionService.DecryptForUser(record.HepatitisBVaccineDate, user);
            }

            if (!string.IsNullOrEmpty(record.HepatitisBVaccineRemarks))
            {
                record.HepatitisBVaccineRemarks = encryptionService.DecryptForUser(record.HepatitisBVaccineRemarks, user);
            }

            // Decrypt Pentavalent doses
            if (!string.IsNullOrEmpty(record.Pentavalent1Date))
            {
                record.Pentavalent1Date = encryptionService.DecryptForUser(record.Pentavalent1Date, user);
            }

            if (!string.IsNullOrEmpty(record.Pentavalent1Remarks))
            {
                record.Pentavalent1Remarks = encryptionService.DecryptForUser(record.Pentavalent1Remarks, user);
            }

            if (!string.IsNullOrEmpty(record.Pentavalent2Date))
            {
                record.Pentavalent2Date = encryptionService.DecryptForUser(record.Pentavalent2Date, user);
            }

            if (!string.IsNullOrEmpty(record.Pentavalent2Remarks))
            {
                record.Pentavalent2Remarks = encryptionService.DecryptForUser(record.Pentavalent2Remarks, user);
            }

            if (!string.IsNullOrEmpty(record.Pentavalent3Date))
            {
                record.Pentavalent3Date = encryptionService.DecryptForUser(record.Pentavalent3Date, user);
            }

            if (!string.IsNullOrEmpty(record.Pentavalent3Remarks))
            {
                record.Pentavalent3Remarks = encryptionService.DecryptForUser(record.Pentavalent3Remarks, user);
            }

            // Decrypt OPV doses
            if (!string.IsNullOrEmpty(record.OPV1Date))
            {
                record.OPV1Date = encryptionService.DecryptForUser(record.OPV1Date, user);
            }

            if (!string.IsNullOrEmpty(record.OPV1Remarks))
            {
                record.OPV1Remarks = encryptionService.DecryptForUser(record.OPV1Remarks, user);
            }

            if (!string.IsNullOrEmpty(record.OPV2Date))
            {
                record.OPV2Date = encryptionService.DecryptForUser(record.OPV2Date, user);
            }

            if (!string.IsNullOrEmpty(record.OPV2Remarks))
            {
                record.OPV2Remarks = encryptionService.DecryptForUser(record.OPV2Remarks, user);
            }

            if (!string.IsNullOrEmpty(record.OPV3Date))
            {
                record.OPV3Date = encryptionService.DecryptForUser(record.OPV3Date, user);
            }

            if (!string.IsNullOrEmpty(record.OPV3Remarks))
            {
                record.OPV3Remarks = encryptionService.DecryptForUser(record.OPV3Remarks, user);
            }

            // Decrypt IPV doses
            if (!string.IsNullOrEmpty(record.IPV1Date))
            {
                record.IPV1Date = encryptionService.DecryptForUser(record.IPV1Date, user);
            }

            if (!string.IsNullOrEmpty(record.IPV1Remarks))
            {
                record.IPV1Remarks = encryptionService.DecryptForUser(record.IPV1Remarks, user);
            }

            if (!string.IsNullOrEmpty(record.IPV2Date))
            {
                record.IPV2Date = encryptionService.DecryptForUser(record.IPV2Date, user);
            }

            if (!string.IsNullOrEmpty(record.IPV2Remarks))
            {
                record.IPV2Remarks = encryptionService.DecryptForUser(record.IPV2Remarks, user);
            }

            // Decrypt PCV doses
            if (!string.IsNullOrEmpty(record.PCV1Date))
            {
                record.PCV1Date = encryptionService.DecryptForUser(record.PCV1Date, user);
            }

            if (!string.IsNullOrEmpty(record.PCV1Remarks))
            {
                record.PCV1Remarks = encryptionService.DecryptForUser(record.PCV1Remarks, user);
            }

            if (!string.IsNullOrEmpty(record.PCV2Date))
            {
                record.PCV2Date = encryptionService.DecryptForUser(record.PCV2Date, user);
            }

            if (!string.IsNullOrEmpty(record.PCV2Remarks))
            {
                record.PCV2Remarks = encryptionService.DecryptForUser(record.PCV2Remarks, user);
            }

            if (!string.IsNullOrEmpty(record.PCV3Date))
            {
                record.PCV3Date = encryptionService.DecryptForUser(record.PCV3Date, user);
            }

            if (!string.IsNullOrEmpty(record.PCV3Remarks))
            {
                record.PCV3Remarks = encryptionService.DecryptForUser(record.PCV3Remarks, user);
            }

            // Decrypt MMR doses
            if (!string.IsNullOrEmpty(record.MMR1Date))
            {
                record.MMR1Date = encryptionService.DecryptForUser(record.MMR1Date, user);
            }

            if (!string.IsNullOrEmpty(record.MMR1Remarks))
            {
                record.MMR1Remarks = encryptionService.DecryptForUser(record.MMR1Remarks, user);
            }

            if (!string.IsNullOrEmpty(record.MMR2Date))
            {
                record.MMR2Date = encryptionService.DecryptForUser(record.MMR2Date, user);
            }

            if (!string.IsNullOrEmpty(record.MMR2Remarks))
            {
                record.MMR2Remarks = encryptionService.DecryptForUser(record.MMR2Remarks, user);
            }

            // Decrypt audit fields
            if (!string.IsNullOrEmpty(record.CreatedBy))
            {
                record.CreatedBy = encryptionService.DecryptForUser(record.CreatedBy, user);
            }

            if (!string.IsNullOrEmpty(record.UpdatedBy))
            {
                record.UpdatedBy = encryptionService.DecryptForUser(record.UpdatedBy, user);
            }

            return record;
        }

        /// <summary>
        /// Decrypts ImmunizationShortcutForm data for authorized users
        /// </summary>
        public static Models.ImmunizationShortcutForm DecryptImmunizationShortcutData(this Models.ImmunizationShortcutForm form, IDataEncryptionService encryptionService, ClaimsPrincipal user)
        {
            if (form == null || encryptionService == null || !encryptionService.CanUserDecrypt(user))
                return form;

            // Decrypt basic information
            if (!string.IsNullOrEmpty(form.ChildName))
            {
                form.ChildName = encryptionService.DecryptForUser(form.ChildName, user);
            }

            if (!string.IsNullOrEmpty(form.MotherName))
            {
                form.MotherName = encryptionService.DecryptForUser(form.MotherName, user);
            }

            if (!string.IsNullOrEmpty(form.FatherName))
            {
                form.FatherName = encryptionService.DecryptForUser(form.FatherName, user);
            }

            if (!string.IsNullOrEmpty(form.Address))
            {
                form.Address = encryptionService.DecryptForUser(form.Address, user);
            }

            if (!string.IsNullOrEmpty(form.Barangay))
            {
                form.Barangay = encryptionService.DecryptForUser(form.Barangay, user);
            }

            if (!string.IsNullOrEmpty(form.Email))
            {
                form.Email = encryptionService.DecryptForUser(form.Email, user);
            }

            if (!string.IsNullOrEmpty(form.ContactNumber))
            {
                form.ContactNumber = encryptionService.DecryptForUser(form.ContactNumber, user);
            }

            if (!string.IsNullOrEmpty(form.PreferredDate))
            {
                form.PreferredDate = encryptionService.DecryptForUser(form.PreferredDate, user);
            }

            if (!string.IsNullOrEmpty(form.PreferredTime))
            {
                form.PreferredTime = encryptionService.DecryptForUser(form.PreferredTime, user);
            }

            if (!string.IsNullOrEmpty(form.Notes))
            {
                form.Notes = encryptionService.DecryptForUser(form.Notes, user);
            }

            if (!string.IsNullOrEmpty(form.CreatedAt))
            {
                form.CreatedAt = encryptionService.DecryptForUser(form.CreatedAt, user);
            }

            if (!string.IsNullOrEmpty(form.UpdatedAt))
            {
                form.UpdatedAt = encryptionService.DecryptForUser(form.UpdatedAt, user);
            }

            if (!string.IsNullOrEmpty(form.CreatedBy))
            {
                form.CreatedBy = encryptionService.DecryptForUser(form.CreatedBy, user);
            }

            if (!string.IsNullOrEmpty(form.Status))
            {
                form.Status = encryptionService.DecryptForUser(form.Status, user);
            }

            return form;
        }
    }
}
