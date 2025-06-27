using PackageAnalyzer.Core.FileSystem;

namespace PackageAnalyzer.Core.Readers
{
    public class ModulesReader
    {
        private Dictionary<string, string> SXAVersions = new Dictionary<string, string>
        {
            { "3.8.0", "SXA 1.8.0 (Sitecore XP 9.0.2)" },
            { "3.8.1", "SXA 1.8.1 (Sitecore XP 9.0.2)" },
            { "4.8.0", "SXA 1.8.0 (Sitecore XP 9.1.0)" },
            { "4.8.1", "SXA 1.8.1 (Sitecore XP 9.1.1)" },
            { "4.9.0", "SXA 1.9.0 (Sitecore XP 9.1.1)" },
            { "5.9.0", "SXA 1.9.0 (Sitecore XP 9.2.0)" },
            { "6.10.0", "SXA 9.3.0 (Sitecore XP 9.3.0)" },
            { "7.0.0", "SXA 10.0.0 (Sitecore XP 10.0.0)" },
            { "8.0.0", "SXA 10.1.0 (Sitecore XP 10.1.X)" },
            { "9.0.0", "SXA 10.2.0 (Sitecore XP 10.2.X)" },
            { "11.1", "SXA 10.3.0 (Sitecore XP 10.3.X)" },
            { "12.0", "SXA 10.4.0 (Sitecore XP 10.4.X)" },
        };

        private Dictionary<string, string> SPSVersions = new Dictionary<string, string>
        {
            {"4.1", "Publishing Service module 4.1.0.0 (Sitecore 9.1.0)"},
            {"4.2", "Publishing Service module 4.2.0.0 (Sitecore 9.1.1)"},
            {"5.0", "Publishing Service module 5.0.0.0 (Sitecore 9.2.0)"},
            {"6.0", "Publishing Service module 6.0.0.0 (Sitecore 9.3.0)"},
            {"7.0", "Publishing Service module 7.0.0.0 (Sitecore 10.0.X)"},
            {"8.0", "Publishing Service module 8.0.0.0 (Sitecore 10.1.X)"},
            {"9.0", "Publishing Service module 9.0.0.0 (Sitecore 10.2.X)"},
            {"11.0", "Publishing Service module 11.0.11.0 (Sitecore 10.3.X)"},
            {"12.0", "Publishing Service module 12.0.5.0 (Sitecore 10.4.X)"},
        };

        private Dictionary<string, string> HeadlessVersions = new Dictionary<string, string>
        {
            {"15.0.0", "Sitecore Headless Rendering 14.0.0 (Sitecore 10.0.X)"},
            {"16.0.0", "Sitecore Headless Rendering 15.0.0 (Sitecore 10.0.X)"},
            {"16.0.1", "Sitecore Headless Rendering 15.0.1 (Sitecore 10.0.X)"},
            {"17.0.0", "Sitecore Headless Rendering 16.0.0 (Sitecore 10.1.X)"},
            {"18.0.0", "Sitecore Headless Rendering 18.0.0 (Sitecore 10.1.X)"},
            {"19.0.0", "Sitecore Headless Rendering 19.0.X (Sitecore 10.2.X)"},
            {"20.0.0", "Sitecore Headless Rendering 20.0.0 (Sitecore 10.2.X)"},
            {"20.0.2", "Sitecore Headless Rendering 20.0.2 (Sitecore 10.2.X)"},
            {"21.0", "Sitecore Headless Rendering 21.0.X (Sitecore 10.3.X)"},
            {"22.0", "Sitecore Headless Rendering 22.0.X (Sitecore 10.4.X)"},
        };

        private Dictionary<string, string> EEConnectorVersions = new Dictionary<string, string>
        {
            {"18.0.0", "Experience Edge Connector 18.0.0 (Sitecore 10.1.X)"},
            {"19.0.0", "Experience Edge Connector 19.0.0 (Sitecore 10.2.X)"},
            {"19.0.1", "Experience Edge Connector 19.0.1 (Sitecore 10.2.X)"},
            {"20.0", "Experience Edge Connector 20.0.X (Sitecore 10.2.X)"},
            {"21.0", "Experience Edge Connector 21.0.X (Sitecore 10.3.X)"},
            {"22.0", "Experience Edge Connector 22.0.X (Sitecore 10.4.X)"},
        };

        private Dictionary<string, string> ConnectForCHVersions = new Dictionary<string, string>
        {
            {"3.1.0", "Sitecore Connect for Content Hub 3.1.0 (Sitecore 9.1.1)"},
            {"4.0.0", "Sitecore Connect for Content Hub 4.0.0 (Sitecore 9.3-10.1)"},
            {"5.0.0", "Sitecore Connect for Content Hub 5.0.0 (Sitecore 9.2-10.2)"},
            {"5.1", "Sitecore Connect for Content Hub 5.1.0 (Sitecore 10.0-10.3)"},
            {"5.2", "Sitecore Connect for Content Hub 5.2.0 (Sitecore 10.0-10.4)"}
        };

        private Dictionary<string, string> ModulesNames = new Dictionary<string, string>
        {
            {"Sitecore.XA.Foundation*", "SXA is installed but cannot resolve version"},
            {"Sitecore.Publishing.Service*", "Publishing Service module is installed but cannot resolve version"},
            {"Sitecore.JavaScriptServices*", "JSS is installed but cannot resolve version"},
            {"ExperienceEdgeConnector*", "Experience Edge Connector is installed but cannot resolve version"},
            {"Sitecore.Connector.ContentHub.DAM*", "Sitecore Connect For Content Hub is installed but cannot resolve version"}

        };
        /// <summary>
        /// Read information about installed Sitecore modules and returns dictionary with them
        /// </summary>
        /// <param name="filepath">Path to package directory</param>
        /// <returns>List of PackageAnalyzer.Core.Readers.Data.Domain</returns>
        public List<string> ReadModules(string filepath)
        {
            var list = new List<string>
            {
                { GenericModuleReader(filepath,"Sitecore.XA.Foundation*", "Sitecore.XA.Foundation.Common.dll", SXAVersions ) },
                { GenericModuleReader(filepath,"Sitecore.Publishing.Service*", "Sitecore.Publishing.Service.dll", SPSVersions ) },
                { GenericModuleReader(filepath,"Sitecore.JavaScriptServices*", "Sitecore.JavaScriptServices.Core.dll", HeadlessVersions ) },
                { GenericModuleReader(filepath,"ExperienceEdgeConnector*", "Sitecore.ExperienceEdge.Connector.Abstraction.dll", EEConnectorVersions ) },
                { GenericModuleReader(filepath,"Sitecore.Connector.ContentHub.DAM*", "Sitecore.Connector.ContentHub.DAM.dll", ConnectForCHVersions ) }
            };
            return list;
        }
        private string GenericModuleReader(string filepath, string keyNameSpace, string assemblyName, Dictionary<string, string> versionsDictionary)
        {
            var manager = new FileManager(filepath);
            var files = manager.FindFiles(keyNameSpace, true);
            if (files != null && files.Count > 0)
            {
                AssemblyInfoReader assemblyInfoReader = new AssemblyInfoReader();
                string assemblyVersion = assemblyInfoReader.GetAssemblyVersion(assemblyName, filepath);
                if (assemblyVersion != null)
                {
                    foreach (var version in versionsDictionary)
                    {
                        if (assemblyVersion.Contains(version.Key))
                        {
                            return version.Value;
                        }
                    }
                }
                else
                {
                    return ModulesNames[keyNameSpace];
                }
            }
            return string.Empty;
        }
    }
}
