using System;
using System.Text.RegularExpressions;

namespace Barangay.Services
{
    public interface IDeviceInfoParser
    {
        DeviceInfo Parse(string userAgent);
    }

    public class DeviceInfo
    {
        public string Browser { get; set; } = "Unknown";
        public string BrowserVersion { get; set; } = "";
        public string OS { get; set; } = "Unknown";
        public string OSVersion { get; set; } = "";
        public string Device { get; set; } = "Desktop";
        public string Platform { get; set; } = "Unknown";
        public string FullInfo { get; set; } = "";
    }

    public class DeviceInfoParser : IDeviceInfoParser
    {
        public DeviceInfo Parse(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                return new DeviceInfo { FullInfo = "Unknown" };
            }

            var deviceInfo = new DeviceInfo { FullInfo = userAgent };

            // Parse Browser
            ParseBrowser(userAgent, deviceInfo);

            // Parse OS
            ParseOS(userAgent, deviceInfo);

            // Parse Device Type
            ParseDevice(userAgent, deviceInfo);

            return deviceInfo;
        }

        private void ParseBrowser(string userAgent, DeviceInfo deviceInfo)
        {
            // Chrome (must check before Safari since Chrome includes Safari in UA)
            var chromeMatch = Regex.Match(userAgent, @"Chrome[\/\s](\d+[\.\d]*)");
            if (chromeMatch.Success && !userAgent.Contains("Edg"))
            {
                deviceInfo.Browser = "Chrome";
                deviceInfo.BrowserVersion = chromeMatch.Groups[1].Value;
                return;
            }

            // Edge (Chromium-based)
            var edgeMatch = Regex.Match(userAgent, @"Edg[\/\s](\d+[\.\d]*)");
            if (edgeMatch.Success)
            {
                deviceInfo.Browser = "Edge";
                deviceInfo.BrowserVersion = edgeMatch.Groups[1].Value;
                return;
            }

            // Firefox
            var firefoxMatch = Regex.Match(userAgent, @"Firefox[\/\s](\d+[\.\d]*)");
            if (firefoxMatch.Success)
            {
                deviceInfo.Browser = "Firefox";
                deviceInfo.BrowserVersion = firefoxMatch.Groups[1].Value;
                return;
            }

            // Safari (check after Chrome)
            var safariMatch = Regex.Match(userAgent, @"Version[\/\s](\d+[\.\d]*).+Safari");
            if (safariMatch.Success)
            {
                deviceInfo.Browser = "Safari";
                deviceInfo.BrowserVersion = safariMatch.Groups[1].Value;
                return;
            }

            // Opera
            var operaMatch = Regex.Match(userAgent, @"(?:Opera|OPR)[\/\s](\d+[\.\d]*)");
            if (operaMatch.Success)
            {
                deviceInfo.Browser = "Opera";
                deviceInfo.BrowserVersion = operaMatch.Groups[1].Value;
                return;
            }

            deviceInfo.Browser = "Unknown";
        }

        private void ParseOS(string userAgent, DeviceInfo deviceInfo)
        {
            // Windows
            if (userAgent.Contains("Windows NT"))
            {
                deviceInfo.OS = "Windows";
                var windowsMatch = Regex.Match(userAgent, @"Windows NT (\d+\.\d+)");
                if (windowsMatch.Success)
                {
                    var version = windowsMatch.Groups[1].Value;
                    deviceInfo.OSVersion = version switch
                    {
                        "10.0" => "10/11",
                        "6.3" => "8.1",
                        "6.2" => "8",
                        "6.1" => "7",
                        "6.0" => "Vista",
                        "5.1" => "XP",
                        _ => version
                    };
                }
                deviceInfo.Platform = "Windows";
                return;
            }

            // macOS
            if (userAgent.Contains("Mac OS X"))
            {
                deviceInfo.OS = "macOS";
                var macMatch = Regex.Match(userAgent, @"Mac OS X (\d+[_\.\d]*)");
                if (macMatch.Success)
                {
                    deviceInfo.OSVersion = macMatch.Groups[1].Value.Replace("_", ".");
                }
                deviceInfo.Platform = "Mac";
                return;
            }

            // Linux
            if (userAgent.Contains("Linux"))
            {
                deviceInfo.OS = "Linux";
                deviceInfo.Platform = "Linux";
                
                // Check for specific distributions
                if (userAgent.Contains("Ubuntu"))
                {
                    deviceInfo.OS = "Ubuntu";
                }
                else if (userAgent.Contains("Fedora"))
                {
                    deviceInfo.OS = "Fedora";
                }
                return;
            }

            // Android
            if (userAgent.Contains("Android"))
            {
                deviceInfo.OS = "Android";
                var androidMatch = Regex.Match(userAgent, @"Android (\d+[\.\d]*)");
                if (androidMatch.Success)
                {
                    deviceInfo.OSVersion = androidMatch.Groups[1].Value;
                }
                deviceInfo.Platform = "Mobile";
                return;
            }

            // iOS
            if (userAgent.Contains("iPhone") || userAgent.Contains("iPad") || userAgent.Contains("iPod"))
            {
                deviceInfo.OS = userAgent.Contains("iPad") ? "iPadOS" : "iOS";
                var iosMatch = Regex.Match(userAgent, @"OS (\d+[_\d]*)");
                if (iosMatch.Success)
                {
                    deviceInfo.OSVersion = iosMatch.Groups[1].Value.Replace("_", ".");
                }
                deviceInfo.Platform = "Mobile";
                return;
            }

            deviceInfo.OS = "Unknown";
        }

        private void ParseDevice(string userAgent, DeviceInfo deviceInfo)
        {
            if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || 
                userAgent.Contains("iPhone") || userAgent.Contains("iPod"))
            {
                deviceInfo.Device = "Mobile";
            }
            else if (userAgent.Contains("iPad") || userAgent.Contains("Tablet"))
            {
                deviceInfo.Device = "Tablet";
            }
            else
            {
                deviceInfo.Device = "Desktop";
            }
        }
    }
}
