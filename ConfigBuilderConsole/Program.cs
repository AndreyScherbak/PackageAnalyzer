using System;
using System.Reflection;
using System.Xml;

namespace ConfigBuilderConsole
{
    class Program
    {
        // Build in Release mode and copy the .exe file to the folder with PackageAnalyzer.exe
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: ConfigBuilderConsole <filePath> <assemblyPath>");
                return;
            }

            string filePath = args[0];
            string assemblyPath = args[1];

            try
            {
                // Load the assembly
                Assembly assembly = Assembly.LoadFrom(assemblyPath);

                // Get the type of ConfigBuilder
                Type configBuilderType = assembly.GetType("Sitecore.Diagnostics.ConfigBuilder.ConfigBuilder");

                // Get the Build method
                MethodInfo buildMethod = configBuilderType.GetMethod("Build", new Type[] { typeof(string), typeof(bool), typeof(bool) });

                // Invoke the Build method
                object result = buildMethod.Invoke(null, new object[] { filePath, false, false });

                // Convert the result to XmlDocument and write it to the console
                XmlDocument xmlDoc = (XmlDocument)result;
                Console.WriteLine(xmlDoc.OuterXml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}

