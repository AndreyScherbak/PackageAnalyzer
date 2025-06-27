using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using PackageAnalyzer.Core.FileSystem;

namespace PackageAnalyzer.Core.Readers
{
    /// <summary>
    /// The Sitecore Version Reader
    /// </summary>
    public class SitecoreVersionReader
    {
        public  Dictionary<string, string> versions = new Dictionary<string, string>();

        private Dictionary<string, string> kernelAndSitecoreVersions = new Dictionary<string, string>
        {
            { "10.4.X", "19" },
            { "10.3.X", "18.0" },
            { "10.2.X", "17.0" },
            { "10.1.X", "16.0" },
            { "10.0.X", "15.0" },
            { "8.1.0", "8.1.0" },
            { "8.1.1", "8.1.1" },
            { "8.1.2", "8.1.2" },
            { "8.1.3", "8.1.3" },
            { "8.2.0", "10.0.0" },
            { "8.2.1", "10.0.3" },
            { "8.2.2", "10.0.4" },
            { "8.2.3", "10.0.5" },
            { "8.2.4", "10.0.6" },
            { "8.2.5", "10.0.7" },
            { "8.2.6", "10.0.8" },
            { "8.2.7", "10.0.9" },
            { "9.0.0", "11.1.0" },
            { "9.0.1", "11.1.1" },
            { "9.0.2", "11.1.2" },
            { "9.1.0", "12.0.0" },
            { "9.1.1", "12.0.1" },
            { "9.2.0", "13.0.0" },
            { "9.3.0", "14.0.0" }
        };

    /// <summary>
    /// Read versions from the package
    /// </summary>
    /// <param name="filepath">Path to package directory</param>
    /// <returns>Dictionary of versions found in all sitecore.version.xml files</returns>
    public Dictionary<string, string> Read (string filepath)
        {
            var manager = new FileManager(filepath);
            var files = manager.FindFiles(Constants.sitecoreVersion);
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {

                    var version = ReadVersion(file);
                    // If the full path doesn't start with the root path, return the full path as is
                    versions.Add(file.DirectoryName + @"\\" + file.Name, version);

                }
            }
            if (versions.Count == 0)
            {
                versions.Add(string.Empty, ReadVersionFromAssemblies(filepath));
            }
            return versions;
        }
        /// <summary>
        /// Read Sitecore version from sitecore.version.xml file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private string ReadVersion(FileInfo file)
        {
            var versionMajor = FileManager.ReadXmlValue(file.DirectoryName + @"\\" + file.Name, "major");
            var versionMinor = FileManager.ReadXmlValue(file.DirectoryName + @"\\" + file.Name, "minor");
            var versionBuild = FileManager.ReadXmlValue(file.DirectoryName + @"\\" + file.Name, "build");
            var versionRevision = FileManager.ReadXmlValue(file.DirectoryName + @"\\" + file.Name, "revision");

            return "Sitecore " + versionMajor + "." + versionMinor + "." + versionBuild + " rev.:" + versionRevision;
        }

        private string ReadVersionFromAssemblies(string filepath)
        {
            var assemblyInfoReader = new AssemblyInfoReader();
            string assemblyVersion = assemblyInfoReader.GetAssemblyVersion("Sitecore.Kernel.dll", filepath);

            if (assemblyVersion == null)
            {
                return null;
            }

            foreach (var item in kernelAndSitecoreVersions)
            {
                if (assemblyVersion.StartsWith(item.Value))
                {
                    return $"Sitecore {item.Key} (Resolved from assemblies. Please check for a pre-release manually)";
                }
            }

            return null;
        }


    }
}
