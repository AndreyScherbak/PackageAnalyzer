using PackageAnalyzer.Core;
using PackageAnalyzer.Configuration;
using PSS.Telemetry;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace PackageAnalyzer.Telemetry
{
    internal class TelemetryMetadataProvider : IMetadataProvider
    {
        private bool CheckIsTrackingEnabled()
        {
            PackageAnalyzerConfiguration packageAnalyzerConfiguration = new PackageAnalyzerConfiguration();
            bool result;
            if (bool.TryParse((packageAnalyzerConfiguration.GetSetting("TrackingEnabled")), out result))
            {
                return result;
            }
            return result;
        }
        public bool IsTrackingEnabled => CheckIsTrackingEnabled();

        public Guid GetApplicationId()
        {
            return PSS.Telemetry.Constants.PackageAnalyzerAppId;
        }

        public string GetAppVersion()
        {
            return ApplicationManager.GetVersion();
        }

        public Guid GetDeviceId()
        {
            // Get the primary MAC address
            string macAddress = GetPrimaryMacAddress();

            // Get the OS version
            string osVersion = Environment.OSVersion.VersionString;

            // Combine them into a unique identifier
            string identifier = $"{macAddress}-{osVersion}";

            // Generate a GUID from the identifier
            using (var md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(identifier));
                return new Guid(hashBytes);
            }
        }

        public string GetLanguage()
        {
            return default;
        }

        public string GetOperatingSystem()
        {
            return default;
        }

        public int GetScreenHeight()
        {
            return default;
        }

        public int GetScreenWidth()
        {
            return default;
        }

        private string GetPrimaryMacAddress()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var primaryInterface = networkInterfaces.FirstOrDefault(nic =>
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                nic.OperationalStatus == OperationalStatus.Up);

            if (primaryInterface != null)
            {
                return primaryInterface.GetPhysicalAddress().ToString();
            }

            return "00:00:00:00:00:00"; // Fallback MAC address if none is found
        }
    }
}
