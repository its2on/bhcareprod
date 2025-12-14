using System;
using System.Collections.Generic;

namespace Barangay.Extensions
{
    public static class StringExtensions
    {
        public static bool HasValue(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        public static string Value(this string str)
        {
            return str ?? string.Empty;
        }
        

        // Ensure method is invoked
        public static bool HasValue(this object obj, string methodName)
        {
            if (obj == null) return false;
            var method = obj.GetType().GetMethod(methodName);
            if (method == null) return false;
            
            var result = method.Invoke(obj, null);
            if (result is string strResult)
                return !string.IsNullOrEmpty(strResult);
            
            return result != null;
        }
        

        public static T Value<T>(this object? obj, string methodName, T defaultValue = default!) where T : class
        {
            if (obj == null) return defaultValue;
            var method = obj.GetType().GetMethod(methodName);
            if (method == null) return defaultValue;
            
            var result = method.Invoke(obj, null);
            return result as T ?? defaultValue;
        }
        
        
        public static List<string> ToStringList(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return new List<string>();
                
            return new List<string> { str };
        }
        
        
        public static string ToSingleString(this List<string> list, string defaultValue = "")
        {
            if (list == null || list.Count == 0)
                return defaultValue;
                
            return list[0];
        }
        

        public static List<string> CoalesceWithList(this List<string> list, string defaultValue)
        {
            if (list == null || list.Count == 0)
                return new List<string> { defaultValue };
                
            return list;
        }
        
        // TimeSpan conversion methods
        
        // Convert string to TimeSpan
        public static TimeSpan ToTimeSpan(this string timeString, TimeSpan defaultValue = default)
        {
            if (string.IsNullOrEmpty(timeString))
                return defaultValue;
                
            if (TimeSpan.TryParse(timeString, out TimeSpan result))
                return result;
                
            return defaultValue;
        }
        
        // Convert TimeSpan to string
        public static string ToString(this TimeSpan timeSpan, string format = "hh\\:mm")
        {
            return timeSpan.ToString(format);
        }
        
        // Null coalescing for TimeSpan and string
        public static TimeSpan CoalesceTimeSpan(this string timeString, TimeSpan defaultValue)
        {
            return timeString.HasValue() ? timeString.ToTimeSpan() : defaultValue;
        }
        
        public static string CoalesceString(this TimeSpan timeSpan, string defaultValue)
        {
            return timeSpan != default ? timeSpan.ToString() : defaultValue;
        }
        
        // Extension method to safely invoke HasValue method
        public static bool HasValueSafe(this object obj, string methodName)
        {
            if (obj == null) return false;
            
            try
            {
                var method = obj.GetType().GetMethod(methodName);
                if (method == null) return false;
                
                var result = method.Invoke(obj, null);
                if (result is string strResult)
                    return !string.IsNullOrEmpty(strResult);
                
                return result != null;
            }
            catch
            {
                return false;
            }
        }
        
        // Additional helper methods for common error patterns
        
        // Helper for TimeSpan property HasValue
        public static bool HasValue(this TimeSpan? timeSpan)
        {
            return timeSpan.HasValue && timeSpan.Value != default;
        }
        
        // Helper for int coalescing
        public static int CoalesceInt(this int? value, int defaultValue)
        {
            return value ?? defaultValue;
        }
        
        // Helper for converting TimeSpan to string with null check
        public static string ToTimeString(this TimeSpan? timeSpan, string defaultValue = "")
        {
            if (!timeSpan.HasValue)
                return defaultValue;
                
            return timeSpan.Value.ToString("hh\\:mm");
        }
        
        // Helper for converting string to TimeSpan with null check
        public static TimeSpan? ToNullableTimeSpan(this string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return null;
                
            if (TimeSpan.TryParse(timeString, out TimeSpan result))
                return result;
                
            return null;
        }
        
        // Helper for ApplicationUser to Doctor conversion
        public static T? ConvertTo<T>(this object? source) where T : class, new()
        {
            if (source == null)
                return null;
                
            var result = new T();
            var sourceProperties = source.GetType().GetProperties();
            var targetProperties = typeof(T).GetProperties();
            
            foreach (var targetProp in targetProperties)
            {
                var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == targetProp.Name);
                if (sourceProp != null && sourceProp.CanRead && targetProp.CanWrite)
                {
                    var value = sourceProp.GetValue(source);
                    if (value != null && targetProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                    {
                        targetProp.SetValue(result, value);
                    }
                }
            }
            
            return result;
        }
    }
}