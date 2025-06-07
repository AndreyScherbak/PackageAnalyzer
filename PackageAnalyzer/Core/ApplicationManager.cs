using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Serilog;


namespace PackageAnalyzer.Core
{
    internal class ApplicationManager
    {
        public static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var type = typeof(AssemblyFileVersionAttribute);
            var versionAttribute = assembly.GetCustomAttributes(type, true);
            if (versionAttribute.Length == 0)
            {
                return string.Empty;
            }
            var version = versionAttribute[0] as AssemblyFileVersionAttribute;
            return version != null ? version.Version : string.Empty;
        }

        public static void GetStartedLogMainInfo()
        {
            Log.Information("**********************************************************************");
            Log.Information("Package Analyzer started");
            Log.Information($"Version: {GetVersion()}");
            Log.Information($"Executable: {Environment.ProcessPath}");
            Log.Information("**********************************************************************");
        }

        public static void GetFinishedLogMainInfo()
        {
            Log.Information("**********************************************************************");
            Log.Information("Package Analyzer stopped");
            Log.Information("**********************************************************************");
        }        
    }    
}
