using PackageAnalyzer.Core.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PackageAnalyzer.Core.Readers
{
    /// <summary>
    /// Web.config reader
    /// </summary>
    public class WebConfigInfoReader
    {
        public Dictionary<string, string> roles = new Dictionary<string, string>();
        /// <summary>
        /// Read Sitecore roles from web.config
        /// </summary>
        /// <param name="filepath">Path to package directory</param>
        /// <returns>Dictionary of roles found in all web.config files of the package</returns>
        public Dictionary<string,string> ReadRole (string filepath)
        {
            string key = "role:define";
            var manager = new FileManager(filepath);
            var files = manager.FindFiles(Constants.webConfig);
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    // If the full path doesn't start with the root path, return the full path as is
                    roles.Add(file.DirectoryName + @"\\" + file.Name, ReadAppSetting(file.DirectoryName + @"\\" + file.Name, key));
                }
            }
            return roles;
        }
        /// <summary>
        /// Read appsetting from web.config
        /// </summary>
        /// <param name="webConfigPath">Path to web.config</param>
        /// <param name="key">appsettings key</param>
        /// <returns>Value of app setting or null if it is not found</returns>
        public static string ReadAppSetting(string webConfigPath, string key)
        {
            try
            {
                // Load the downloaded web.config file into an XmlDocument
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(webConfigPath);
              
                // Access the appSettings section
                XmlNode appSettingsNode = xmlDoc.SelectSingleNode("/configuration/appSettings");

                if (appSettingsNode != null)
                {
                    // Find the key in appSettings
                    XmlNode keyNode = appSettingsNode.SelectSingleNode($"add[@key='{key}']");

                    if (keyNode != null && keyNode.Attributes != null)
                    {
                        // Get the value associated with the key
                        XmlAttribute valueAttribute = keyNode.Attributes["value"];

                        if (valueAttribute != null)
                        {
                            return valueAttribute.Value;
                        }
                    }
                }

                return null; // Key not found
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
