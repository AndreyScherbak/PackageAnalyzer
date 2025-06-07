using PackageAnalyzer.Core.FileSystem;

namespace PackageAnalyzer.Core.Readers
{
    public class ConfigFileReader
    {
        public string GetConfigFile(string path, string fileName)
        {
            var manager = new FileManager(path);
            var files = manager.FindFiles(fileName, true);
            if (files.Count == 0)
            {
                return $"No {fileName} was found in {path}";
            }
            var file = files[0];
            return File.ReadAllText(file.FullName); 
        }
    }
}
