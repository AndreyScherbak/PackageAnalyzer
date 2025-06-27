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
    /// The Sitecore topology Checker
    /// </summary>
    public class TopologyChecker
    {
        public string topology;
        /// <summary>
        /// Initializes a new instance of the TopologyChecker
        /// </summary>
        public TopologyChecker()
        {
                 
        }
        /// <summary>
        /// Checks Sitecore topology
        /// </summary>
        /// <param name="filepath">Path to package directory</param>
        /// <returns></returns>
        public string CheckTopology(string filepath)
        {
            var manager = new FileManager(filepath);
            var anyConfigFile = manager.FindFiles("*.config", true);
           
            var files = manager.FindFiles(Constants.xdbConfig,false);
        
            if (anyConfigFile == null)
            {
                return "role not found";
            }
            if (files == null)
            {
                return "XM";
            }
            else
            {
                if (files.Count > 0)
                {
                    var xdbEnabled = ReadSetting(files.FirstOrDefault().DirectoryName + @"\\" + files.FirstOrDefault().Name, "Xdb.Enabled");
                    var xdbTrackingEnabled = ReadSetting(files.FirstOrDefault().DirectoryName + @"\\" + files.FirstOrDefault().Name, "Xdb.Tracking.Enabled");
                    if (xdbEnabled == "false" & xdbTrackingEnabled == "false")
                    {
                        topology = "XP in CMS-ONLY mode";

                    }
                    topology = "XP";
                }
                return topology;
            }
        }
        /// <summary>
        /// Read setting from config
        /// </summary>
        /// <param name="webConfigPath">Path to config file</param>
        /// <param name="key">Name of the setting</param>
        /// <returns></returns>
        public static string ReadSetting(string webConfigPath, string key)
        {
            try
            {
                // Load the downloaded web.config file into an XmlDocument
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(webConfigPath);

                // Access the appSettings section
                XmlNode appSettingsNode = xmlDoc.SelectSingleNode("/configuration/sitecore/settings");

                if (appSettingsNode != null)
                {
                    // Find the key in appSettings
                    XmlNode keyNode = appSettingsNode.SelectSingleNode($"setting[@name='{key}']");

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
