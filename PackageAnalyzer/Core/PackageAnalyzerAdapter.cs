using PackageAnalyzer.Core.FileSystem;
using PackageAnalyzer.Core.Readers;
using PackageAnalyzer.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace PackageAnalyzer.Core
{
    internal static class PackageAnalyzerAdapter
    {
        public static Dictionary<string, string> SitecoreRoles = new Dictionary<string, string>();
        public static string GetSitecoreRoles(string filePath)
        {
            string result = string.Empty;
            try
            {
                WebConfigInfoReader webConfigInfoReader = new WebConfigInfoReader();
                foreach (var item in webConfigInfoReader.ReadRole(filePath))
                {
                    if (item.Value != null && !string.IsNullOrWhiteSpace(item.Value.ToString()))
                    {
                        result += item.Value + Environment.NewLine;
                    }
                }
            }
            catch (Exception)
            {
                // Write the error to logs
                result = "No roles found";
            }
            return result;
        }
        public static string GetSitecoreVersions(string filePath)
        {
            string result = string.Empty;
            try
            {
                SitecoreVersionReader sitecoreVersionReader = new SitecoreVersionReader();

                var versions = sitecoreVersionReader.Read(filePath);

                if (versions == null || versions.Count() == 0)
                {
                    result = "No version information found";
                    return result;
                }

                if (versions.Values.Distinct().Count() == 1)
                {
                    return versions.Values.FirstOrDefault();
                }
                else
                {
                    foreach (var item in versions)
                    {
                        result += item.Value + Environment.NewLine;
                    }
                }
            }
            catch (Exception ex)
            {
                // Write the error to logs
                Serilog.Log.Error(ex.ToString());
                result = "No version information found";
            }

            return result;
        }

        public static List<AssemblyData> GetSitecoreAssemblyVersions(string filePath)
        {
            List<AssemblyData> result = new List<AssemblyData>();
            try
            {
                AssemblyInfoReader assemblyInfoReader = new AssemblyInfoReader();
                if (assemblyInfoReader.IsAssemblyInfoFileExists(filePath))
                {
                    foreach (var item in assemblyInfoReader.ReadAssemblyVersionsFromFile(filePath))
                    {
                        result.Add(new AssemblyData() { AssemblyName = item.Key, AssemblyVersion = item.Value });
                    }
                }
                else
                {
                    foreach (var item in assemblyInfoReader.ReadAssemblyVersionsFromBinFolder(filePath))
                    {
                        result.Add(new AssemblyData() { AssemblyName = item.Key, AssemblyVersion = item.Value });
                    }
                }
            }
            catch (Exception)
            {
                // Write the error to logs
                result.Add(new AssemblyData() { AssemblyName = "No assemblies were found", AssemblyVersion = "" });
            }
            return result;
        }

        public static string GetSitecoreModules(string filePath)
        {
            string result = string.Empty;
            ModulesReader modulesReader = new ModulesReader();
            foreach (var item in modulesReader.ReadModules(filePath))
            {
                if (item != string.Empty)
                {
                    result += item + Environment.NewLine;
                }
            }
            if (result == string.Empty)
            {
                return "-";
            }
            return result;

        }

        private static XmlDocument GetShowConfig(string filePath)
        {
            string consoleAppPath = Environment.ProcessPath.Replace("PackageAnalyzer.exe", string.Empty) + "ConfigBuilderConsole.exe"; // add it to your debug or release folder where PackageAnalyzer.exe is present.
            string assemblyPath = Environment.ProcessPath.Replace("PackageAnalyzer.exe", string.Empty) + "Sitecore.Diagnostics.ConfigBuilder.dll";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = consoleAppPath,
                Arguments = $"\"{filePath}\" \"{assemblyPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"Error running ConfigBuilderConsole: {error}");
                }

                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.LoadXml(output);
                }
                catch (XmlException ex)
                {
                    throw new InvalidOperationException("Error loading XML output from ConfigBuilderConsole.", ex);
                }
                return xmlDoc;
            }
        }

        public static string GetSitecoreSettings(string filePath)
        {
            string result = string.Empty;

            //string SettingsFolderPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "Settings");
            string SettingsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");

            SettingsReader settingsReader = new SettingsReader();

            if (!Directory.Exists(SettingsFolderPath))
            {
                Serilog.Log.Information($"Not found directory or its subdirectories for '{SettingsFolderPath}'");
                return result;
            }

            Dictionary<string, string> settingsList = new Dictionary<string, string>();

            settingsList = settingsReader.ReadAllXmlFiles(SettingsFolderPath);

            XmlDocument showConfig = new XmlDocument();
            var manager = new FileManager(filePath);
            var showConfigFile = manager.FindFiles("ShowConfig.xml");
            if (showConfigFile != null)
            {
                showConfig.Load(showConfigFile.FirstOrDefault().FullName);
            }
            else
            {
                var files = manager.FindFiles(Constants.webConfig);
                if (files != null && files.Count > 0)
                {
                    string webConfigPath = files.FirstOrDefault().FullName;
                    if (manager.IsArchive(Path.GetExtension(filePath)))
                    {
                        manager.FindFolder("App_Config");
                    }
                    showConfig = GetShowConfig(webConfigPath);
                    // Save the XML document to the new file path
                    var newFilePath = Path.Combine(filePath, "ShowConfig.xml");
                    showConfig.Save(newFilePath);
                }  
            }
            

            Dictionary<string, string> resultSettingsList = settingsReader.MatchDictionaryValuesWithXml(settingsList, showConfig);

            foreach (var item in resultSettingsList)
            {
                result += item.Key + "\r\n" + item.Value + "\r\n";
            }

            return result;
        }

        public static Dictionary<string, string> GetSitecoreRolesForComboBox(string filePath)
        {
            var sitecoreRoles = new Dictionary<string, string>();
            WebConfigInfoReader webConfigInfoReader = new WebConfigInfoReader();
            var roles = webConfigInfoReader.ReadRole(filePath);

            foreach (var role in roles)
            {
                if (role.Value == null) continue;

                ReadOnlySpan<char> keySpan = role.Key.AsSpan();
                string roleKey = null;

                if (keySpan.EndsWith(@"\Configs\Web.config", StringComparison.OrdinalIgnoreCase))
                {
                    roleKey = keySpan.Slice(0, keySpan.Length - @"\Configs\Web.config".Length).ToString();
                }
                if (keySpan.EndsWith(@"\Configs\\Web.config", StringComparison.OrdinalIgnoreCase))
                {
                    roleKey = keySpan.Slice(0, keySpan.Length - @"\Configs\\Web.config".Length).ToString();
                }
                else if (keySpan.EndsWith(@"\Web.config", StringComparison.OrdinalIgnoreCase))
                {
                    roleKey = keySpan.Slice(0, keySpan.Length - @"\Web.config".Length).ToString();
                }

                if (roleKey != null)
                {
                    sitecoreRoles[roleKey] = role.Value;
                }
            }

            return sitecoreRoles;
        }


        public static string InitialUnzipping(string filePath)
        {
            FileManager fileManager = new FileManager(filePath);
            if (fileManager.IsArchive(Path.GetExtension(filePath)))
            {
                string[] foldersToExclude = new string[]
                {
                    "Logs/",
                    "Assemblies/",
                    "Diagnostics/",
                    "Databases/",
                    "Framework Files/",
                    "UpgradeHistory/",
                    "App_Data/",
                    "sitecore/",
                    "Views/"
                };

                return ArchiveProvider.UnarchiveToTempFolder(filePath, foldersToExclude);
            }
            else
            {
                return filePath;
            }

        }

        public static string GetCustomTypes(string filePath)
        {
            XmlDocument showConfig = new XmlDocument();
            var manager = new FileManager(filePath);
            var showConfigFile = manager.FindFiles("ShowConfig.xml");
            if (showConfigFile == null)
            {
                return "Showconfig was not resolved";
            }
            var customTypesReader = new CustomTypesReader();
            var customConfig = customTypesReader.GetCustomTypes(showConfigFile.FirstOrDefault().ToString());
            return customConfig;
        }

        public static string GetConfigFile(string filePath, string fileName)
        {
            var configFileReader = new ConfigFileReader();
            return configFileReader.GetConfigFile(filePath, fileName);
        }
    }
}
