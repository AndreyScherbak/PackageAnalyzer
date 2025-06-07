using PackageAnalyzer.Core.FileSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageAnalyzer.Core
{
    public static class LogAnalyzerManager
    {
        public static string GetSelectedFilePath(string selectedValue, string selectedItem, IEnumerable<string> uploadedFiles)
        {
            if (!string.IsNullOrEmpty(selectedValue))
            {
                return selectedValue;
            }

            return uploadedFiles.FirstOrDefault(file => file.Contains(selectedItem));
        }

        public static string FindLogsFolder(string filePath, IEnumerable<string> uploadedFiles)
        {
            var fileManager = new FileManager(filePath);
            string logsFolder = fileManager.FindFolder("Logs");

            if (!string.IsNullOrEmpty(logsFolder))
            {
                return logsFolder;
            }

            if (filePath.Contains("SearchArchiveTemp"))
            {
                foreach (var file in uploadedFiles)
                {
                    if (filePath.Contains(Path.GetFileNameWithoutExtension(file)))
                    {
                        var unarchivedFolder = new FileManager(file).FindFolder(Path.GetFileName(filePath));
                        logsFolder = new FileManager(unarchivedFolder).FindFolder("Logs");
                        if (!string.IsNullOrEmpty(logsFolder))
                        {
                            return logsFolder;
                        }
                    }
                }
            }

            return null;
        }

        public static void LaunchLogAnalyzer(string logAnalyzerPath, string logsFolder)
        {
            const string toRemove = " \"%1\"";
            string executablePath = logAnalyzerPath.Replace(toRemove, "");
            string arguments = $"\"{logsFolder}\""; // Enclose logsFolder in double quotes to handle spaces

            Process.Start(executablePath, arguments);
        }
    }
}

