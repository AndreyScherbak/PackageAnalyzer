using PackageAnalyzer.Core.FileSystem;
using System.Reflection;
using Serilog;

namespace PackageAnalyzer.Core.Readers
{
    public class AssemblyInfoReader
    {
        public Dictionary<string, string> assemblyVersions = new Dictionary<string, string>();

        public Dictionary<string, string> ReadAssemblyVersionsFromFile(string filepath)
        {
            var manager = new FileManager(filepath);
            var files = manager.FindFiles(Constants.assemblyInfoFile);
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    var assemblyVersion = FileManager.ReadXmlPairValues(file.DirectoryName + @"\\" + file.Name, "assembly", "productVersion");
                    foreach (var version in assemblyVersion)
                    {
                        // Check if the key already exists before adding it
                        if (!assemblyVersions.ContainsKey(version.Key))
                        {
                            assemblyVersions.Add(version.Key, version.Value);
                        }
                    }
                }
            }
            return assemblyVersions;
        }
        public Dictionary<string, string> ReadAssemblyVersionsFromBinFolder(string filepath, string assemblyName = "")
        {
            var manager = new FileManager(filepath);
            var files = string.IsNullOrWhiteSpace(assemblyName)
                ? manager.FindFiles("*.dll")
                : manager.FindFiles(assemblyName);

            var assemblyVersions = new Dictionary<string, string>();

            foreach (var file in files)
            {
                try
                {
                    var assembly = Assembly.LoadFile(file.FullName);
                    var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
                    var productVersion = fileVersionAttribute?.Version ?? string.Empty;

                    assemblyVersions.TryAdd(file.Name, productVersion);
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to get \"Product version\" attribute for {file.Name}: {ex.Message}");
                }
            }

            return assemblyVersions;
        }

        public bool IsAssemblyInfoFileExists(string filePath)
        {
            var manager = new FileManager(filePath);
            var files = manager.FindFiles(Constants.assemblyInfoFile);
            
            if (files != null) { 
                return true;
            }
            return false;
        }

        public string GetAssemblyVersion(string assemblyName, string filepath)
        {
            // Check if the assembly version is already in the dictionary
            if (assemblyVersions.TryGetValue(assemblyName, out string version))
            {
                return version;
            }

            // Try to read from AssemblyInfo.xml
            var assemblyVersionsFromFile = ReadAssemblyVersionsFromFile(filepath);
            if (assemblyVersionsFromFile.TryGetValue(assemblyName, out version))
            {
                return version;
            }

            // Try to read from the bin folder
            var assemblyVersionsFromBin = ReadAssemblyVersionsFromBinFolder(filepath, assemblyName);
            if (assemblyVersionsFromBin.TryGetValue(assemblyName, out version))
            {
                return version;
            }

            // If not found, return null or an appropriate message
            return null;
        }
    }
}
