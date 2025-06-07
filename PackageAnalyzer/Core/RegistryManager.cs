using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace PackageAnalyzer.Core
{
    internal class RegistryManager
    {
        public static bool TryUpdateShellIntegration(out string errorMessage)
        {
            return TryExecuteWithElevation(() =>
            {
                UpdateContextMenu();
            }, "-UpdateRegistry", out errorMessage);
        }

        public static bool TryClearShellIntegration(out string errorMessage)
        {
            return TryExecuteWithElevation(() =>
            {
                DeleteExistingContextMenu();
            }, "-ClearRegistry", out errorMessage);
        }

        private static bool TryExecuteWithElevation(Action targetAction, string argument, out string errorMessage)
        {
            bool isElevated;
            errorMessage = null;

            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception se)
            {
                errorMessage = "Unable to validate elevated access. Error: " + se.Message;
                return false;
            }

            if (!isElevated)
            {
                var proc = new Process();
                proc.StartInfo.FileName = GetExePath();
                proc.StartInfo.Arguments = argument;
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.Verb = "runas";
                try
                {
                    proc.Start();
                    proc.WaitForExit();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("The operation was canceled by the user"))
                        errorMessage = "Unable to get elevated access for registry modification. The operation was canceled by the user.";
                    else
                        errorMessage = ex.Message;
                    return false;
                }
                return proc.ExitCode == 0;
            }

            //elevation validated, invoke the code requiring elevation
            targetAction?.Invoke();
            return true;
        }

        private static void UpdateContextMenu()
        {
            DeleteExistingContextMenu();
            CreateContextMenuRegistery("&Open with Package Analyzer", null, GetExePath(), addSeperatorAfterCommand: true);
            CreateContextMenuRegistryForFolders();
        }

        public static void DeleteExistingContextMenu()
        {
            using var classesRoot = Registry.ClassesRoot;
            using var key = classesRoot.OpenSubKey($"*\\shell", writable: true);
            key?.DeleteSubKeyTree("PackageAnalyzer", throwOnMissingSubKey: false);
            using var key2 = classesRoot.OpenSubKey($"Folder\\shell\\", writable: true);
            key2?.DeleteSubKeyTree("PackageAnalyzer", throwOnMissingSubKey: false);
        }
        private static void CreateContextMenuRegistery(string menuTitle, string commmandLineArgs, string icon = null, bool addSeperatorAfterCommand = false)
        {
            using (var appRootKey = GetAppRootKey())
            {
                using var classesRoot = Registry.ClassesRoot;
                using var key = classesRoot.OpenSubKey($"*\\shell\\PackageAnalyzer", writable: true);
                using (var commandKey = key.CreateSubKey("command"))
                {
                    if (commandKey != null)
                    {
                        commandKey.SetValue(string.Empty, $"\"{GetExePath()}\" \"%1\" {commmandLineArgs}");
                    }
                }
            }
        }

        private static void CreateContextMenuRegistryForFolders()
        {
            using var classesRoot = Registry.ClassesRoot;
            using var key = classesRoot.CreateSubKey($"Folder\\shell\\PackageAnalyzer");
            key.SetValue(string.Empty, "Open with Package Analyzer");
            key.SetValue("Icon", GetExePath());

            using var commandKey = key.CreateSubKey("command");
            commandKey.SetValue(string.Empty, $"\"{GetExePath()}\" \"%1\"");
        }

        private static RegistryKey GetAppRootKey()
        {
            using var classesRoot = Registry.ClassesRoot;
            using var key = classesRoot.CreateSubKey($"*\\shell\\PackageAnalyzer");
            key.SetValue(string.Empty, "Open with Package Analyzer");
            key.SetValue("Icon", GetExePath());
            key.SetValue("AppliesTo", "System.FileName:\"*.zip\" OR \"*.7z\" OR \"*.rar\"");
            return key;
        }

        private static string GetExePath() =>
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PackageAnalyzer.exe");

        public static string GetLogAnalyzerCommandData()
        {
            string registryPath = @"Software\Classes\folder\shell\Open with scla\command";
            string valueName = "";

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
            {
                if (key != null)
                {
                    object value = key.GetValue(valueName);
                    if (value != null && value is string)
                    {
                        return value.ToString();
                    }
                }
            }

            return null;
        }

    }
}
